using System.Text;
using System.Xml;
using System.Xml.Linq;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs.SriDTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class SriComprobantesService(
    IXmlFacturaService xmlFacturaService,
    IConfiguration configuration,
    ILogger<SriComprobantesService> logger,
    ApplicationDbContext context)
    : ISriComprobantesService
{
    private readonly string _sriEndpointAutorizacionPruebas = configuration.GetValue<string>("SriEndpoints:Autorizar_Pruebas") ??
                                                               "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    private readonly string _sriEndpointEnvioPruebas = configuration.GetValue<string>("SriEndpoints:Enviar_Pruebas") ??
                                                       "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    private readonly string _sriEndpointAutorizacionProduccion = configuration.GetValue<string>("SriEndpoints:Autorizar_Produccion") ??
                                                                  "https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    private readonly string _sriEndpointEnvioProduccion = configuration.GetValue<string>("SriEndpoints:Enviar_Produccion") ??
                                                          "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";


    public async Task<SriResponse> EnviarComprobanteAsync(int invoiceId)
    {
        try
        {
            // 1) Obtener y validar la factura
            var invoice = await context.Invoices
                .Include(i => i.Enterprise) 
                .FirstOrDefaultAsync(i => i.inv_id == invoiceId);
                
            if (invoice == null || string.IsNullOrEmpty(invoice.xml))
                throw new Exception($"No se encontró la factura o su ruta XML para el ID: {invoiceId}");

            // 2) Verificar el ambiente (1=pruebas, 2=producción)
            string sriEndpoint;
            switch (invoice.Enterprise.environment)
            {
                case 1:
                    sriEndpoint = _sriEndpointEnvioPruebas;
                    logger.LogInformation($"Utilizando ambiente de PRUEBAS para factura ID: {invoiceId}");
                    break;
                case 2:
                    sriEndpoint = _sriEndpointEnvioProduccion;
                    logger.LogInformation($"Utilizando ambiente de PRODUCCIÓN para factura ID: {invoiceId}");
                    break;
                default:
                    throw new Exception($"Ambiente no válido ({invoice.Enterprise?.environment}) para la empresa de la factura ID: {invoiceId}");
            }

            // 3) Leer el XML y limpiar espacios/formatos problemáticos
            var xmlContent = await File.ReadAllTextAsync(invoice.xml);
            
            // 4) Codificar a Base64 (UTF-8 sin BOM)
            var base64Xml = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(xmlContent),
                Base64FormattingOptions.None
            );
            invoice.XmlBase64 = base64Xml;
            await context.SaveChangesAsync();

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

            // 7) Enviar al SRI
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(sriEndpoint);
            httpClient.DefaultRequestHeaders.Add("SOAPAction", "");

            var response = await httpClient.PostAsync("", new StringContent(
                soapRequest,
                Encoding.UTF8,
                "text/xml"
            ));

            var responseContent = await response.Content.ReadAsStringAsync();

            // 8) Parsear respuesta
            var xdoc = XDocument.Parse(responseContent);
            var ns = XNamespace.Get("http://ec.gob.sri.ws.recepcion");

            return new SriResponse
            {
                Estado = xdoc.Descendants(ns + "estado").FirstOrDefault()?.Value ?? "RECIBIDA",
                Mensajes = xdoc.Descendants("mensaje").Select(m => new SriMessage
                {
                    Identificador = m.Element("identificador")?.Value ?? String.Empty,
                    Mensaje = m.Element("mensaje")?.Value ?? String.Empty,
                    Tipo = m.Element("tipo")?.Value ?? String.Empty
                }).ToList(),
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar comprobante al SRI");
            return new SriResponse
            {
                Estado = "ERROR_INESPERADO",
                Mensajes = [new SriMessage { Mensaje = ex.Message }],
                RawResponse = ex.ToString()
            };
        }
    }

    public async Task<SriAutorizacionResponse> AutorizarComprobanteAsync(string claveAcceso, int invoiceId)
    {
        try
        {
            logger.LogInformation($"Iniciando autorización para clave: {claveAcceso} y factura ID: {invoiceId}");
            
            var invoice = await context.Invoices.Include(i => i.Enterprise).FirstOrDefaultAsync(i => i.inv_id == invoiceId);
            if (invoice == null)
                throw new Exception($"No se encontró la factura con ID: {invoiceId}");
            
            string sriEndpoint;
            switch (invoice.Enterprise?.environment)
            {
                case 1:
                    sriEndpoint = _sriEndpointAutorizacionPruebas;
                    logger.LogInformation($"Utilizando ambiente de PRUEBAS para factura ID: {invoiceId}");
                    break;
                case 2:
                    sriEndpoint = _sriEndpointAutorizacionProduccion;
                    logger.LogInformation($"Utilizando ambiente de PRODUCCIÓN para factura ID: {invoiceId}");
                    break;
                default:
                    throw new Exception($"Ambiente no válido ({invoice.Enterprise?.environment}) para la empresa de la factura ID: {invoiceId}");
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

            logger.LogDebug($"Request SOAP: {soapRequest}");

            // 2) Configurar y enviar la petición HTTP
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(sriEndpoint);
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            // Configurar encabezados exactamente como en Postman
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("SOAPAction", "");

            // Crear el contenido HTTP con Content-Type exactamente como en Postman
            var content = new StringContent(
                soapRequest,
                Encoding.UTF8,
                "text/xml"
            );

            // Asegurarse de que el Content-Type sea exactamente "text/xml" sin charset
            if (content.Headers.ContentType != null) content.Headers.ContentType.CharSet = null;

            // Enviar la solicitud y capturar la respuesta
            logger.LogInformation($"Enviando solicitud a: {_sriEndpointAutorizacionPruebas}");
            var httpResponse = await httpClient.PostAsync("", content);

            // Capturar la respuesta y registrar el código de estado
            var rawXml = await httpResponse.Content.ReadAsStringAsync();
            logger.LogInformation($"Respuesta HTTP: {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}");

            if (!httpResponse.IsSuccessStatusCode)
            {
                logger.LogError($"Error HTTP {(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}");
                logger.LogError($"Contenido de respuesta: {rawXml}");

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
                logger.LogDebug("XML parseado correctamente");

                // Buscar en todos los namespaces para mayor robustez
                var respNode = xdoc.Descendants()
                    .FirstOrDefault(n => n.Name.LocalName == "RespuestaAutorizacionComprobante");

                if (respNode == null)
                {
                    logger.LogWarning("No se encontró el nodo RespuestaAutorizacionComprobante");
                    logger.LogDebug($"XML recibido: {rawXml}");

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
                        if (DateTime.TryParse(fechaStr, out fechaAutorizacion))
                            return new SriAutorizacion
                            {
                                Estado = a.Descendants().FirstOrDefault(x => x.Name.LocalName == "estado")?.Value ??
                                         throw new InvalidOperationException(),
                                NumeroAutorizacion = a.Descendants()
                                                         .FirstOrDefault(x => x.Name.LocalName == "numeroAutorizacion")
                                                         ?.Value ??
                                                     throw new InvalidOperationException(),
                                FechaAutorizacion = fechaAutorizacion,
                                Ambiente = a.Descendants().FirstOrDefault(x => x.Name.LocalName == "ambiente")?.Value ??
                                           throw new InvalidOperationException(),
                                Comprobante =
                                    a.Descendants().FirstOrDefault(x => x.Name.LocalName == "comprobante")?.Value ??
                                    throw new InvalidOperationException()
                            };
                        fechaAutorizacion = DateTime.Now; // Valor predeterminado
                        logger.LogWarning($"No se pudo parsear la fecha: {fechaStr}");

                        return new SriAutorizacion
                        {
                            Estado = a.Descendants().FirstOrDefault(x => x.Name.LocalName == "estado")?.Value ?? throw new InvalidOperationException(),
                            NumeroAutorizacion = a.Descendants()
                                .FirstOrDefault(x => x.Name.LocalName == "numeroAutorizacion")?.Value ?? throw new InvalidOperationException(),
                            FechaAutorizacion = fechaAutorizacion,
                            Ambiente = a.Descendants().FirstOrDefault(x => x.Name.LocalName == "ambiente")?.Value ?? throw new InvalidOperationException(),
                            Comprobante = a.Descendants().FirstOrDefault(x => x.Name.LocalName == "comprobante")?.Value ?? throw new InvalidOperationException()
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

                logger.LogInformation(
                    $"Autorización completada. Estado: {(autorizaciones.Any() ? autorizaciones.First().Estado : "Sin autorizaciones")}");

                // 7) Actualizar el estado de la factura si se proporcionó un ID
                if (invoiceId > 0) await ActualizarEstadoFacturaAsync(invoiceId, respuesta);

                return respuesta;
            }
            catch (XmlException xmlEx)
            {
                logger.LogError(xmlEx, "Error al parsear XML de respuesta");
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
            logger.LogError(ex, "Timeout al autorizar comprobante en el SRI");
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
            logger.LogError(ex, "Error de red al autorizar comprobante en el SRI");
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
            logger.LogError(ex, "Error general al autorizar comprobante en el SRI");
            return new SriAutorizacionResponse
            {
                ClaveAccesoConsultada = claveAcceso,
                NumeroComprobantes = 0,
                Error = ex.Message,
                RawResponse = ex.ToString()
            };
        }
    }


    private async Task ActualizarEstadoFacturaAsync(int invoiceId, SriAutorizacionResponse respuesta)
    {
        try
        {
            logger.LogInformation($"Actualizando estado de factura ID: {invoiceId} con respuesta SRI");

            // Buscar la factura
            var invoice = await context.Invoices.FindAsync(invoiceId);
            if (invoice == null)
            {
                logger.LogError($"No se encontró la factura con ID: {invoiceId}");
                return;
            }

            // Verificar si hay autorizaciones en la respuesta
            if (!respuesta.Autorizaciones.Any())
            {
                logger.LogWarning($"No se encontraron autorizaciones para la factura ID: {invoiceId}");
                invoice.electronic_status = "RECHAZADO";
                invoice.invoice_status = "NO AUTORIZADO";
                await context.SaveChangesAsync();
                return;
            }

            // Obtener la primera autorización (normalmente solo hay una)
            var autorizacion = respuesta.Autorizaciones.First();

            // Actualizar los campos basados en el estado de la autorización
            if (autorizacion.Estado == "AUTORIZADO")
            {
                logger.LogInformation($"Factura ID: {invoiceId} autorizada por SRI");

                // Actualizar los campos del modelo Invoice
                invoice.authorization_date = autorizacion.FechaAutorizacion;
                invoice.authorization_number = autorizacion.NumeroAutorizacion;
                invoice.electronic_status = "AUTORIZADO";
                invoice.invoice_status = "AUTORIZADO";
            }
            else
            {
                logger.LogWarning($"Factura ID: {invoiceId} no autorizada. Estado: {autorizacion.Estado}");

                // Actualizar solo los campos de estado
                invoice.electronic_status = autorizacion.Estado ?? "RECHAZADO";
                invoice.invoice_status = "NO AUTORIZADO";
            }

            // Guardar los cambios en la base de datos
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error al actualizar estado de factura ID: {invoiceId}");
        }
    }
}