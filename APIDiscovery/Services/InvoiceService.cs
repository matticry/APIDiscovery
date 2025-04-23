using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class InvoiceService : IInvoiceService
{
    
    private readonly ApplicationDbContext _context;

    public InvoiceService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    
    public async Task<InvoiceDTO> CreateInvoiceAsync(InvoiceDTO invoiceDto)
    {
        if (invoiceDto == null || invoiceDto.Details == null || !invoiceDto.Details.Any())
            throw new ArgumentException("La factura debe tener al menos un detalle.");

        bool isConsumerFinal = invoiceDto.TotalAmount <= 50;

        Client clientEntity;

        if (isConsumerFinal)
        {
            clientEntity = await _context.Clients.FirstOrDefaultAsync(c => c.dni == "9999999999999") ?? throw new InvalidOperationException();
            
            if (clientEntity == null)
            {
                clientEntity = new Client
                {
                    razon_social = "CONSUMIDOR FINAL",
                    dni = "9999999999999",
                    address = "CONSUMIDOR FINAL",
                    phone = "099999999",
                    email = "consumidorfinal@email.com",
                    info = "Factura generada para consumidor final",
                    id_type_dni = 7
                };
                _context.Clients.Add(clientEntity);
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            if (invoiceDto.Client == null)
                throw new ArgumentException("Para montos mayores a $50 debe enviar datos del adquirente.");

            clientEntity = new Client
            {
                razon_social = invoiceDto.Client.RazonSocial,
                dni = invoiceDto.Client.Dni,
                address = invoiceDto.Client.Address,
                phone = invoiceDto.Client.Phone,
                email = invoiceDto.Client.Email,
                info = invoiceDto.Client.Info,
                id_type_dni = invoiceDto.Client.TypeDniId
            };
            _context.Clients.Add(clientEntity);
            await _context.SaveChangesAsync();
        }
        var branchCode = await _context.Branches
            .Where(b => b.id_br == invoiceDto.Branch.IdBranch)
            .Select(b => b.code)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(branchCode))
            throw new Exception($"Código de sucursal no encontrado para ID {invoiceDto.Branch.IdBranch}");

        branchCode = branchCode.PadLeft(3, '0');

        var emissionPointCode = await _context.EmissionPoints
            .Where(e => e.id_e_p == invoiceDto.EmissionPoint.IdEmissionPoint)
            .Select(e => e.code)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(emissionPointCode))
            throw new Exception($"Código de punto de emisión no encontrado para ID {invoiceDto.EmissionPoint.IdEmissionPoint}");


        emissionPointCode = emissionPointCode.PadLeft(3, '0');

        string serie = branchCode + emissionPointCode;

        var ambiente = "1"; 
        var tipoComprobante = invoiceDto.DocumentType.IdDocumentType.ToString("D2");
        var ruc = invoiceDto.Enterprise.Ruc.PadLeft(13, '0');
        var numeroFactura = invoiceDto.Sequence.Code.PadLeft(9, '0');
        var codigoNumerico = GenerarCodigoNumerico();
        var tipoEmision = "1";

        var claveAcceso = GenerarClaveAcceso(invoiceDto.EmissionDate, tipoComprobante, ruc, ambiente, serie, numeroFactura, codigoNumerico, tipoEmision);
        var invoice = new Invoice
        {
            emission_date = invoiceDto.EmissionDate,
            total_without_taxes = invoiceDto.TotalWithoutTaxes,
            total_discount = invoiceDto.TotalDiscount,
            tip = invoiceDto.Tip,
            total_amount = invoiceDto.TotalAmount,
            currency = invoiceDto.Currency,
            sequence_id = invoiceDto.Sequence.IdSequence,
            id_emission_point = invoiceDto.EmissionPoint.IdEmissionPoint,
            company_id = invoiceDto.Enterprise.IdEnterprise,
            client_id = clientEntity.id_client,
            branch_id = invoiceDto.Branch.IdBranch,
            receipt_id = invoiceDto.DocumentType.IdDocumentType,
            electronic_status = "PENDIENTE",
            access_key = claveAcceso,
            authorization_number = invoiceDto.AuthorizationNumber,
            authorization_date = invoiceDto.AuthorizationDate,
            additional_info = invoiceDto.AdditionalInfo,
            message = invoiceDto.Message,
            identifier = invoiceDto.Client?.Dni ?? "9999999999999",
            type = invoiceDto.Type
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        foreach (var detailDto in invoiceDto.Details)
        {
            var detail = new InvoiceDetail
            {
                code_stub = detailDto.CodeStub,
                description = detailDto.Description,
                amount = detailDto.Amount,
                price_unit = detailDto.PriceUnit,
                discount = detailDto.Discount, 
                price_with_discount = detailDto.PriceWithDiscount,
                neto = detailDto.Neto,
                iva_porc = detailDto.IvaPorc,
                iva_valor = detailDto.IvaValor,
                ice_porc = detailDto.IcePorc,
                ice_valor = detailDto.IceValor,
                subtotal = detailDto.Subtotal,
                total = detailDto.Total,
                note1 = detailDto.Note1,
                note2 = detailDto.Note2,
                note3 = detailDto.Note3,
                id_tariff = detailDto.TariffId,  
                id_article = detailDto.ArticleId, 
                id_invoice = invoice.inv_id
            };
            _context.InvoiceDetails.Add(detail);
        }

        foreach (var paymentDto in invoiceDto.Payments)
        {
            var payment = new InvoicePayment
            {
                id_invoice = invoice.inv_id,
                total = paymentDto.Total,
                deadline = paymentDto.Deadline,
                unit_time = paymentDto.UnitTime,
                id_payment = paymentDto.PaymentId
            };
            _context.InvoicePayments.Add(payment);
        }

        await _context.SaveChangesAsync();

        invoiceDto = await GetInvoiceDtoById(invoice.inv_id);

        return invoiceDto;
    }
    
    public static string GenerarClaveAcceso(DateTime fechaEmision, string tipoComprobante, string ruc, string ambiente,
        string serie, string numeroFactura, string codigoNumerico, string tipoEmision)
    {
        string fecha = fechaEmision.ToString("ddMMyyyy");
        string claveSinDigito = fecha + tipoComprobante + ruc + ambiente + serie + numeroFactura + codigoNumerico + tipoEmision;

        int digitoVerificador = CalcularDigitoVerificadorModulo11(claveSinDigito);

        return claveSinDigito + digitoVerificador.ToString();
    }
    
    private string GenerarCodigoNumerico()
    {
        var random = new Random();
        return random.Next(0, 99999999).ToString("D8");
    }

    private static int CalcularDigitoVerificadorModulo11(string clave)
    {
        int[] pesos = { 2, 3, 4, 5, 6, 7 };
        int suma = 0;
        int pesoIndex = 0;

        for (int i = clave.Length - 1; i >= 0; i--)
        {
            int valor = int.Parse(clave[i].ToString());
            suma += valor * pesos[pesoIndex];
            pesoIndex = (pesoIndex + 1) % pesos.Length;
        }

        int residuo = suma % 11;
        int digito = 11 - residuo;

        if (digito == 11) digito = 0;
        else if (digito == 10) digito = 1;

        return digito;
    }
    
    private async Task<InvoiceDTO> GetInvoiceDtoById(int invoiceId)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Branch)
            .Include(i => i.Sequence)
            .Include(i => i.DocumentType)
            .Include(i => i.Enterprise)
            .Include(i => i.EmissionPoint)
            .Include(i => i.InvoiceDetails)
            .Include(i => i.InvoicePayments)
            .FirstOrDefaultAsync(i => i.inv_id == invoiceId);

        if (invoice == null) return null;

        var dto = new InvoiceDTO
        {
            EmissionDate = invoice.emission_date,
            TotalAmount = invoice.total_amount,
            TotalDiscount = invoice.total_discount,
            Tip = invoice.tip,
            Currency = invoice.currency,
            AccessKey = invoice.access_key,
            ElectronicStatus = invoice.electronic_status,
            AuthorizationNumber = invoice.authorization_number,
            AuthorizationDate = invoice.authorization_date,
            AdditionalInfo = invoice.additional_info,
            Message = invoice.message,
            Client = new ClientDTO
            {
                RazonSocial = invoice.Client.razon_social,
                Dni = invoice.Client.dni,
                Address = invoice.Client.address,
                Phone = invoice.Client.phone,
                Email = invoice.Client.email,
                TypeDniId = invoice.Client.id_type_dni
            },
            Branch = new BranchDTO
            {
                Code = invoice.Branch.code,
                Description = invoice.Branch.description,
                Address = invoice.Branch.address,
                Phone = invoice.Branch.phone
            },
            Sequence = new SequenceDTO
            {
                Code = invoice.Sequence.code
            },
            DocumentType = new DocumentTypeDTO
            {
                NameDocument = invoice.DocumentType.name_document
            },
            Enterprise = new EnterpriseDTO
            {
                CompanyName = invoice.Enterprise.company_name,
                ComercialName = invoice.Enterprise.comercial_name,
                Ruc = invoice.Enterprise.ruc,
                AddressMatriz = invoice.Enterprise.address_matriz,
                Phone = invoice.Enterprise.phone,
                Email = invoice.Enterprise.email,
                Accountant = invoice.Enterprise.accountant
            },
            EmissionPoint = new EmissionPointDTO
            {
                Code = invoice.EmissionPoint.code,
                Details = invoice.EmissionPoint.details
            },
            Details = invoice.InvoiceDetails.Select(d => new InvoiceDetailDTO
            {
                CodeStub = d.code_stub,
                Description = d.description,
                Amount = d.amount,
                PriceUnit = d.price_unit,
                PriceWithDiscount = d.price_with_discount,
                Neto = d.neto,
                IvaPorc = d.iva_porc,
                IvaValor = d.iva_valor,
                IcePorc = d.ice_porc,
                IceValor = d.ice_valor,
                Subtotal = d.subtotal,
                Total = d.total,
                Note1 = d.note1,
                Note2 = d.note2,
                Note3 = d.note3,
                
            }).ToList(),
            Payments = invoice.InvoicePayments.Select(p => new InvoicePaymentDTO
            {
                Total = p.total,
                Deadline = p.deadline,
                UnitTime = p.unit_time,
                PaymentId = p.id_payment
            }).ToList()
        };

        return dto;
    }

    
}