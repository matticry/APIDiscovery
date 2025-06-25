using System.Text;
using System.Xml;
using System.Xml.Linq;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs.SriDTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class SriCreditNoteService : ISriCreditNoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SriCreditNoteService> _logger;
    private readonly string _sriEndpointAutorizacionPruebas;
    private readonly string _sriEndpointEnvioPruebas;
    private readonly string _sriEndpointAutorizacionProduccion;
    private readonly string _sriEndpointEnvioProduccion;

    public SriCreditNoteService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<SriCreditNoteService> logger)
    {
        _context = context;
        _logger = logger;

        _sriEndpointAutorizacionPruebas = configuration.GetValue<string>("SriEndpoints:Autorizar_Pruebas") ??
                                          "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
        _sriEndpointEnvioPruebas = configuration.GetValue<string>("SriEndpoints:Enviar_Pruebas") ??
                                   "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
        _sriEndpointAutorizacionProduccion = configuration.GetValue<string>("SriEndpoints:Autorizar_Produccion") ??
                                             "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
        _sriEndpointEnvioProduccion = configuration.GetValue<string>("SriEndpoints:Enviar_Produccion") ??
                                      "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    }

    public async Task<SriResponse> EnviarNotaCreditoAsync(int creditNoteId)
    {
        try
        {
            _logger.LogInformation($"Iniciando envío de nota de crédito ID: {creditNoteId} al SRI");

            // 1) Obtener y validar la nota de crédito
            var creditNote = await _context.CreditNotes
                .Include(cn => cn.Enterprise)
                .FirstOrDefaultAsync(cn => cn.IdCreditNote == creditNoteId);

            if (creditNote == null || string.IsNullOrEmpty(creditNote.Xml))
                throw new Exception($"No se encontró la nota de crédito o su ruta XML para el ID: {creditNoteId}");

            // 2) Verificar el ambiente (1=pruebas, 2=producción)
            string sriEndpoint;
            switch (creditNote.Enterprise.environment)
            {
                case 1:
                    sriEndpoint = _sriEndpointEnvioPruebas;
                    _logger.LogInformation($"Utilizando ambiente de PRUEBAS para nota de crédito ID: {creditNoteId}");
                    break;
                case 2:
                    sriEndpoint = _sriEndpointEnvioProduccion;
                    _logger.LogInformation($"Utilizando ambiente de PRODUCCIÓN para nota de crédito ID: {creditNoteId}");
                    break;
                default:
                    throw new Exception($"Ambiente no válido ({creditNote.Enterprise?.environment}) para la empresa de la nota de crédito ID: {creditNoteId}");
            }

            // 3) Leer el XML y limpiar espacios/formatos problemáticos
            var xmlContent = await File.ReadAllTextAsync(creditNote.Xml);

            // 4) Codificar a Base64 (UTF-8 sin BOM)
            var base64Xml = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(xmlContent),
                Base64FormattingOptions.None
            );

            // Guardar el Base64 en la nota de crédito para referencia futura
            creditNote.XmlBase64 = base64Xml;
            await _context.SaveChangesAsync();

            var soapRequest = $"""
                               <soapenv:Envelope 
                                   xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/" 
                                   xmlns:ec="http://ec.gob.sri.ws.recepcion">
                                   <soapenv:Header/>
                                   <soapenv:Body>
                                       <ec:validarComprobante>
                                           <xml>{base64Xml}</xml>
                                       </ec:validarComprobante>
                                   </soapenv:Body>
                               </soapenv:Envelope>
                               """;

            _logger.LogDebug($"SOAP Request para nota de crédito {creditNoteId}: {soapRequest}");

            // 5) Enviar al SRI
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(sriEndpoint);
            httpClient.DefaultRequestHeaders.Add("SOAPAction", "");
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            var response = await httpClient.PostAsync("", new StringContent(
                soapRequest,
                Encoding.UTF8,
                "text/xml"
            ));

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"Respuesta del SRI para nota de crédito {creditNoteId}: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error HTTP al enviar nota de crédito {creditNoteId}: {response.StatusCode}");
                _logger.LogError($"Contenido de respuesta: {responseContent}");

                return new SriResponse
                {
                    Estado = "ERROR_HTTP",
                    Mensajes = [new SriMessage { Mensaje = $"Error HTTP {(int)response.StatusCode}: {response.ReasonPhrase}" }],
                    RawResponse = responseContent
                };
            }

            // 6) Parsear respuesta
            var xdoc = XDocument.Parse(responseContent);
            var ns = XNamespace.Get("http://ec.gob.sri.ws.recepcion");

            var estado = xdoc.Descendants(ns + "estado").FirstOrDefault()?.Value ?? "RECIBIDA";
            var mensajes = xdoc.Descendants("mensaje").Select(m => new SriMessage
            {
                Identificador = m.Element("identificador")?.Value ?? String.Empty,
                Mensaje = m.Element("mensaje")?.Value ?? String.Empty,
                Tipo = m.Element("tipo")?.Value ?? String.Empty
            }).ToList();

            // 7) Actualizar estado inicial en la nota de crédito
            creditNote.ElectronicStatus = estado;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Nota de crédito {creditNoteId} enviada exitosamente. Estado: {estado}");

            return new SriResponse
            {
                Estado = estado,
                Mensajes = mensajes,
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al enviar nota de crédito {creditNoteId} al SRI");
            return new SriResponse
            {
                Estado = "ERROR_INESPERADO",
                Mensajes = [new SriMessage { Mensaje = ex.Message }],
                RawResponse = ex.ToString()
            };
        }
    }

    public async Task<SriAutorizacionResponse> AutorizarNotaCreditoAsync(string claveAcceso, int creditNoteId)
    {
        try
        {
            _logger.LogInformation($"Iniciando autorización para clave: {claveAcceso} y nota de crédito ID: {creditNoteId}");

            var creditNote = await _context.CreditNotes
                .Include(cn => cn.Enterprise)
                .FirstOrDefaultAsync(cn => cn.IdCreditNote == creditNoteId);

            if (creditNote == null)
                throw new Exception($"No se encontró la nota de crédito con ID: {creditNoteId}");

            string sriEndpoint;
            switch (creditNote.Enterprise?.environment)
            {
                case 1:
                    sriEndpoint = _sriEndpointAutorizacionPruebas;
                    _logger.LogInformation($"Utilizando ambiente de PRUEBAS para nota de crédito ID: {creditNoteId}");
                    break;
                case 2:
                    sriEndpoint = _sriEndpointAutorizacionProduccion;
                    _logger.LogInformation($"Utilizando ambiente de PRODUCCIÓN para nota de crédito ID: {creditNoteId}");
                    break;
                default:
                    throw new Exception($"Ambiente no válido ({creditNote.Enterprise?.environment}) para la empresa de la nota de crédito ID: {creditNoteId}");
            }

            // 1) Construir el SOAP request con el namespace correcto
            var soapRequest = $@"
                <soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                    <soap:Header/>
                    <soap:Body>
                        <ns2:autorizacionComprobante xmlns:ns2=""http://ec.gob.sri.ws.autorizacion"">
                            <claveAccesoComprobante>{claveAcceso}</claveAccesoComprobante>
                        </ns2:autorizacionComprobante>
                    </soap:Body>
                </soap:Envelope>";

            _logger.LogDebug($"Request SOAP para autorización: {soapRequest}");

            // 2) Configurar y enviar la petición HTTP
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(sriEndpoint);
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            // Configurar encabezados exactamente como en el servicio de facturas
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("SOAPAction", "");

            // Crear el contenido HTTP con Content-Type exactamente como en el servicio original
            var content = new StringContent(
                soapRequest,
                Encoding.UTF8,
                "text/xml"
            );

            // Asegurarse de que el Content-Type sea exactamente "text/xml" sin charset
            if (content.Headers.ContentType != null) content.Headers.ContentType.CharSet = null;

            // Enviar la solicitud y capturar la respuesta
            _logger.LogInformation($"Enviando solicitud de autorización a: {sriEndpoint}");
            var httpResponse = await httpClient.PostAsync("", content);

            // Capturar la respuesta y registrar el código de estado
            var rawXml = await httpResponse.Content.ReadAsStringAsync();
            _logger.LogInformation($"Respuesta HTTP autorización: {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}");

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Error HTTP {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}");
                _logger.LogError($"Contenido de respuesta: {rawXml}");

                return new SriAutorizacionResponse
                {
                    ClaveAccesoConsultada = claveAcceso,
                    NumeroComprobantes = 0,
                    Error = $"Error HTTP {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}",
                    RawResponse = rawXml
                };
            }

            // 3) Intentar parsear la respuesta XML
            try
            {
                var xdoc = XDocument.Parse(rawXml);
                _logger.LogDebug("XML de autorización parseado correctamente");

                // Buscar en todos los namespaces para mayor robustez
                var respNode = xdoc.Descendants()
                    .FirstOrDefault(n => n.Name.LocalName == "RespuestaAutorizacionComprobante");

                if (respNode == null)
                {
                    _logger.LogWarning("No se encontró el nodo RespuestaAutorizacionComprobante");
                    _logger.LogDebug($"XML recibido: {rawXml}");

                    return new SriAutorizacionResponse
                    {
                        ClaveAccesoConsultada = claveAcceso,
                        NumeroComprobantes = 0,
                        Error = "No se encontró información de autorización en la respuesta",
                        RawResponse = rawXml
                    };
                }

                // 4) Extraer información relevante
                var clave = respNode.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "claveAccesoConsultada")?.Value;

                var numeroComprobantes = respNode.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "numeroComprobantes")?.Value;

                var count = int.TryParse(numeroComprobantes, out var tmp) ? tmp : 0;

                // 5) Procesar las autorizaciones
                var autorizaciones = respNode.Descendants()
                    .Where(n => n.Name.LocalName == "autorizacion")
                    .Select(a =>
                    {
                        var fechaStr = a.Descendants()
                            .FirstOrDefault(x => x.Name.LocalName == "fechaAutorizacion")?.Value;

                        DateTime fechaAutorizacion;
                        if (!DateTime.TryParse(fechaStr, out fechaAutorizacion))
                        {
                            fechaAutorizacion = DateTime.Now; // Valor predeterminado
                            _logger.LogWarning($"No se pudo parsear la fecha: {fechaStr}");
                        }

                        return new SriAutorizacion
                        {
                            Estado = a.Descendants().FirstOrDefault(x => x.Name.LocalName == "estado")?.Value ?? "DESCONOCIDO",
                            NumeroAutorizacion = a.Descendants()
                                .FirstOrDefault(x => x.Name.LocalName == "numeroAutorizacion")?.Value ?? "",
                            FechaAutorizacion = fechaAutorizacion,
                            Ambiente = a.Descendants().FirstOrDefault(x => x.Name.LocalName == "ambiente")?.Value ?? "",
                            Comprobante = a.Descendants().FirstOrDefault(x => x.Name.LocalName == "comprobante")?.Value ?? ""
                        };
                    })
                    .ToList();

                // 6) Construir la respuesta
                var respuesta = new SriAutorizacionResponse
                {
                    ClaveAccesoConsultada = clave,
                    NumeroComprobantes = count,
                    Autorizaciones = autorizaciones,
                    RawResponse = rawXml
                };

                _logger.LogInformation(
                    $"Autorización de nota de crédito completada. Estado: {(autorizaciones.Any() ? autorizaciones.First().Estado : "Sin autorizaciones")}");

                // 7) Actualizar el estado de la nota de crédito
                await ActualizarEstadoNotaCreditoAsync(creditNoteId, respuesta);

                return respuesta;
            }
            catch (XmlException xmlEx)
            {
                _logger.LogError(xmlEx, "Error al parsear XML de respuesta de autorización");
                return new SriAutorizacionResponse
                {
                    ClaveAccesoConsultada = claveAcceso,
                    NumeroComprobantes = 0,
                    Error = $"Error al parsear XML: {xmlEx.Message}",
                    RawResponse = rawXml
                };
            }
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout al autorizar nota de crédito en el SRI");
            return new SriAutorizacionResponse
            {
                ClaveAccesoConsultada = claveAcceso,
                NumeroComprobantes = 0,
                Error = "La operación excedió el tiempo límite (timeout)",
                RawResponse = ex.ToString()
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error de red al autorizar nota de crédito en el SRI");
            return new SriAutorizacionResponse
            {
                ClaveAccesoConsultada = claveAcceso,
                NumeroComprobantes = 0,
                Error = $"Error de red: {ex.Message}",
                RawResponse = ex.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al autorizar nota de crédito en el SRI");
            return new SriAutorizacionResponse
            {
                ClaveAccesoConsultada = claveAcceso,
                NumeroComprobantes = 0,
                Error = ex.Message,
                RawResponse = ex.ToString()
            };
        }
    }

    private async Task ActualizarEstadoNotaCreditoAsync(int creditNoteId, SriAutorizacionResponse respuesta)
    {
        try
        {
            _logger.LogInformation($"Actualizando estado de nota de crédito ID: {creditNoteId} con respuesta SRI");

            // Buscar la nota de crédito
            var creditNote = await _context.CreditNotes.FindAsync(creditNoteId);
            if (creditNote == null)
            {
                _logger.LogError($"No se encontró la nota de crédito con ID: {creditNoteId}");
                return;
            }

            // Verificar si hay autorizaciones en la respuesta
            if (!respuesta.Autorizaciones.Any())
            {
                _logger.LogWarning($"No se encontraron autorizaciones para la nota de crédito ID: {creditNoteId}");
                creditNote.ElectronicStatus = "RECHAZADO";
                await _context.SaveChangesAsync();
                return;
            }

            // Obtener la primera autorización (normalmente solo hay una)
            var autorizacion = respuesta.Autorizaciones.First();

            // Actualizar los campos basados en el estado de la autorización
            if (autorizacion.Estado == "AUTORIZADO")
            {
                _logger.LogInformation($"Nota de crédito ID: {creditNoteId} autorizada por SRI");

                // Actualizar los campos del modelo CreditNote
                creditNote.AuthorizationDate = autorizacion.FechaAutorizacion;
                creditNote.AuthorizationNumber = autorizacion.NumeroAutorizacion;
                creditNote.ElectronicStatus = "AUTORIZADO";
            }
            else
            {
                _logger.LogWarning($"Nota de crédito ID: {creditNoteId} no autorizada. Estado: {autorizacion.Estado}");

                // Actualizar solo el campo de estado
                creditNote.ElectronicStatus = autorizacion.Estado ?? "RECHAZADO";
            }

            // Guardar los cambios en la base de datos
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Estado de nota de crédito ID: {creditNoteId} actualizado exitosamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al actualizar estado de nota de crédito ID: {creditNoteId}");
        }
    }
}