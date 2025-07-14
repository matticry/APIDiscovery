using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using Microsoft.EntityFrameworkCore;
namespace APIDiscovery.Services;

public partial class EmailSendService : IEmailSendService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailSendService> _logger;
    private readonly InvoicePdfGenerator _pdfGenerator;

    public EmailSendService(
        ApplicationDbContext context,
        InvoicePdfGenerator pdfGenerator,
        ILogger<EmailSendService> logger)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }

    /// <summary>
    ///     Envía una factura por correo electrónico al cliente.
    /// </summary>
    /// <param name="invoiceId">ID de la factura a enviar</param>
    /// <returns>True si el envío fue exitoso, False en caso contrario</returns>
    public async Task<bool> SendInvoiceEmailAsync(int invoiceId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Enterprise)
                .Include(i => i.Client)
                .Include(i => i.Branch)
                .Include(i => i.EmissionPoint)
                .Include(i => i.Sequence)
                .Include(i => i.DocumentType)
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Article)
                .Include(i => i.InvoiceDetails)
                .ThenInclude(d => d.Tariff)
                .Include(i => i.InvoicePayments)
                .ThenInclude(p => p.Payment)
                .FirstOrDefaultAsync(i => i.inv_id == invoiceId);

            if (invoice == null)
                throw new EntityNotFoundException($"No se encontró la factura con ID {invoiceId}");

            if (invoice.Enterprise == null)
                throw new BusinessException("La factura no tiene asociada una empresa");

            if (invoice.Client == null || string.IsNullOrEmpty(invoice.Client.email))
                throw new BusinessException("El cliente no tiene un correo electrónico registrado");

            ValidateEmailConfiguration(invoice.Enterprise);
            var invoiceDto = MapToDto(invoice);
            var pdfBytes = _pdfGenerator.GenerateInvoicePdf(invoiceDto);
            await SendEmailWithAttachment(
                invoice.Enterprise,
                invoice.Client.email,
                invoice.Client.razon_social,
                invoiceDto,
                pdfBytes);

            // 6. Registrar el envío exitoso (opcional)
            // Si necesitas añadir campos a la entidad Invoice para registrar el envío:
            // invoice.email_sent = true;
            // invoice.email_sent_date = DateTime.Now;
            // await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar factura por correo: {Message}", ex.Message);
            return false;
        }
    }
    
    private static void ValidateEmailConfiguration(Enterprise enterprise)
    {
        // Validar correo del remitente
        if (string.IsNullOrEmpty(enterprise.email_user))
            throw new BusinessException("La empresa no tiene configurado un correo electrónico de envío");

        if (!IsValidEmail(enterprise.email_user))
            throw new BusinessException($"El correo electrónico {enterprise.email_user} no es válido");

        // Validar contraseña
        if (string.IsNullOrEmpty(enterprise.email_password))
            throw new BusinessException("No se ha configurado la contraseña del correo electrónico");

        // Validar puerto
        if (string.IsNullOrEmpty(enterprise.email_port) || !int.TryParse(enterprise.email_port, out _))
            throw new BusinessException("El puerto SMTP configurado no es válido");

        // Validar servidor SMTP
        if (string.IsNullOrEmpty(enterprise.email_smtp))
            throw new BusinessException("No se ha configurado el servidor SMTP");
    }
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
        try
        {
            var regex = MyRegex();
            return regex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     Envía un correo electrónico con el PDF de la factura adjunto
    /// </summary>
    private async Task SendEmailWithAttachment(
        Enterprise enterprise, 
        string clientEmail, 
        string clientName,
        InvoiceDTO invoice,
        byte[] pdfAttachment)
    {
        // Usar estos valores exactos que vimos en la base de datos
        var smtpServer = enterprise.email_smtp.Trim(); 
        var smtpPort = int.Parse(enterprise.email_port.Trim()); 
        var smtpUsername = enterprise.email_user.Trim(); 
        var password = enterprise.email_password.Trim(); 
        
        _logger.LogInformation($"Configuración SMTP: Servidor={smtpServer}, Puerto={smtpPort}, Usuario={smtpUsername}");
        
        try
        {
            // Configurar cliente SMTP con propiedades exactas
            var smtpClient = new SmtpClient
            {
                Host = smtpServer,
                Port = smtpPort,
                EnableSsl = true, 
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUsername, password),
                Timeout = 30000 
            };

            // Crear el mensaje
            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUsername, enterprise.company_name ?? "Sistema de Facturación"),
                Subject = $"Factura Electrónica {invoice.Branch.Code}-{invoice.EmissionPoint.Code}-{invoice.sequenceCode}",
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(new MailAddress(clientEmail, clientName));
            
            var body = new StringBuilder();
            body.AppendLine($"<h2>Estimado/a {clientName},</h2>");
            body.AppendLine("<p>Adjunto encontrará su factura electrónica.</p>");
            body.AppendLine($"<p><strong>Número de factura:</strong> {invoice.Branch.Code}-{invoice.EmissionPoint.Code}-{invoice.sequenceCode}</p>");
            body.AppendLine($"<p><strong>Fecha de emisión:</strong> {invoice.EmissionDate:dd/MM/yyyy}</p>");
            body.AppendLine($"<p><strong>Total:</strong> ${invoice.TotalAmount:N2}</p>");
            
            if (!string.IsNullOrEmpty(invoice.AccessKey))
            {
                body.AppendLine($"<p><strong>Clave de acceso:</strong> {invoice.AccessKey}</p>");
            }
            
            body.AppendLine("<p>Este correo es generado automáticamente, por favor no responda a este mensaje.</p>");
            body.AppendLine($"<p>Atentamente,<br>{enterprise.company_name}</p>");

            mailMessage.Body = body.ToString();

            // Adjuntar el PDF
            var fileName = $"Factura_{invoice.Branch.Code}-{invoice.EmissionPoint.Code}-{invoice.sequenceCode}.pdf";
            using var ms = new MemoryStream(pdfAttachment);
            var attachment = new Attachment(ms, fileName, "application/pdf");
            mailMessage.Attachments.Add(attachment);

            _logger.LogInformation($"Intentando enviar correo a: {clientEmail} desde: {smtpUsername}");
            await Task.Run(() => smtpClient.Send(mailMessage));
            
            _logger.LogInformation("Correo enviado exitosamente a {Email} para factura {InvoiceNumber}", 
                clientEmail, $"{invoice.Branch.Code}-{invoice.EmissionPoint.Code}-{invoice.sequenceCode}");
        }
        catch (SmtpException smtpEx)
        {
            _logger.LogError("Error SMTP específico: Código={StatusCode}, Mensaje={Message}", 
                smtpEx.StatusCode, smtpEx.Message);
            
            throw new BusinessException($"Error al enviar correo: {smtpEx.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al enviar correo: {Message}", ex.Message);
            throw new BusinessException($"Error al enviar correo: {ex.Message}");
        }
    }
    
    private InvoiceDTO MapToDto(Invoice invoice)
    {
        var invoiceDto = new InvoiceDTO
        {
            InvoiceId = invoice.inv_id,
            EmissionDate = invoice.emission_date,
            AuthorizationDate = invoice.authorization_date,
            AuthorizationNumber = invoice.authorization_number,
            AccessKey = invoice.access_key ?? string.Empty,
            TotalAmount = invoice.total_amount,
            TotalWithoutTaxes = invoice.total_without_taxes,
            TotalDiscount = invoice.total_discount,
            Tip = invoice.tip,
            sequenceCode = invoice.sequence,
            AdditionalInfo = invoice.additional_info ?? string.Empty,

            Enterprise = new EnterpriseDTO
            {
                IdEnterprise = invoice.Enterprise.id_en,
                Ruc = invoice.Enterprise.ruc,
                CompanyName = invoice.Enterprise.company_name,
                ComercialName = invoice.Enterprise.comercial_name,
                AddressMatriz = invoice.Enterprise.address_matriz,
                Accountant = invoice.Enterprise.accountant,
                Enviroment = invoice.Enterprise.environment
            },

            Branch = new BranchDto
            {
                IdBranch = invoice.Branch.id_br,
                Code = invoice.Branch.code,
                Address = invoice.Branch.address,
                Description = invoice.Branch.description
            },

            EmissionPoint = new EmissionPointDto
            {
                IdEmissionPoint = invoice.EmissionPoint.id_e_p,
                Code = invoice.EmissionPoint.code,
                Details = invoice.EmissionPoint.details
            },

            Sequence = new SequenceDTO
            {
                IdSequence = invoice.Sequence.id_sequence,
                Code = invoice.Sequence.code
            },

            Client = new ClientDTO
            {
                RazonSocial = invoice.Client.razon_social,
                Dni = invoice.Client.dni,
                Address = invoice.Client.address,
                Phone = invoice.Client.phone,
                Email = invoice.Client.email
            },

            Details = invoice.InvoiceDetails.Select(d => new InvoiceDetailDTO
            {
                CodeStub = d.code_stub,
                Description = d.description,
                Note1 = d.note1,
                Amount = d.amount,
                PriceUnit = d.price_unit,
                Discount = d.discount,
                IvaPorc = d.iva_porc,
                IvaValor = d.iva_valor,
                Total = d.total
            }).ToList(),

            // Mapeo de Pagos
            Payments = invoice.InvoicePayments.Select(p => new InvoicePaymentDTO
            {
                PaymentId = p.id_payment,
                Total = p.total,
                Deadline = p.deadline,
                UnitTime = p.unit_time
            }).ToList()
        };

        return invoiceDto;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex MyRegex();
}