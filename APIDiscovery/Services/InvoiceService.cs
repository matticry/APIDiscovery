using APIDiscovery.Core;
using APIDiscovery.Exceptions;
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
        try
        {
            // 1. Validaciones iniciales
            if (invoiceDto == null)
                throw new BusinessException("No se proporcionó información de la factura");

            if (invoiceDto.Details == null || !invoiceDto.Details.Any())
                throw new BusinessException("La factura debe tener al menos un detalle");

            // 2. Validar existencia de Enterprise, Branch, EmissionPoint y Sequence
            var enterprise = await _context.Enterprises
                                 .FirstOrDefaultAsync(e => e.id_en == invoiceDto.Enterprise.IdEnterprise)
                             ?? throw new EntityNotFoundException(
                                 $"Empresa con ID {invoiceDto.Enterprise.IdEnterprise} no encontrada");

            var branch = await _context.Branches
                             .FirstOrDefaultAsync(b => b.id_br == invoiceDto.Branch.IdBranch)
                         ?? throw new EntityNotFoundException(
                             $"Sucursal con ID {invoiceDto.Branch.IdBranch} no encontrada");

            var emissionPoint = await _context.EmissionPoints
                                    .FirstOrDefaultAsync(e => e.id_e_p == invoiceDto.EmissionPoint.IdEmissionPoint)
                                ?? throw new EntityNotFoundException(
                                    $"Punto de emisión con ID {invoiceDto.EmissionPoint.IdEmissionPoint} no encontrado");

            var sequence = await _context.Sequences
                               .FirstOrDefaultAsync(s => s.id_sequence == invoiceDto.Sequence.IdSequence)
                           ?? throw new EntityNotFoundException(
                               $"Secuencia con ID {invoiceDto.Sequence.IdSequence} no encontrada");

            string nextSequenceNumber;

            var lastInvoice = await _context.Invoices
                .Where(i => i.sequence_id == sequence.id_sequence)
                .OrderByDescending(i => i.inv_id)
                .FirstOrDefaultAsync();

            if (lastInvoice != null && !string.IsNullOrEmpty(lastInvoice.sequence))
            {
                var lastSequenceValue = int.Parse(lastInvoice.sequence);
                nextSequenceNumber = (lastSequenceValue + 1).ToString().PadLeft(lastInvoice.sequence.Length, '0');
            }
            else
            {
                var initialSequenceValue = int.Parse(sequence.code);
                nextSequenceNumber = (initialSequenceValue + 1).ToString().PadLeft(sequence.code.Length, '0');
            }

            var documentType = await _context.DocumentTypes
                                   .FirstOrDefaultAsync(d => d.id_d_t == invoiceDto.DocumentType.IdDocumentType)
                               ?? throw new EntityNotFoundException(
                                   $"Tipo de documento con ID {invoiceDto.DocumentType.IdDocumentType} no encontrado");

            // LÓGICA DE CLIENTE MODIFICADA
            Client clientEntity;

            // Si el monto es mayor o igual a $50, los datos del cliente son OBLIGATORIOS
            if (invoiceDto.TotalAmount >= 50)
            {
                if (invoiceDto.Client == null)
                    throw new BusinessException("Para montos de $50 o más debe enviar datos del adquirente");

                // Buscar cliente existente por DNI
                clientEntity = await _context.Clients.FirstOrDefaultAsync(c => c.dni == invoiceDto.Client.Dni);

                if (clientEntity == null)
                {
                    // Crear nuevo cliente
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
                }
                else
                {
                    // Actualizar información del cliente existente
                    clientEntity.razon_social = invoiceDto.Client.RazonSocial;
                    clientEntity.address = invoiceDto.Client.Address;
                    clientEntity.phone = invoiceDto.Client.Phone;
                    clientEntity.email = invoiceDto.Client.Email;
                    clientEntity.info = invoiceDto.Client.Info;
                    clientEntity.id_type_dni = invoiceDto.Client.TypeDniId;

                    _context.Clients.Update(clientEntity);
                }

                await _context.SaveChangesAsync();
            }
            else
            {
                // Para montos menores a $50, los datos del cliente son OPCIONALES
                if (invoiceDto.Client != null && !string.IsNullOrEmpty(invoiceDto.Client.Dni))
                {
                    // Si se enviaron datos del cliente, usarlos
                    clientEntity = await _context.Clients.FirstOrDefaultAsync(c => c.dni == invoiceDto.Client.Dni);

                    if (clientEntity == null)
                    {
                        // Crear nuevo cliente
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
                    }
                    else
                    {
                        // Actualizar información del cliente existente
                        clientEntity.razon_social = invoiceDto.Client.RazonSocial;
                        clientEntity.address = invoiceDto.Client.Address;
                        clientEntity.phone = invoiceDto.Client.Phone;
                        clientEntity.email = invoiceDto.Client.Email;
                        clientEntity.info = invoiceDto.Client.Info;
                        clientEntity.id_type_dni = invoiceDto.Client.TypeDniId;

                        _context.Clients.Update(clientEntity);
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Si no se enviaron datos del cliente, usar CONSUMIDOR FINAL
                    clientEntity = await _context.Clients.FirstOrDefaultAsync(c => c.dni == "9999999999999");

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
            }

            var branchCode = branch.code?.PadLeft(3, '0');
            var emissionPointCode = emissionPoint.code.PadLeft(3, '0');
            var serie = branchCode + emissionPointCode;
            var ambiente = enterprise.environment;
            var tipoComprobante = documentType.id_d_t.ToString("D2");
            var ruc = enterprise.ruc?.PadLeft(13, '0');
            var numeroFactura = nextSequenceNumber.PadLeft(9, '0');
            var codigoNumerico = GenerarCodigoNumerico();
            var tipoEmision = "1"; // Emisión normal

            var claveAcceso = GenerarClaveAcceso(
                invoiceDto.EmissionDate,
                tipoComprobante,
                ruc,
                ambiente.ToString(),
                serie,
                numeroFactura,
                codigoNumerico,
                tipoEmision
            );

            decimal totalSinImpuestos = 0;
            decimal totalDescuento = 0;
            decimal totalImpuestos = 0;

            var detallesCalculados = new List<(
                int ArticleId,
                int TariffId,
                decimal Amount,
                decimal PrecioUnitario,
                decimal Descuento,
                decimal Neto,
                decimal IvaPorc,
                decimal IvaValor,
                string Note1,
                string Note2,
                string Note3)>();

            foreach (var detailDto in invoiceDto.Details)
            {
                var article = await _context.Articles
                                  .FirstOrDefaultAsync(a => a.id_ar == detailDto.ArticleId)
                              ?? throw new EntityNotFoundException(
                                  $"Artículo con ID {detailDto.ArticleId} no encontrado");

                var tariff = await _context.Fares
                                 .FirstOrDefaultAsync(t => t.id_fare == detailDto.TariffId)
                             ?? throw new EntityNotFoundException($"Tarifa con ID {detailDto.TariffId} no encontrada");

                var articleTariff = await _context.TariffArticles
                                        .FirstOrDefaultAsync(at =>
                                            at.id_article == detailDto.ArticleId && at.id_fare == detailDto.TariffId)
                                    ?? throw new BusinessException(
                                        $"No existe relación entre el artículo {detailDto.ArticleId} y la tarifa {detailDto.TariffId}");

                var fare = await _context.Fares
                               .FirstOrDefaultAsync(f => f.id_fare == articleTariff.id_fare)
                           ?? throw new EntityNotFoundException($"Tarifa con ID {detailDto.TariffId} no encontrada");

                // NUEVA LÓGICA DE CÁLCULO CON IncludeVat
                var precioOriginal = article.price_unit;
                var cantidad = detailDto.Amount;
                var descuentoPorcentaje = detailDto.Discount;
                var ivaPorc = fare.percentage;

                decimal precioUnitario;
                decimal subtotal;
                decimal ivaValor;
                decimal descuentoMonetario;

                if (article.include_vat == 'I') // IVA INCLUIDO
                {
                    // El precio ya incluye IVA, necesitamos extraer el valor base
                    var factorIva = 1 + ivaPorc / 100;
                    precioUnitario = Math.Round(precioOriginal / factorIva, 4); // Precio sin IVA

                    // Calcular descuento sobre el precio sin IVA
                    descuentoMonetario = Math.Round(precioUnitario * cantidad * (descuentoPorcentaje / 100), 2);

                    // Subtotal sin IVA después del descuento
                    subtotal = Math.Round(cantidad * precioUnitario - descuentoMonetario, 2);

                    // IVA calculado sobre el subtotal
                    ivaValor = Math.Round(subtotal * (ivaPorc / 100), 2);

                    Console.WriteLine(
                        $"Artículo {article.name} - IVA INCLUIDO: Precio original: {precioOriginal:C2}, Precio base: {precioUnitario:C2}, IVA: {ivaValor:C2}");
                }
                else if (article.include_vat == 'E') // IVA EXCLUIDO
                {
                    // El precio NO incluye IVA, cálculo tradicional
                    precioUnitario = precioOriginal;

                    // Calcular descuento sobre el precio sin IVA
                    descuentoMonetario = Math.Round(precioUnitario * cantidad * (descuentoPorcentaje / 100), 2);

                    // Subtotal sin IVA después del descuento
                    subtotal = Math.Round(cantidad * precioUnitario - descuentoMonetario, 2);

                    // IVA calculado sobre el subtotal
                    ivaValor = Math.Round(subtotal * (ivaPorc / 100), 2);

                    Console.WriteLine(
                        $"Artículo {article.name} - IVA EXCLUIDO: Precio base: {precioUnitario:C2}, IVA: {ivaValor:C2}");
                }
                else
                {
                    throw new BusinessException(
                        $"Valor de IncludeVat no válido para el artículo {article.name}. Debe ser 'I' (Incluido) o 'E' (Excluido). Valor actual: {article.include_vat}");
                }

                totalSinImpuestos += subtotal;
                totalDescuento += descuentoMonetario;
                totalImpuestos += ivaValor;

                if (detailDto.TariffId != null)
                    detallesCalculados.Add(((int ArticleId, int TariffId, decimal Amount, decimal PrecioUnitario, decimal Descuento, decimal Neto, decimal IvaPorc, decimal IvaValor, string Note1, string Note2, string Note3))(
                        detailDto.ArticleId,
                        detailDto.TariffId,
                        cantidad,
                        precioUnitario, // Este será el precio base (sin IVA)
                        descuentoMonetario,
                        subtotal,
                        ivaPorc,
                        ivaValor,
                        detailDto.Note1,
                        detailDto.Note2,
                        detailDto.Note3
                    ));
            }

            var propina = invoiceDto.Tip;
            var importeTotal = Math.Round(totalSinImpuestos + totalImpuestos + propina, 2);
            var invoice = new Invoice
            {
                emission_date = invoiceDto.EmissionDate,
                total_without_taxes = Math.Round(totalSinImpuestos, 2),
                total_discount = Math.Round(totalDescuento, 2),
                tip = propina,
                total_amount = importeTotal,
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
                sequence = nextSequenceNumber
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var detalle in detallesCalculados)
            {
                var article = await _context.Articles.FirstOrDefaultAsync(a => a.id_ar == detalle.ArticleId);

                if (article == null)
                    continue;

                switch (article.type)
                {
                    case 'N' when article.stock < detalle.Amount:
                        throw new BusinessException(
                            $"No hay suficiente stock para el artículo {article.name}. Stock disponible: {article.stock}, Cantidad solicitada: {detalle.Amount}");
                    case 'N':
                        article.stock -= (int)detalle.Amount;
                        _context.Articles.Update(article);

                        Console.WriteLine(
                            $"Stock actualizado para artículo {article.name}: {article.stock + (int)detalle.Amount} -> {article.stock}");
                        break;
                    case 'S':
                        Console.WriteLine($"Artículo de servicio {article.name} - No se actualiza stock");
                        break;
                    default:
                        throw new BusinessException(
                            $"Tipo de artículo no válido para {article.name}. Tipo actual: {article.type}");
                }

                var detail = new InvoiceDetail
                {
                    code_stub = article.code,
                    description = article.description,
                    amount = (int)detalle.Amount,
                    price_unit = detalle.PrecioUnitario,
                    discount = detalle.Descuento,
                    neto = detalle.Neto,
                    iva_porc = detalle.IvaPorc,
                    iva_valor = detalle.IvaValor,
                    ice_porc = 0,
                    ice_valor = 0,
                    subtotal = detalle.Neto,
                    total = detalle.Neto + detalle.IvaValor,
                    note1 = detalle.Note1,
                    note2 = detalle.Note2,
                    note3 = detalle.Note3,
                    id_tariff = detalle.TariffId,
                    id_article = detalle.ArticleId,
                    id_invoice = invoice.inv_id
                };

                _context.InvoiceDetails.Add(detail);
            }

            foreach (var paymentDto in invoiceDto.Payments)
            {
                var paymentMethod = await _context.Payments
                                        .FirstOrDefaultAsync(p => p.id_payment == paymentDto.PaymentId)
                                    ?? throw new EntityNotFoundException(
                                        $"Método de pago con ID {paymentDto.PaymentId} no encontrado");

                var payment = new InvoicePayment
                {
                    id_invoice = invoice.inv_id,
                    total = paymentDto.Total,
                    id_payment = paymentDto.PaymentId
                };

                if (paymentMethod.sri_detail != "01")
                {
                    payment.deadline = paymentDto.Deadline;
                    payment.unit_time = paymentDto.UnitTime;
                }

                _context.InvoicePayments.Add(payment);
            }

            await _context.SaveChangesAsync();

            return await GetInvoiceDtoById(invoice.inv_id);
        }
        catch (EntityNotFoundException ex)
        {
            throw;
        }
        catch (BusinessException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al crear la factura: {ex.Message}", ex);
        }
    }


    public async Task<List<InvoiceDTO>> GetAuthorizedInvoicesByEnterpriseId(int enterpriseId)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Branch)
            .Include(i => i.Sequence)
            .Include(i => i.DocumentType)
            .Include(i => i.Enterprise)
            .Include(i => i.EmissionPoint)
            .Include(i => i.InvoiceDetails)
            .Include(i => i.InvoicePayments)
            .Where(i => i.company_id == enterpriseId && i.invoice_status == "AUTORIZADO")
            .ToListAsync();

        if (invoices.Count == 0)
            throw new EntityNotFoundException(
                $"No se encontraron facturas no autorizadas para la empresa con ID {enterpriseId}");

        return invoices.Select(invoice => new InvoiceDTO
            {
                InvoiceId = invoice.inv_id,
                EmissionDate = invoice.emission_date,
                TotalAmount = invoice.total_amount,
                sequenceCode = invoice.sequence,
                TotalWithoutTaxes = invoice.total_without_taxes,
                TotalDiscount = invoice.total_discount,
                Tip = invoice.tip,
                Currency = invoice.currency,
                AccessKey = invoice.access_key,
                ElectronicStatus = invoice.electronic_status,
                InvoiceStatus = invoice.invoice_status,
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
                    IdBranch = invoice.Branch.id_br,
                    Code = invoice.Branch.code,
                    Description = invoice.Branch.description,
                    Address = invoice.Branch.address,
                    Phone = invoice.Branch.phone
                },
                Sequence = new SequenceDTO { IdSequence = invoice.Sequence.id_sequence, Code = invoice.Sequence.code },
                DocumentType = new DocumentTypeDTO
                    { IdDocumentType = invoice.DocumentType.id_d_t, NameDocument = invoice.DocumentType.name_document },
                Enterprise = new EnterpriseDTO
                {
                    IdEnterprise = invoice.Enterprise.id_en,
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
                    IdEmissionPoint = invoice.EmissionPoint.id_e_p, Code = invoice.EmissionPoint.code,
                    Details = invoice.EmissionPoint.details
                },
                Details = invoice.InvoiceDetails.Select(d => new InvoiceDetailDTO
                    {
                        CodeStub = d.code_stub,
                        Description = d.description,
                        Amount = d.amount,
                        PriceUnit = d.price_unit,
                        Discount = d.discount,
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
                        ArticleId = d.id_article,
                        TariffId = d.id_tariff
                    })
                    .ToList(),
                Payments = invoice.InvoicePayments.Select(p => new InvoicePaymentDTO
                        { Total = p.total, Deadline = p.deadline, UnitTime = p.unit_time, PaymentId = p.id_payment })
                    .ToList()
            })
            .ToList();
    }

    public async Task<List<InvoiceSummaryDTO>> GetTopInvoicesByCompanyIdAsync(int companyId, int count = 3)
    {
        try
        {
            var enterpriseExists = await _context.Enterprises.AnyAsync(e => e.id_en == companyId);
            if (!enterpriseExists) throw new EntityNotFoundException($"Empresa con ID {companyId} no encontrada");

            // Obtener las primeras N facturas ordenadas por fecha de emisión (o ID si prefieres)
            var topInvoices = await _context.Invoices
                .Include(i => i.Client)
                .Include(i => i.Sequence)
                .Where(i => i.company_id == companyId)
                .OrderByDescending(i => i.emission_date)
                .Take(count)
                .Select(i => new InvoiceSummaryDTO
                {
                    InvoiceId = i.inv_id,
                    EmissionDate = i.emission_date,
                    Status = i.invoice_status,
                    ElectronicStatus = i.electronic_status,
                    TotalAmount = i.total_amount,
                    ClientName = i.Client.razon_social,
                    ClientDni = i.Client.dni,
                    SequenceNumber = i.sequence
                })
                .ToListAsync();

            if (topInvoices.Count == 0)
                throw new EntityNotFoundException($"No se encontraron facturas para la empresa con ID {companyId}");

            return topInvoices;
        }
        catch (EntityNotFoundException ex)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener las facturas: {ex.Message}", ex);
        }
    }

    public async Task<string> GetXmlBase64ByInvoiceId(int invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null) throw new EntityNotFoundException($"Factura con ID {invoiceId} no encontrada");

        return invoice.XmlBase64 ?? string.Empty;
    }

    public async Task<int> GetTotalInvoiceUnAuthorizedCountByCompanyIdAsync(int companyId)
    {
        try
        {
            // Verificar si la empresa existe
            var enterpriseExists = await _context.Enterprises.AnyAsync(e => e.id_en == companyId);
            if (!enterpriseExists) throw new EntityNotFoundException($"Empresa con ID {companyId} no encontrada");

            var totalInvoices = await _context.Invoices
                .Where(i => i.company_id == companyId && i.invoice_status == "NO AUTORIZADO")
                .CountAsync();

            return totalInvoices;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener el conteo de facturas: {ex.Message}", ex);
        }
    }

    public async Task<decimal> GetTotalInvoiceAmountByCompanyIdAsync(int companyId)
    {
        try
        {
            var enterpriseExists = await _context.Enterprises.AnyAsync(e => e.id_en == companyId);
            if (!enterpriseExists) throw new EntityNotFoundException($"Empresa con ID {companyId} no encontrada");

            var totalAmount = await _context.Invoices
                .Where(i => i.company_id == companyId)
                .SumAsync(i => i.total_amount);

            return totalAmount;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener el monto total facturado: {ex.Message}", ex);
        }
    }

    public async Task<int> GetTotalInvoiceCountByCompanyIdAsync(int companyId)
    {
        try
        {
            var enterpriseExists = await _context.Enterprises.AnyAsync(e => e.id_en == companyId);
            if (!enterpriseExists) throw new EntityNotFoundException($"Empresa con ID {companyId} no encontrada");

            var totalInvoices = await _context.Invoices
                .Where(i => i.company_id == companyId)
                .CountAsync();

            return totalInvoices;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener el conteo de facturas: {ex.Message}", ex);
        }
    }

    public async Task<int> GetTotalInvoiceAuthorizedCountByCompanyIdAsync(int companyId)
    {
        try
        {
            // Verificar si la empresa existe
            var enterpriseExists = await _context.Enterprises.AnyAsync(e => e.id_en == companyId);
            if (!enterpriseExists) throw new EntityNotFoundException($"Empresa con ID {companyId} no encontrada");

            var totalInvoices = await _context.Invoices
                .Where(i => i.company_id == companyId && i.invoice_status == "AUTORIZADO")
                .CountAsync();

            return totalInvoices;
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al obtener el conteo de facturas: {ex.Message}", ex);
        }
    }


    public async Task<List<InvoiceDTO>> GetUnauthorizedInvoicesByEnterpriseId(int enterpriseId)
    {
        var invoices = await _context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Branch)
            .Include(i => i.Sequence)
            .Include(i => i.DocumentType)
            .Include(i => i.Enterprise)
            .Include(i => i.EmissionPoint)
            .Include(i => i.InvoiceDetails)
            .Include(i => i.InvoicePayments)
            .Where(i => i.company_id == enterpriseId && i.invoice_status == "NO AUTORIZADO")
            .ToListAsync();

        if (invoices.Count == 0)
            throw new EntityNotFoundException(
                $"No se encontraron facturas no autorizadas para la empresa con ID {enterpriseId}");

        return invoices.Select(invoice => new InvoiceDTO
            {
                InvoiceId = invoice.inv_id,
                EmissionDate = invoice.emission_date,
                sequenceCode = invoice.sequence,
                TotalAmount = invoice.total_amount,
                TotalWithoutTaxes = invoice.total_without_taxes,
                TotalDiscount = invoice.total_discount,
                Tip = invoice.tip,
                Currency = invoice.currency,
                AccessKey = invoice.access_key,
                ElectronicStatus = invoice.electronic_status,
                InvoiceStatus = invoice.invoice_status,
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
                    IdBranch = invoice.Branch.id_br,
                    Code = invoice.Branch.code,
                    Description = invoice.Branch.description,
                    Address = invoice.Branch.address,
                    Phone = invoice.Branch.phone
                },
                Sequence = new SequenceDTO { IdSequence = invoice.Sequence.id_sequence, Code = invoice.Sequence.code },
                DocumentType = new DocumentTypeDTO
                    { IdDocumentType = invoice.DocumentType.id_d_t, NameDocument = invoice.DocumentType.name_document },
                Enterprise = new EnterpriseDTO
                {
                    IdEnterprise = invoice.Enterprise.id_en,
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
                    IdEmissionPoint = invoice.EmissionPoint.id_e_p, Code = invoice.EmissionPoint.code,
                    Details = invoice.EmissionPoint.details
                },
                Details = invoice.InvoiceDetails.Select(d => new InvoiceDetailDTO
                    {
                        CodeStub = d.code_stub,
                        Description = d.description,
                        Amount = d.amount,
                        PriceUnit = d.price_unit,
                        Discount = d.discount,
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
                        ArticleId = d.id_article,
                        TariffId = d.id_tariff
                    })
                    .ToList(),
                Payments = invoice.InvoicePayments.Select(p => new InvoicePaymentDTO
                        { Total = p.total, Deadline = p.deadline, UnitTime = p.unit_time, PaymentId = p.id_payment })
                    .ToList()
            })
            .ToList();
    }

    public async Task<InvoiceDTO> GetInvoiceDtoById(int invoiceId)
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

        if (invoice == null)
            throw new EntityNotFoundException($"Factura con ID {invoiceId} no encontrada");

        var dto = new InvoiceDTO
        {
            InvoiceId = invoice.inv_id,
            EmissionDate = invoice.emission_date,
            TotalAmount = invoice.total_amount,
            TotalWithoutTaxes = invoice.total_without_taxes,
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
                IdBranch = invoice.Branch.id_br,
                Code = invoice.Branch.code,
                Description = invoice.Branch.description,
                Address = invoice.Branch.address,
                Phone = invoice.Branch.phone
            },
            Sequence = new SequenceDTO
            {
                IdSequence = invoice.Sequence.id_sequence,
                Code = invoice.Sequence.code
            },
            DocumentType = new DocumentTypeDTO
            {
                IdDocumentType = invoice.DocumentType.id_d_t,
                NameDocument = invoice.DocumentType.name_document
            },
            Enterprise = new EnterpriseDTO
            {
                IdEnterprise = invoice.Enterprise.id_en,
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
                IdEmissionPoint = invoice.EmissionPoint.id_e_p,
                Code = invoice.EmissionPoint.code,
                Details = invoice.EmissionPoint.details
            },
            Details = invoice.InvoiceDetails.Select(d => new InvoiceDetailDTO
            {
                CodeStub = d.code_stub,
                Description = d.description,
                Amount = d.amount,
                PriceUnit = d.price_unit,
                Discount = d.discount,
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
                ArticleId = d.id_article,
                TariffId = d.id_tariff
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

    private static string GenerarClaveAcceso(DateTime fechaEmision, string tipoComprobante, string ruc, string ambiente,
        string serie, string numeroFactura, string codigoNumerico, string tipoEmision)
    {
        var fecha = fechaEmision.ToString("ddMMyyyy");
        var claveSinDigito = fecha + tipoComprobante + ruc + ambiente + serie + numeroFactura + codigoNumerico +
                             tipoEmision;

        var digitoVerificador = CalcularDigitoVerificadorModulo11(claveSinDigito);

        return claveSinDigito + digitoVerificador;
    }

    private string GenerarCodigoNumerico()
    {
        var random = new Random();
        return random.Next(0, 99999999).ToString("D8");
    }

    private static int CalcularDigitoVerificadorModulo11(string clave)
    {
        int[] pesos = { 2, 3, 4, 5, 6, 7 };
        var suma = 0;
        var pesoIndex = 0;

        for (var i = clave.Length - 1; i >= 0; i--)
        {
            var valor = int.Parse(clave[i].ToString());
            suma += valor * pesos[pesoIndex];
            pesoIndex = (pesoIndex + 1) % pesos.Length;
        }

        var residuo = suma % 11;
        var digito = 11 - residuo;

        if (digito == 11) digito = 0;
        else if (digito == 10) digito = 1;

        return digito;
    }
}