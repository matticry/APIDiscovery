using APIDiscovery.Core;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using APIDiscovery.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]

public class InvoiceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InvoiceController> _logger;
    private readonly InvoicePdfGenerator _pdfGenerator;

    public InvoiceController(
        ApplicationDbContext context,
        InvoicePdfGenerator pdfGenerator,
        ILogger<InvoiceController> logger)
    {
        _context = context;
        _pdfGenerator = pdfGenerator;
        _logger = logger;
    }


    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GeneratePdf(int id)
    {
        try
        {
            _logger.LogInformation("Generando PDF para factura con ID: {InvoiceId}", id);

            var invoice = await _context.Invoices
                .Include(i => i.Enterprise)
                .Include(i => i.Branch)
                .Include(i => i.EmissionPoint)
                .Include(i => i.Client)
                .Include(i => i.InvoiceDetails)
                .Include(i => i.InvoicePayments)
                .Include(i => i.Sequence)
                .FirstOrDefaultAsync(i => i.inv_id == id);

            if (invoice == null)
            {
                _logger.LogWarning("Factura con ID {InvoiceId} no encontrada", id);
                return NotFound($"No se encontró la factura con ID {id}");
            }

            // Mapear la entidad de la base de datos al DTO
            var invoiceDto = MapToDto(invoice);
            if (string.IsNullOrEmpty(invoiceDto.AccessKey) || invoiceDto.AccessKey == "string")
                _logger.LogWarning("La clave de acceso para la factura {InvoiceId} no es válida", id);
            var pdfBytes = _pdfGenerator.GenerateInvoicePdf(invoiceDto);

            // Devolver el PDF como un archivo para descargar
            var fileName = $"Factura_{invoiceDto.Enterprise.Ruc}_{invoiceDto.Sequence.Code}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar PDF para factura {InvoiceId}", id);
            return StatusCode(500, $"Error interno al generar el PDF: {ex.Message}");
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

            Branch = new BranchDTO
            {
                IdBranch = invoice.Branch.id_br,
                Code = invoice.Branch.code,
                Address = invoice.Branch.address,
                Description = invoice.Branch.description
            },

            EmissionPoint = new EmissionPointDTO
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
                Note1 = d.note1 ?? string.Empty,
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
}