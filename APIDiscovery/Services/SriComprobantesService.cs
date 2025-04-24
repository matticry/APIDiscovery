using System.Text;
using System.Xml;
using System.Xml.Linq;
using APIDiscovery.Interfaces;
using APIDiscovery.Models.DTOs.SriDTOs;

namespace APIDiscovery.Services;

public class SriComprobantesService : ISriComprobantesService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SriComprobantesService> _logger;
    private readonly string _sriEndpointProduccion;
    private readonly string _sriEndpointPruebas;

    private readonly IXmlFacturaService _xmlFacturaService;

    public SriComprobantesService(
        IXmlFacturaService xmlFacturaService,
        IConfiguration configuration,
        ILogger<SriComprobantesService> logger)
    {
        _xmlFacturaService = xmlFacturaService;
        _configuration = configuration;
        _logger = logger;
        _sriEndpointPruebas = _configuration.GetValue<string>("SriEndpoints:Pruebas") ??
                              "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
        _sriEndpointProduccion = _configuration.GetValue<string>("SriEndpoints:Produccion") ??
                                 "https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline";
    }

    public async Task<SriResponse> EnviarComprobanteAsync(int invoiceId)
    {
        try
        {
            // 1. Generar el XML de la factura
            var xmlFilePath = await _xmlFacturaService.GenerarXmlFacturaAsync(invoiceId);
            if (string.IsNullOrEmpty(xmlFilePath) || !File.Exists(xmlFilePath))
                throw new Exception($"No se pudo generar el archivo XML para la factura ID: {invoiceId}");

            // 2. Leer el XML generado
            var xmlContent = await File.ReadAllTextAsync(xmlFilePath);
            var xmlDoc = XDocument.Parse(xmlContent);

            // 3. Determinar el ambiente (1=pruebas, 2=producción)
            var ambiente = int.Parse(xmlDoc.Root?.Element("infoTributaria")?.Element("ambiente")?.Value ?? "1");
            var endpoint = ambiente == 1 ? _sriEndpointPruebas : _sriEndpointProduccion;

            // 4. Convertir el XML a Base64
            var xmlBytes = Encoding.UTF8.GetBytes(xmlContent);
            var base64String = Convert.ToBase64String(xmlBytes);

            // 5. Preparar la solicitud SOAP
            var response = await EnviarSolicitudSoapAsync(endpoint, base64String);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar comprobante al SRI para factura ID: {InvoiceId}", invoiceId);
            throw;
        }
    }

    private async Task<SriResponse> EnviarSolicitudSoapAsync(string endpoint, string base64Xml)
    {
        // Construir el envelope SOAP
        var soapEnvelope = new StringBuilder();
        soapEnvelope.Append(
            "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ec=\"http://ec.gob.sri.ws.recepcion\">");
        soapEnvelope.Append("<soapenv:Header/>");
        soapEnvelope.Append("<soapenv:Body>");
        soapEnvelope.Append("<ec:validarComprobante>");
        soapEnvelope.Append($"<xml>{base64Xml}</xml>");
        soapEnvelope.Append("</ec:validarComprobante>");
        soapEnvelope.Append("</soapenv:Body>");
        soapEnvelope.Append("</soapenv:Envelope>");

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("SOAPAction", ""); // Si es necesario

        var content = new StringContent(soapEnvelope.ToString(), Encoding.UTF8, "text/xml");

        try
        {
            var httpResponse = await httpClient.PostAsync(endpoint, content);
            httpResponse.EnsureSuccessStatusCode();

            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            return ProcesarRespuestaSoap(responseContent);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error en la comunicación HTTP con el SRI: {Message}", ex.Message);
            return new SriResponse
            {
                Estado = "ERROR",
                Mensajes = new List<SriMessage>
                {
                    new()
                    {
                        Identificador = "ERROR_HTTP",
                        Mensaje = ex.Message,
                        Tipo = "ERROR"
                    }
                }
            };
        }
    }

    private SriResponse ProcesarRespuestaSoap(string soapResponse)
    {
        try
        {
            var response = new SriResponse();

            // Parsear la respuesta SOAP
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(soapResponse);

            // Definir los namespaces
            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            nsManager.AddNamespace("ns2", "http://ec.gob.sri.ws.recepcion");

            // Extraer el estado
            var estadoNode = xmlDoc.SelectSingleNode("//ns2:RespuestaRecepcionComprobante/estado", nsManager);
            response.Estado = estadoNode?.InnerText ?? "DESCONOCIDO";

            // Extraer mensajes
            var mensajesNodes =
                xmlDoc.SelectNodes("//ns2:RespuestaRecepcionComprobante/comprobantes/comprobante/mensajes/mensaje",
                    nsManager);

            if (mensajesNodes != null)
            {
                response.Mensajes = new List<SriMessage>();

                foreach (XmlNode mensajeNode in mensajesNodes)
                {
                    var mensaje = new SriMessage();

                    var identificadorNode = mensajeNode.SelectSingleNode("identificador", nsManager);
                    mensaje.Identificador = identificadorNode?.InnerText;

                    var mensajeTextNode = mensajeNode.SelectSingleNode("mensaje", nsManager);
                    mensaje.Mensaje = mensajeTextNode?.InnerText;

                    var infoAdicionalNode = mensajeNode.SelectSingleNode("informacionAdicional", nsManager);
                    mensaje.InformacionAdicional = infoAdicionalNode?.InnerText;

                    var tipoNode = mensajeNode.SelectSingleNode("tipo", nsManager);
                    mensaje.Tipo = tipoNode?.InnerText;

                    response.Mensajes.Add(mensaje);
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la respuesta SOAP del SRI: {Message}", ex.Message);
            return new SriResponse
            {
                Estado = "ERROR_PARSE",
                Mensajes = new List<SriMessage>
                {
                    new()
                    {
                        Identificador = "ERROR_PARSING",
                        Mensaje = "Error al procesar la respuesta: " + ex.Message,
                        Tipo = "ERROR"
                    }
                }
            };
        }
    }
}