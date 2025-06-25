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

public class XmlCreditNoteService : IXmlCreditNoteService
{
    private readonly string _certificadosPath;
    private readonly ApplicationDbContext _context;
    private readonly EncryptionHelper _encryptionHelper;
    private readonly string _xmlOutputDirectory;

    public XmlCreditNoteService(
        ApplicationDbContext context,
        IConfiguration config,
        EncryptionHelper encryptionHelper)
    {
        _context = context;
        _encryptionHelper = encryptionHelper;

        _xmlOutputDirectory = config.GetValue<string>("NotasCreditoXmlDirectory") ?? "NotasCreditoXml";
        _certificadosPath = config.GetValue<string>("CertificadosPath") ?? "Certificados";
        if (!Directory.Exists(_xmlOutputDirectory)) Directory.CreateDirectory(_xmlOutputDirectory);
    }

    public async Task<string> GenerarXmlNotaCreditoAsync(int creditNoteId)
    {
        // 1) Cargar y validar la nota de crédito
        var creditNote = await _context.CreditNotes
            .Include(cn => cn.Client)
            .Include(cn => cn.Branch)
            .Include(cn => cn.Enterprise)
            .Include(cn => cn.EmissionPoint)
            .Include(cn => cn.DocumentType)
            .Include(cn => cn.InvoiceOriginal)
            .Include(cn => cn.CreditNoteDetails)
                .ThenInclude(d => d.Tariff)
            .Include(cn => cn.CreditNoteDetails)
                .ThenInclude(d => d.Article)
            .FirstOrDefaultAsync(cn => cn.IdCreditNote == creditNoteId);

        if (creditNote == null)
            throw new Exception($"No se encontró la nota de crédito con ID {creditNoteId}");

        var doc = CrearEstructuraXml(creditNote);

        var baseName = creditNote.AccessKey;
        var rutaTemp = Path.Combine(_xmlOutputDirectory, $"temp_{baseName}.xml");
        var rutaFinal = Path.Combine(_xmlOutputDirectory, $"{baseName}.xml");

        // 4) Guardar temporalmente
        await Task.Run(() => doc.Save(rutaTemp));

        if (string.IsNullOrEmpty(creditNote.Enterprise?.ruc))
        {
            File.Move(rutaTemp, rutaFinal, true);
            return rutaFinal;
        }

        var certPath = await ObtenerCertificadoPath(creditNote.Enterprise.ruc)
                       ?? throw new Exception("No se encontró certificado para la empresa.");
        var clavePriv = await ObtenerClaveDesencriptada(creditNote.Enterprise.ruc)
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
            DigestMethod = DigestMethod.SHA1,
            SignatureMethod = SignatureMethod.RSAwithSHA1,
            SigningDate = DateTime.Now,
            Signer = new Signer(cert),
            ElementIdToSign = "comprobante",
            DataFormat = new DataFormat
            {
                MimeType = "text/xml",
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

        // Actualizar ruta del XML en la base de datos
        creditNote.Xml = rutaFinal;
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

    private XDocument CrearEstructuraXml(CreditNote creditNote)
    {
        // Validaciones exhaustivas
        if (creditNote.Enterprise == null) throw new Exception("creditNote.Enterprise es null");
        if (creditNote.Enterprise.company_name == null) throw new Exception("creditNote.Enterprise.company_name es null");
        if (creditNote.Enterprise.ruc == null) throw new Exception("creditNote.Enterprise.ruc es null");
        if (creditNote.Branch == null) throw new Exception("creditNote.Branch es null");
        if (creditNote.Branch.code == null) throw new Exception("creditNote.Branch.code es null");
        if (creditNote.Branch.address == null) throw new Exception("creditNote.Branch.address es null");
        if (creditNote.EmissionPoint == null) throw new Exception("creditNote.EmissionPoint es null");
        if (creditNote.EmissionPoint.code == null) throw new Exception("creditNote.EmissionPoint.code es null");
        if (creditNote.Sequence == null) throw new Exception("creditNote.Sequence es null");
        if (creditNote.Client == null) throw new Exception("creditNote.Client es null");
        if (creditNote.Client.dni == null) throw new Exception("creditNote.Client.dni es null");
        if (creditNote.DocumentType == null) throw new Exception("creditNote.DocumentType es null");
        if (creditNote.DocumentType.code == null) throw new Exception("creditNote.DocumentType.code es null");
        if (creditNote.CreditNoteDetails == null) throw new Exception("creditNote.CreditNoteDetails es null");

        foreach (var item in creditNote.CreditNoteDetails)
        {
            if (item.CodeStub == null) throw new Exception("item.CodeStub es null");
            if (item.Description == null) throw new Exception("item.Description es null");
            if (item.Tariff == null) throw new Exception("item.Tariff es null");
            if (item.Tariff?.percentage == null) throw new Exception("item.Tariff.percentage es null");
        }

        // Si todo está bien, continuamos
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null)
        );

        var notaCreditoElement = new XElement("notaCredito",
            new XAttribute("id", "comprobante"),
            new XAttribute("version", "1.1.0")
        );

        notaCreditoElement.Add(CrearInfoTributaria(creditNote));
        notaCreditoElement.Add(CrearInfoNotaCredito(creditNote));
        notaCreditoElement.Add(CrearDetalles(creditNote));
        notaCreditoElement.Add(CrearInfoAdicional(creditNote));

        doc.Add(notaCreditoElement);
        return doc;
    }

    private XElement CrearInfoTributaria(CreditNote creditNote)
    {
        var ambiente = creditNote.Enterprise.environment;
        var tipoEmision = "1"; // 1=Emisión normal

        var estab = creditNote.Branch.code.PadLeft(3, '0');
        var ptoEmi = creditNote.EmissionPoint.code.PadLeft(3, '0');
        var secuencial = creditNote.Sequence.PadLeft(9, '0');

        var infoTributaria = new XElement("infoTributaria",
            new XElement("ambiente", ambiente),
            new XElement("tipoEmision", tipoEmision),
            new XElement("razonSocial", creditNote.Enterprise.company_name),
            new XElement("nombreComercial", creditNote.Enterprise.comercial_name),
            new XElement("ruc", creditNote.Enterprise.ruc),
            new XElement("claveAcceso", creditNote.AccessKey),
            new XElement("codDoc", "04"), // Código específico para nota de crédito
            new XElement("estab", estab),
            new XElement("ptoEmi", ptoEmi),
            new XElement("secuencial", secuencial),
            new XElement("dirMatriz", creditNote.Enterprise.address_matriz)
        );

        return infoTributaria;
    }

    private XElement CrearInfoNotaCredito(CreditNote creditNote)
    {
        var obligadoContabilidad = creditNote.Enterprise.accountant switch
        {
            'Y' => "SI",
            'N' => "NO",
            _ => "NO" // Valor por defecto si no está claro
        };

        var fechaEmision = creditNote.EmissionDate.ToString("dd/MM/yyyy");
        var fechaEmisionDocSustento = creditNote.EmissionDateDocSustento.ToString("dd/MM/yyyy");

        var infoNotaCredito = new XElement("infoNotaCredito",
            new XElement("fechaEmision", fechaEmision),
            new XElement("dirEstablecimiento", creditNote.Branch.address),
            new XElement("tipoIdentificacionComprador", creditNote.Client.id_type_dni.ToString("D2")),
            new XElement("razonSocialComprador", creditNote.Client.razon_social),
            new XElement("identificacionComprador", creditNote.Client.dni),
            new XElement("obligadoContabilidad", obligadoContabilidad),
            new XElement("codDocModificado", creditNote.CodDocModificado),
            new XElement("numDocModificado", creditNote.NumDocModificado),
            new XElement("fechaEmisionDocSustento", fechaEmisionDocSustento),
            new XElement("totalSinImpuestos", FormatDecimal(creditNote.TotalWithoutTaxes)),
            new XElement("valorModificacion", FormatDecimal(creditNote.ModificationValue)),
            new XElement("moneda", GetMonedaDescripcion(creditNote.Currency))
        );

        var totalConImpuestos = CrearTotalConImpuestos(creditNote);
        infoNotaCredito.Add(totalConImpuestos);

        infoNotaCredito.Add(new XElement("motivo", creditNote.Motive));

        return infoNotaCredito;
    }

    private XElement CrearTotalConImpuestos(CreditNote creditNote)
    {
        var totalConImpuestos = new XElement("totalConImpuestos");

        // Agrupar impuestos IVA por porcentaje
        var impuestosIva = creditNote.CreditNoteDetails
            .Where(d => d.IvaValor > 0)
            .GroupBy(d => new
            {
                Codigo = "2",
                CodigoPorcentaje = GetCodigoPorcentajeIva(d.IvaPorc),
                Tarifa = d.IvaPorc
            })
            .Select(g => new
            {
                g.Key.Codigo,
                g.Key.CodigoPorcentaje,
                g.Key.Tarifa,
                BaseImponible = g.Sum(d => d.Neto),
                Valor = g.Sum(d => d.IvaValor)
            });

        foreach (var impuesto in impuestosIva)
        {
            var totalImpuesto = new XElement("totalImpuesto",
                new XElement("codigo", impuesto.Codigo),
                new XElement("codigoPorcentaje", impuesto.CodigoPorcentaje),
                new XElement("baseImponible", FormatDecimal(impuesto.BaseImponible)),
                new XElement("valor", FormatDecimal(impuesto.Valor))
            );

            totalConImpuestos.Add(totalImpuesto);
        }

        // Agrupar impuestos ICE por porcentaje
        var impuestosIce = creditNote.CreditNoteDetails
            .Where(d => d.IceValor > 0)
            .GroupBy(d => new
            {
                Codigo = "3",
                CodigoPorcentaje = "3032", // Código genérico para ICE
                Tarifa = d.IcePorc
            })
            .Select(g => new
            {
                g.Key.Codigo,
                g.Key.CodigoPorcentaje,
                g.Key.Tarifa,
                BaseImponible = g.Sum(d => d.Neto),
                Valor = g.Sum(d => d.IceValor)
            });

        foreach (var impuesto in impuestosIce)
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

    private XElement CrearDetalles(CreditNote creditNote)
    {
        var detalles = new XElement("detalles");

        foreach (var item in creditNote.CreditNoteDetails)
        {
            var detalle = new XElement("detalle",
                new XElement("codigoInterno", item.CodeStub),
                new XElement("descripcion", item.Description),
                new XElement("cantidad", FormatDecimal(item.Amount)),
                new XElement("precioUnitario", FormatDecimal(item.PriceUnit, 8, true)),
                new XElement("descuento", FormatDecimal(item.Discount)),
                new XElement("precioTotalSinImpuesto", FormatDecimal(item.Neto))
            );

            // Detalles adicionales (notas)
            if (!string.IsNullOrEmpty(item.Nota1) || !string.IsNullOrEmpty(item.Nota2) ||
                !string.IsNullOrEmpty(item.Nota3))
            {
                var detallesAdicionales = new XElement("detallesAdicionales");

                if (!string.IsNullOrEmpty(item.Nota1))
                    detallesAdicionales.Add(new XElement("detAdicional",
                        new XAttribute("nombre", "NOTAS"),
                        new XAttribute("valor", item.Nota1)));

                if (!string.IsNullOrEmpty(item.Nota2))
                    detallesAdicionales.Add(new XElement("detAdicional",
                        new XAttribute("nombre", "OBSERVACION"),
                        new XAttribute("valor", item.Nota2)));

                if (!string.IsNullOrEmpty(item.Nota3))
                    detallesAdicionales.Add(new XElement("detAdicional",
                        new XAttribute("nombre", "ADICIONAL"),
                        new XAttribute("valor", item.Nota3)));

                detalle.Add(detallesAdicionales);
            }

            // Impuestos del detalle
            var impuestos = new XElement("impuestos");

            // IVA
            if (item.IvaValor > 0)
            {
                var impuestoIva = new XElement("impuesto",
                    new XElement("codigo", "2"),
                    new XElement("codigoPorcentaje", GetCodigoPorcentajeIva(item.IvaPorc)),
                    new XElement("tarifa", FormatDecimal(item.IvaPorc, 0)),
                    new XElement("baseImponible", FormatDecimal(item.Neto)),
                    new XElement("valor", FormatDecimal(item.IvaValor))
                );
                impuestos.Add(impuestoIva);
            }

            // ICE
            if (item.IceValor > 0)
            {
                var impuestoIce = new XElement("impuesto",
                    new XElement("codigo", "3"),
                    new XElement("codigoPorcentaje", "3032"),
                    new XElement("tarifa", FormatDecimal(item.IcePorc, 2)),
                    new XElement("baseImponible", FormatDecimal(item.Neto)),
                    new XElement("valor", FormatDecimal(item.IceValor))
                );
                impuestos.Add(impuestoIce);
            }

            detalle.Add(impuestos);
            detalles.Add(detalle);
        }

        return detalles;
    }

    private static XElement CrearInfoAdicional(CreditNote creditNote)
    {
        var infoAdicional = new XElement("infoAdicional");

        if (!string.IsNullOrEmpty(creditNote.Client.address))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "Direccion"),
                creditNote.Client.address));

        if (!string.IsNullOrEmpty(creditNote.Client.phone))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "Telefono"),
                creditNote.Client.phone));

        if (!string.IsNullOrEmpty(creditNote.Client.email))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "Email"),
                creditNote.Client.email));

        if (!string.IsNullOrEmpty(creditNote.AditionalInfo))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "Información Adicional"),
                creditNote.AditionalInfo));

        if (!string.IsNullOrEmpty(creditNote.Message))
            infoAdicional.Add(new XElement("campoAdicional",
                new XAttribute("nombre", "Mensaje"),
                creditNote.Message));

        // Número de documento personalizado
        infoAdicional.Add(new XElement("campoAdicional",
            new XAttribute("nombre", "NumDocumento"),
            $"NC{creditNote.Sequence}-{creditNote.Branch.code}-{creditNote.EmissionPoint.code}"));

        return infoAdicional;
    }

    private string GetCodigoPorcentajeIva(decimal porcentaje)
    {
        return porcentaje switch
        {
            0 => "0",
            12 => "2",
            14 => "3",
            15 => "4",
            _ => "2" // Default: 12%
        };
    }

    private string GetMonedaDescripcion(string currency)
    {
        return currency switch
        {
            "USD" => "DOLAR",
            "EUR" => "EURO",
            _ => "DOLAR"
        };
    }

    private static string FormatDecimal(decimal? value, int decimals = 2, bool padSpaces = false)
    {
        var safeValue = value ?? 0m;
        var format = $"0.{new string('0', decimals)}";
        var formattedValue = safeValue.ToString(format, CultureInfo.InvariantCulture);

        return padSpaces ? $"{new string(' ', 2)}{formattedValue}" : formattedValue;
    }
}