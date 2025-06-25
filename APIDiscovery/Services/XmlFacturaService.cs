using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Xml.Linq;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Utils;
using FirmaXadesNet;
using FirmaXadesNet.Crypto;
using FirmaXadesNet.Signature.Parameters;
using Microsoft.EntityFrameworkCore;


namespace APIDiscovery.Services;

public class XmlFacturaService : IXmlFacturaService
{
    private readonly string _certificadosPath;
    private readonly ApplicationDbContext _context;
    private readonly EncryptionHelper _encryptionHelper;
    private readonly string _xmlOutputDirectory;

    public XmlFacturaService(
        ApplicationDbContext context,
        IConfiguration config,
        EncryptionHelper encryptionHelper)
    {
        _context = context;
        _encryptionHelper = encryptionHelper;

        _xmlOutputDirectory = config.GetValue<string>("XmlOutputDirectory") ?? "FacturasXml";
        _certificadosPath = config.GetValue<string>("CertificadosPath") ?? "Certificados";
        if (!Directory.Exists(_xmlOutputDirectory)) Directory.CreateDirectory(_xmlOutputDirectory);
    }

    public async Task<string> GenerarXmlFacturaAsync(int invoiceId)
    {
        // 1) Cargar y validar la factura
        var invoice = await _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Branch)
            .Include(i => i.Enterprise)
            .Include(i => i.EmissionPoint)
            .Include(i => i.DocumentType)
            .Include(i => i.Sequence)
            .Include(i => i.InvoiceDetails).ThenInclude(d => d.Tariff)
            .Include(i => i.InvoicePayments).ThenInclude(p => p.Payment)
            .FirstOrDefaultAsync(i => i.inv_id == invoiceId);

        if (invoice == null)
            throw new Exception($"No se encontró la factura con ID {invoiceId}");

        var doc = CrearEstructuraXml(invoice);

        var baseName = invoice.access_key;
        var rutaTemp = Path.Combine(_xmlOutputDirectory, $"temp_{baseName}.xml");
        var rutaFinal = Path.Combine(_xmlOutputDirectory, $"{baseName}.xml");

        // 4) Guardar temporalmente
        await Task.Run(() => doc.Save(rutaTemp));

        if (string.IsNullOrEmpty(invoice.Enterprise?.ruc))
        {
            File.Move(rutaTemp, rutaFinal, true);
            return rutaFinal;
        }

        var certPath = await ObtenerCertificadoPath(invoice.Enterprise.ruc)
                       ?? throw new Exception("No se encontró certificado para la empresa.");
        var clavePriv = await ObtenerClaveDesencriptada(invoice.Enterprise.ruc)
                        ?? throw new Exception("No se encontró clave privada de la empresa.");

        var cert = new X509Certificate2(
            certPath,
            clavePriv,
            X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet
        );

        var xadesService = new XadesService();
        var parameters = new SignatureParameters
        {
            SignaturePackaging = SignaturePackaging.ENVELOPED,

            DigestMethod      = DigestMethod.SHA1,
            SignatureMethod   = SignatureMethod.RSAwithSHA1,

            SigningDate       = DateTime.Now,

            Signer            = new Signer(cert),

            ElementIdToSign   = "comprobante",
            

            DataFormat = new DataFormat
            {
                MimeType    = "text/xml",
                Description = "contenido comprobante"
            }
        };
        
        XmlDocument xmlFirmado;
        await using (var fs = new FileStream(rutaTemp, FileMode.Open, FileAccess.Read))
        {
            var result = xadesService.Sign(fs, parameters);
            xmlFirmado = result.Document;
        }

        
        xmlFirmado.Save(rutaFinal);
        File.Delete(rutaTemp);

        
        invoice.xml = rutaFinal;
        await _context.SaveChangesAsync();
        return rutaFinal;
    }
    

    private async Task<string> ObtenerCertificadoPath(string ruc)
    {
        var empresa = await _context.Enterprises.FirstOrDefaultAsync(e => e.ruc == ruc);
        if (empresa == null || string.IsNullOrEmpty(empresa.electronic_signature))
            return null;

        return Path.Combine(_certificadosPath, empresa.electronic_signature);
    }

    private async Task<string> ObtenerClaveDesencriptada(string ruc)
    {
        var empresa = await _context.Enterprises.FirstOrDefaultAsync(e => e.ruc == ruc);
        if (empresa == null || string.IsNullOrEmpty(empresa.key_signature))
            return null;

        return _encryptionHelper.Decrypt(empresa.key_signature);
    }


    private XDocument CrearEstructuraXml(Invoice invoice)
    {
        if (invoice.Enterprise == null) throw new Exception("invoice.Enterprise es null");
        if (invoice.Enterprise.company_name == null) throw new Exception("invoice.Enterprise.company_name es null");
        if (invoice.Enterprise.ruc == null) throw new Exception("invoice.Enterprise.ruc es null");
        if (invoice.Branch == null) throw new Exception("invoice.Branch es null");
        if (invoice.Branch.code == null) throw new Exception("invoice.Branch.code es null");
        if (invoice.Branch.address == null) throw new Exception("invoice.Branch.address es null");
        if (invoice.EmissionPoint == null) throw new Exception("invoice.EmissionPoint es null");
        if (invoice.EmissionPoint.code == null) throw new Exception("invoice.EmissionPoint.code es null");
        if (invoice.Sequence == null) throw new Exception("invoice.Sequence es null");
        if (invoice.Sequence.code == null) throw new Exception("invoice.Sequence.code es null");
        if (invoice.Client == null) throw new Exception("invoice.Client es null");
        if (invoice.Client.dni == null) throw new Exception("invoice.Client.dni es null");
        if (invoice.DocumentType == null) throw new Exception("invoice.DocumentType es null");
        if (invoice.DocumentType.code == null) throw new Exception("invoice.DocumentType.code es null");
        if (invoice.InvoiceDetails == null) throw new Exception("invoice.InvoiceDetails es null");

        foreach (var item in invoice.InvoiceDetails)
        {
            if (item.code_stub == null) throw new Exception("item.code_stub es null");
            if (item.description == null) throw new Exception("item.description es null");
            if (item.Tariff == null) throw new Exception("item.Tariff es null");
            if (item.Tariff?.percentage == null) throw new Exception("item.Tariff.percentage es null");
        }

        // Si todo está bien, continuamos
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null)
        );

        var facturaElement = new XElement("factura",
            new XAttribute("id", "comprobante"),
            new XAttribute("version", "1.1.0")
        );

        facturaElement.Add(CrearInfoTributaria(invoice));
        facturaElement.Add(CrearInfoFactura(invoice));
        facturaElement.Add(CrearDetalles(invoice));
        facturaElement.Add(CrearInfoAdicional(invoice));

        doc.Add(facturaElement);
        return doc;
    }

    private XElement CrearInfoTributaria(Invoice invoice)
    {
        var ambiente = invoice.Enterprise.environment;
        var tipoEmision = "1"; // 1=Emisión normal

        var estab = invoice.Branch.code.PadLeft(3, '0');
        var ptoEmi = invoice.EmissionPoint.code.PadLeft(3, '0');
        var secuencial = invoice.sequence.PadLeft(9, '0');

        var infoTributaria = new XElement("infoTributaria",
            new XElement("ambiente", ambiente),
            new XElement("tipoEmision", tipoEmision),
            new XElement("razonSocial", invoice.Enterprise.company_name),
            new XElement("nombreComercial", invoice.Enterprise.comercial_name),
            new XElement("ruc", invoice.Enterprise.ruc),
            new XElement("claveAcceso", invoice.access_key),
            new XElement("codDoc", invoice.DocumentType.code.PadLeft(2, '0')),
            new XElement("estab", estab),
            new XElement("ptoEmi", ptoEmi),
            new XElement("secuencial", secuencial),
            new XElement("dirMatriz", invoice.Enterprise.address_matriz)
        );

        return infoTributaria;
    }


    private XElement CrearInfoFactura(Invoice invoice)
    {
        var obligadoContabilidad = invoice.Enterprise.accountant switch
        {
            'Y' => "SI",
            'N' => "NO",
            _ => "NO" // Valor por defecto si no está claro
        };
        var fechaEmision = invoice.emission_date.ToString("dd/MM/yyyy");

        var infoFactura = new XElement("infoFactura",
            new XElement("fechaEmision", fechaEmision),
            new XElement("dirEstablecimiento", invoice.Branch.address),
            new XElement("obligadoContabilidad", obligadoContabilidad),
            new XElement("tipoIdentificacionComprador", invoice.Client.id_type_dni.ToString("D2")),
            new XElement("razonSocialComprador", invoice.Client.razon_social),
            new XElement("identificacionComprador", invoice.Client.dni),
            new XElement("direccionComprador", invoice.Client.address),
            new XElement("totalSinImpuestos", FormatDecimal(invoice.total_without_taxes)),
            new XElement("totalDescuento", FormatDecimal(invoice.total_discount))
        );

        var totalConImpuestos = CrearTotalConImpuestos(invoice);
        infoFactura.Add(totalConImpuestos);

        infoFactura.Add(new XElement("propina", FormatDecimal(invoice.tip)));

        infoFactura.Add(new XElement("importeTotal", FormatDecimal(invoice.total_amount)));

        infoFactura.Add(new XElement("moneda", invoice.currency));


        if (invoice.InvoicePayments.Count == 0) return infoFactura;
        var pagos = new XElement("pagos");

        foreach (var pagoItem in invoice.InvoicePayments)
        {
            var pago = new XElement("pago",
                new XElement("formaPago", pagoItem.Payment?.sri_detail ?? "01"),
                new XElement("total", FormatDecimal(pagoItem.total))
            );

            if (pagoItem.deadline > 0)
            {
                pago.Add(new XElement("plazo", pagoItem.deadline));
                pago.Add(new XElement("unidadTiempo", pagoItem.unit_time ?? "días"));
            }

            pagos.Add(pago);
        }

        infoFactura.Add(pagos);

        return infoFactura;
    }

    private XElement CrearTotalConImpuestos(Invoice invoice)
    {
        var totalConImpuestos = new XElement("totalConImpuestos");

        var impuestosPorTipo = invoice.InvoiceDetails
            .GroupBy(d => new
                { Codigo = "2", CodigoPorcentaje = d.Tariff?.code ?? "2", Tarifa = d.Tariff?.percentage ?? 12 })
            .Select(g => new
            {
                g.Key.Codigo,
                g.Key.CodigoPorcentaje,
                g.Key.Tarifa,
                BaseImponible = g.Sum(d => d.neto),
                Valor = g.Sum(d => d.iva_valor)
            });

        foreach (var impuesto in impuestosPorTipo)
        {
            var totalImpuesto = new XElement("totalImpuesto",
                new XElement("codigo", impuesto.Codigo),
                new XElement("codigoPorcentaje", impuesto.CodigoPorcentaje),
                new XElement("baseImponible", FormatDecimal(impuesto.BaseImponible)),
                new XElement("valor", FormatDecimal(impuesto.Valor))
            );

            totalConImpuestos.Add(totalImpuesto);
        }

        return totalConImpuestos;
    }

    private XElement CrearDetalles(Invoice invoice)
    {
        var detalles = new XElement("detalles");

        foreach (var item in invoice.InvoiceDetails)
        {
            var detalle = new XElement("detalle",
                new XElement("codigoPrincipal", item.code_stub),
                new XElement("descripcion", item.description),
                new XElement("cantidad", FormatDecimal(item.amount)),
                new XElement("precioUnitario", FormatDecimal(item.price_unit, 8, true)),
                new XElement("descuento", FormatDecimal(item.discount)),
                new XElement("precioTotalSinImpuesto", FormatDecimal(item.neto))
            );

            if (!string.IsNullOrEmpty(item.note1) || !string.IsNullOrEmpty(item.note2) ||
                !string.IsNullOrEmpty(item.note3))
            {
                var detallesAdicionales = new XElement("detallesAdicionales");

                if (!string.IsNullOrEmpty(item.note1))
                    detallesAdicionales.Add(new XElement("detAdicional",
                        new XAttribute("nombre", "NOTAS"),
                        new XAttribute("valor", item.note1)));

                if (!string.IsNullOrEmpty(item.note2))
                    detallesAdicionales.Add(new XElement("detAdicional",
                        new XAttribute("nombre", "OBSERVACION"),
                        new XAttribute("valor", item.note2)));

                detalle.Add(detallesAdicionales);
            }

            var impuestos = new XElement("impuestos");
            var impuesto = new XElement("impuesto",
                new XElement("codigo", "2"),
                new XElement("codigoPorcentaje", item.Tariff?.code ?? "2"),
                new XElement("tarifa", FormatDecimal(item.Tariff?.percentage ?? 12, 0)),
                new XElement("baseImponible", FormatDecimal(item.neto)),
                new XElement("valor", FormatDecimal(item.iva_valor))
            );

            impuestos.Add(impuesto);
            detalle.Add(impuestos);

            detalles.Add(detalle);
        }

        return detalles;
    }

    private static XElement CrearInfoAdicional(Invoice invoice)
    {
        var infoAdicional = new XElement("infoAdicional");

        if (!string.IsNullOrEmpty(invoice.Client.address))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "Direccion"),
                invoice.Client.address));

        if (!string.IsNullOrEmpty(invoice.Client.phone))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "Telefono"),
                invoice.Client.phone));

        if (!string.IsNullOrEmpty(invoice.Client.email))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "Email"),
                invoice.Client.email));

        if (!string.IsNullOrEmpty(invoice.additional_info))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "NumDocumento"),
                invoice.additional_info));

        return infoAdicional;
    }

    private static string FormatDecimal(decimal? value, int decimals = 2, bool padSpaces = false)
    {
        var safeValue = value ?? 0m;
        var format = $"0.{new string('0', decimals)}";
        var formattedValue = safeValue.ToString(format, CultureInfo.InvariantCulture);

        return padSpaces ? $"{new string(' ', 2)}{formattedValue}" : formattedValue;
    }
}