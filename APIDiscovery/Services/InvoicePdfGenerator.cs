using System.Drawing;
using System.Globalization;
using System.Text;
using APIDiscovery.Exceptions;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using BarcodeStandard;
using DinkToPdf;
using DinkToPdf.Contracts;
using QRCoder;
using SkiaSharp;
using Type = BarcodeStandard.Type;

namespace APIDiscovery.Services;

public class InvoicePdfGenerator(IConverter converter)
{
    
    public byte[] GenerateInvoicePdf(InvoiceDTO invoice)
    {
        var htmlContent = GenerateHtmlContent(invoice);

        var globalSettings = new GlobalSettings
        {
            ColorMode = ColorMode.Color,
            Orientation = Orientation.Portrait,
            PaperSize = PaperKind.A4,
            Margins = new MarginSettings { Top = 10, Bottom = 10, Left = 10, Right = 10 },
            DocumentTitle = $"Invoice_{invoice.Sequence.Code}"
        };

        var objectSettings = new ObjectSettings
        {
            PagesCount = true,
            HtmlContent = htmlContent,
            WebSettings = { DefaultEncoding = "utf-8" },
            HeaderSettings = { FontName = "Arial", FontSize = 9, Line = false },
            FooterSettings = { 
                FontName = "Arial", 
                FontSize = 9, 
                Line = false,
                Center = "",
                Left = "Comprobante electrónico generado por RN2 Software",
                Right = "www.maxnet-business.com",
            }
        };

        var pdf = new HtmlToPdfDocument
        {
            GlobalSettings = globalSettings,
            Objects = { objectSettings }
        };

        return converter.Convert(pdf);
    }

    private static string GenerateHtmlContent(InvoiceDTO invoice)
    {
        // Format values
        var formattedInvoiceNumber =
            $"{invoice.Branch.Code?.PadLeft(3, '0')}-{invoice.EmissionPoint.Code.PadLeft(3, '0')}-{invoice.sequenceCode?.PadLeft(9, '0')}";
        var formattedEmissionDate = invoice.EmissionDate.ToString("dd/MM/yyyy");
        var formattedAuthorizationDate = invoice.AuthorizationDate?.ToString("dd/MM/yyyy HH:mm") ?? "";

        // Culture-aware decimal formatting
        var cultureInfo = new CultureInfo("es-EC");

        var sb = new StringBuilder();
        sb.Append("""
                  <!DOCTYPE html>
                  <html lang="es">
                  <head>
                      <meta charset="UTF-8">
                      <meta name="viewport" content="width=device-width, initial-scale=1.0">
                      <title>Factura</title>
                      <style>
                          body {
                              font-family: Arial, sans-serif;
                              margin: 0;
                              padding: 0;
                              font-size: 11px;
                              line-height: 1.5;
                          }
                          .container {
                              width: 100%;
                              max-width: 800px;
                              margin: 0 auto;
                              background-color: #fff;
                          }
                          .header {
                              display: table;
                              width: 100%;
                              border-bottom: 1px solid #000;
                          }
                          .left-header {
                              display: table-cell;
                              width: 40%;
                              padding: 20px;
                              background-color: #f8f8f8;
                              vertical-align: top;
                          }
                          .right-header {
                              display: table-cell;
                              width: 60%;
                              padding: 20px;
                              vertical-align: top;
                          }
                          .personal-info {
                              margin-bottom: 20px;
                          }
                          .personal-info h2 {
                              font-size: 13px;
                              margin: 0;
                              text-align: center;
                              font-weight: bold;
                          }
                          .info-row {
                              margin-bottom: 10px;
                              clear: both;
                          }
                          .info-label {
                              font-weight: bold;
                              float: left;
                              width: 35%;
                              padding-right: 10px;
                          }
                          .info-value {
                              float: left;
                              width: 60%;
                          }
                          .right-info {
                              margin-bottom: 10px;
                          }
                          .barcode {
                              text-align: center;
                              margin-top: 15px;
                              clear: both;
                          }
                          .barcode img {
                              width: 100%;
                              height: 70px;
                          }
                          .barcode-number {
                              font-size: 9px;
                              margin-top: 5px;
                              word-break: break-all;
                          }
                          .client-info {
                              display: table;
                              width: 100%;
                              border-bottom: 1px solid #000;
                              padding: 10px 0;
                          }
                          .client-left, .client-right {
                              display: table-cell;
                              width: 50%;
                              padding: 0 20px;
                              vertical-align: top;
                          }
                          table {
                              width: 100%;
                              border-collapse: collapse;
                          }
                          th {
                              background-color: #214478;
                              color: white;
                              text-align: left;
                              padding: 5px;
                          }
                          td {
                              padding: 8px 5px;
                              border-bottom: 1px solid #ddd;
                          }
                          .summary {
                              display: table;
                              width: 100%;
                              margin-top: 20px;
                          }
                          .additional-info {
                              display: table-cell;
                              width: 50%;
                              padding: 0 20px;
                              background-color: #f8f8f8;
                              vertical-align: top;
                          }
                          .totals {
                              display: table-cell;
                              width: 50%;
                              padding: 0 20px;
                              vertical-align: top;
                          }
                          .totals .info-row {
                              margin-bottom: 12px;
                          }
                          .totals .info-label {
                              width: 60%;
                          }
                          .totals .info-value {
                              width: 35%;
                              text-align: right;
                          }
                          .payment-info {
                              margin-top: 20px;
                          }
                          .payment-table {
                              width: 100%;
                              border-collapse: collapse;
                          }
                          .payment-table th, .payment-table td {
                              border: 1px solid #ddd;
                              padding: 5px;
                          }
                          .final-total {
                              background-color: #214478;
                              color: white;
                              padding: 8px 20px;
                              text-align: right;
                              font-weight: bold;
                              margin-top: 10px;
                              font-size: 14px;
                          }
                          .footer {
                              background-color: #214478;
                              color: white;
                              padding: 8px 15px;
                              margin-top: 100px;
                              display: flex;
                              justify-content: space-between;
                              align-items: center;
                              font-size: 10px;
                          }
                          .clearfix:after {
                              content: "";
                              display: table;
                              clear: both;
                          }
                      </style>
                  </head>
                  <body>
                      <div class="container">
                  """);

        // Header Section
        if (invoice.AccessKey != null)
            sb.Append($"""
                       
                               <div class="header">
                                   <div class="left-header">
                                       <div class="personal-info">
                                           <h2>{invoice.Enterprise.CompanyName}</h2>
                                           <h2>{invoice.Enterprise.ComercialName}</h2>
                                           <div class="info-row clearfix">
                                               <div class="info-label">Dirección Matriz :</div>
                                               <div class="info-value">{invoice.Enterprise.AddressMatriz}</div>
                                           </div>
                                           <div class="info-row clearfix">
                                               <div class="info-label">Dirección Sucursal :</div>
                                               <div class="info-value">{invoice.Branch.Address}</div>
                                           </div>
                                           <div class="info-row clearfix">
                                               <div class="info-label">Obligado a llevar contabilidad :</div>
                                               <div class="info-value">{(invoice.Enterprise.Accountant == 'Y' ? "SI" : "NO")}</div>
                                           </div>
                                       </div>
                                   </div>
                                   <div class="right-header">
                                       <div class="right-info">
                                           <div class="info-row clearfix">
                                               <div class="info-label">R.U.C. :</div>
                                               <div class="info-value">{invoice.Enterprise.Ruc}</div>
                                           </div>
                                           <div class="info-row clearfix">
                                               <div class="info-label">FACTURA :</div>
                                               <div class="info-value">{formattedInvoiceNumber}</div>
                                           </div>
                                           <div class="info-row clearfix">
                                               <div class="info-label">NÚMERO DE AUTORIZACIÓN :</div>
                                               <div class="info-value">{invoice.AuthorizationNumber}</div>
                                           </div>
                                           <div class="info-row clearfix">
                                               <div class="info-label">FECHA AUTORIZACION :</div>
                                               <div class="info-value">{formattedAuthorizationDate}</div>
                                           </div>
                                           <div class="info-row clearfix">
                                               <div class="info-label">AMBIENTE :</div>
                                               <div class="info-value">{(invoice.Enterprise.Enviroment == 1 ? "PRUEBAS" : "PRODUCCION")}</div>
                                           </div>
                                           <div class="info-row clearfix">
                                               <div class="info-label">EMISION :</div>
                                               <div class="info-value">NORMAL</div>
                                           </div>
                                           <div class="info-row clearfix">
                                               <div class="info-label">CLAVE DE ACCESO :</div>
                                               <div class="info-value">{invoice.AccessKey}</div>
                                           </div>
                                       </div>
                                       <div class="barcode">
                                           <img src="data:image/png;base64,{GenerateBarcode(invoice.AccessKey)}" alt="Código de barras">
                                           <div class="barcode-number">{invoice.AccessKey}</div>
                                       </div>
                                   </div>
                               </div>
                       """);

        // Client Information
        sb.Append($"""
                   
                           <div class="client-info">
                               <div class="client-left">
                                   <div class="info-row clearfix">
                                       <div class="info-label">Razón Social :</div>
                                       <div class="info-value">{invoice.Client.RazonSocial}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">Fecha de Emisión :</div>
                                       <div class="info-value">{formattedEmissionDate}</div>
                                   </div>
                               </div>
                               <div class="client-right">
                                   <div class="info-row clearfix">
                                       <div class="info-label">Identificación :</div>
                                       <div class="info-value">{invoice.Client.Dni}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">Guía de Remisión :</div>
                                       <div class="info-value"></div>
                                   </div>
                               </div>
                           </div>
                   """);

        // Invoice Details Table
        sb.Append("""
                  
                          <table>
                              <thead>
                                  <tr>
                                      <th>CÓDIGO</th>
                                      <th>DESCRIPCIÓN</th>
                                      <th>DET. ADICIONAL</th>
                                      <th>CANT.</th>
                                      <th>PRECIO U.</th>
                                      <th>DESC.</th>
                                      <th>TOTAL</th>
                                  </tr>
                              </thead>
                              <tbody>
                  """);

        // Add invoice details
        foreach (var detail in invoice.Details)
            sb.Append($"""
                       
                                       <tr>
                                           <td>{detail.CodeStub}</td>
                                           <td>{detail.Description}</td>
                                           <td>{detail.Note1}</td>
                                           <td>{detail.Amount.ToString("0.00", cultureInfo)}</td>
                                           <td>{detail.PriceUnit}</td>
                                           <td>{detail.Discount}</td>
                                           <td>{detail.Total}</td>
                                       </tr>
                       """);

        sb.Append("""
                  
                              </tbody>
                          </table>
                  """);

        // Additional Information and Totals
        sb.Append($"""
                   
                           <div class="summary">
                               <div class="additional-info">
                                   <h3>Información Adicional</h3>
                                   <div class="info-row clearfix">
                                       <div class="info-label">Dirección :</div>
                                       <div class="info-value">{invoice.Client.Address}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">Teléfono :</div>
                                       <div class="info-value">{invoice.Client.Phone}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">Email :</div>
                                       <div class="info-value">{invoice.Client.Email}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">Recargos :</div>
                                       <div class="info-value">0.00</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">Observ. :</div>
                                       <div class="info-value">{invoice.AdditionalInfo}</div>
                                   </div>
                                   
                                   <div class="payment-info">
                                       <h3>Forma de Pago</h3>
                                       <table class="payment-table">
                                           <thead>
                                               <tr>
                                                   <th></th>
                                                   <th>Valor</th>
                                                   <th>Plazo</th>
                                                   <th>Tiempo</th>
                                               </tr>
                                           </thead>
                                           <tbody>
                   """);

        // Add payment methods
        foreach (var payment in invoice.Payments)
        {
            var paymentName = GetPaymentMethodName(payment.PaymentId);
            sb.Append($"""
                       
                                                   <tr>
                                                       <td>{paymentName}</td>
                                                       <td>{payment.Total.ToString("0.00", cultureInfo)}</td>
                                                       <td>{payment.Deadline}</td>
                                                       <td>{payment.UnitTime}</td>
                                                   </tr>
                       """);
        }

        sb.Append("""
                  
                                          </tbody>
                                      </table>
                                  </div>
                              </div>
                  """);

        // Totals section
        var subtotal15 = CalculateSubtotal(invoice.Details, 15);
        var subtotal0 = CalculateSubtotal(invoice.Details, 0);
        var totalDiscount = invoice.TotalDiscount;
        const decimal ice = 0; // Assuming ICE is not used or is 0
        var iva15 = CalculateIva(invoice.Details, 15);
        const decimal irbpnr = 0; // Assuming IRBPNR is not used or is 0

        sb.Append($"""
                   
                               <div class="totals">
                                   <div class="info-row clearfix">
                                       <div class="info-label">SUBTOTAL 15% :</div>
                                       <div class="info-value">{subtotal15.ToString("0.00", cultureInfo)}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">SUBTOTAL 0% :</div>
                                       <div class="info-value">{subtotal0.ToString("0.00", cultureInfo)}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">SUBTOTAL SIN IMPUESTOS :</div>
                                       <div class="info-value">{invoice.TotalWithoutTaxes.ToString("0.00", cultureInfo)}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">TOTAL DESCUENTO :</div>
                                       <div class="info-value">{totalDiscount.ToString("0.00", cultureInfo)}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">ICE :</div>
                                       <div class="info-value">{ice.ToString("0.00", cultureInfo)}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">IVA 15% :</div>
                                       <div class="info-value">{iva15.ToString("0.00", cultureInfo)}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">IRBPNR :</div>
                                       <div class="info-value">{irbpnr.ToString("0.00", cultureInfo)}</div>
                                   </div>
                                   <div class="info-row clearfix">
                                       <div class="info-label">PROPINA :</div>
                                       <div class="info-value">{invoice.Tip.ToString("0.00", cultureInfo)}</div>
                                   </div>
                               </div>
                           </div>
                           
                           <div class="final-total">
                               VALOR TOTAL : {invoice.TotalAmount.ToString("0.00", cultureInfo)}
                           </div>
                       </div>
                   </body>
                   </html>
                   """);

        return sb.ToString();
    }

    // Helper methods
    private static string GenerateBarcode(string accessKey)
    {
        if (string.IsNullOrEmpty(accessKey))
        {
            return string.Empty;
        }

        try
        {
            var barcode = new Barcode
            {
                BarWidth = 2,
                Height = 80
            };
        
            var barcodeImage = barcode.Encode(
                Type.Code128, 
                accessKey, 
                SKColors.Black, 
                SKColors.White,  
                800,  
                80    
            );

            if (barcodeImage == null)
            {
                return string.Empty;
            }

            using var memoryStream = new MemoryStream();
            barcodeImage.Encode(SKEncodedImageFormat.Png, 100).SaveTo(memoryStream);
            var imageBytes = memoryStream.ToArray();
            return Convert.ToBase64String(imageBytes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al generar el código de barras: {ex.Message}");
            try {
                return GenerateQrCodeAsBackup(accessKey);
            }
            catch {
                return string.Empty;
            }
        }
    }

// Método alternativo usando QRCoder como respaldo
    private static string GenerateQrCodeAsBackup(string content)
    {
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var imageBytes = qrCode.GetGraphic(3);
        return Convert.ToBase64String(imageBytes);
    }

    private static string GetPaymentMethodName(int paymentId)
    {
        return paymentId switch
        {
            1 => "SIN UTILIZACIÓN DEL SISTEMA FINANCIERO",
            2 => "COMPENSACIÓN DE DEUDAS",
            3 => "TARJETA DE DÉBITO",
            4 => "TARJETA DE CRÉDITO",
            5 => "DINERO ELECTRÓNICO",
            6 => "OTROS CON UTILIZACIÓN DEL SISTEMA FINANCIERO",
            7 => "ENDOSO DE TÍTULOS",
            _ => "MÉTODO DE PAGO DESCONOCIDO"
        };
    }

    private static decimal CalculateSubtotal(List<InvoiceDetailDTO> details, decimal taxRate)
    {
        return details
            .Where(d => d.IvaPorc == taxRate)
            .Sum(d => d.Total - d.IvaValor);
    }

    private static decimal CalculateIva(List<InvoiceDetailDTO> details, decimal taxRate)
    {
        return details
            .Where(d => d.IvaPorc == taxRate)
            .Sum(d => d.IvaValor);
    }
}