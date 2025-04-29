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
    private readonly string _sriEndpointAutorizacion;
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
        _sriEndpointAutorizacion = _configuration.GetValue<string>("SriEndpoints:Produccion") ??
                                 "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline";
    }

    public async Task<SriResponse> EnviarComprobanteAsync(string base64Xml)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(_sriEndpointPruebas);

            // **SOAP Envelope correctamente formado**
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

            var content = new StringContent(
                soapRequest,
                Encoding.UTF8,
                "text/xml" // **Content-Type para SOAP**
            );

            // **Headers requeridos por el SRI**
            httpClient.DefaultRequestHeaders.Add("SOAPAction", "");

            var response = await httpClient.PostAsync("", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error SRI: {responseContent}");
            }

            // **Parsear respuesta SOAP**
            var xdoc = XDocument.Parse(responseContent);
            var ns = XNamespace.Get("http://ec.gob.sri.ws.recepcion");
            var estado = xdoc.Descendants(ns + "estado").FirstOrDefault()?.Value;

            return new SriResponse
            {
                Estado = estado,
                Mensaje = xdoc.Descendants(ns + "mensaje").FirstOrDefault()?.Value,
                XmlResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en comunicación con SRI");
            throw;
        }
    }
}