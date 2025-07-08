using APIDiscovery.Core;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs.CreditNoteDTOs;
using APIDiscovery.Models.DTOs.InvoiceDTOs;
using APIDiscovery.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class CreditNoteService : ICreditNoteService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreditNoteService> _logger;
    private readonly IXmlCreditNoteService _xmlCreditNoteService;

    public CreditNoteService(ApplicationDbContext context, IXmlCreditNoteService xmlCreditNoteService,
        ILogger<CreditNoteService> logger)
    {
        _context = context;
        _xmlCreditNoteService = xmlCreditNoteService;
        _logger = logger;
    }

    public async Task<CreditNoteDTO> CreateCreditNoteAsync(CreateCreditNoteDTO creditNoteDto)
    {
        try
        {
            // 1. Validaciones iniciales
            if (creditNoteDto == null)
                throw new BusinessException("No se proporcionó información de la nota de crédito");

            // 2. Obtener la factura original con todas sus relaciones
            var originalInvoice = await _context.Invoices
                                      .Include(i => i.Enterprise)
                                      .Include(i => i.Branch)
                                      .Include(i => i.EmissionPoint)
                                      .Include(i => i.Client)
                                      .Include(i => i.DocumentType)
                                      .Include(i => i.Sequence)
                                      .Include(i => i.InvoiceDetails)
                                      .ThenInclude(d => d.Article)
                                      .Include(i => i.InvoiceDetails)
                                      .ThenInclude(d => d.Tariff)
                                      .FirstOrDefaultAsync(i => i.inv_id == creditNoteDto.InvoiceOriginalId)
                                  ?? throw new EntityNotFoundException(
                                      $"Factura con ID {creditNoteDto.InvoiceOriginalId} no encontrada");

            // 3. Validar que la factura esté autorizada
            if (originalInvoice.electronic_status != "AUTORIZADO")
                throw new BusinessException("Solo se pueden crear notas de crédito para facturas autorizadas");

            if (originalInvoice.Client.dni == "9999999999999" ||
                originalInvoice.Client.razon_social.ToUpper()
                    .Contains("CONSUMIDOR FINAL", StringComparison.CurrentCultureIgnoreCase))
                throw new BusinessException(
                    "No se pueden crear notas de crédito para facturas emitidas a CONSUMIDOR FINAL. " +
                    "Las notas de crédito requieren datos específicos del cliente.");

            // 4. Obtener la secuencia para notas de crédito (tipo documento 04)
            var creditNoteDocumentType = await _context.DocumentTypes
                                             .FirstOrDefaultAsync(d => d.code == "04")
                                         ?? throw new EntityNotFoundException(
                                             "Tipo de documento para nota de crédito no encontrado");

            var creditNoteSequence = await _context.Sequences
                                         .FirstOrDefaultAsync(s => s.id_document_type == creditNoteDocumentType.id_d_t)
                                     ?? throw new EntityNotFoundException(
                                         "Secuencia para nota de crédito no encontrada");

            // 5. Generar número de secuencia
            var nextSequenceNumber = await GenerateNextSequenceNumber(creditNoteSequence.id_sequence);

            // 6. Generar clave de acceso
            var claveAcceso = GenerarClaveAcceso(
                DateTime.Now,
                "04", // Código para nota de crédito
                originalInvoice.Enterprise.ruc?.PadLeft(13, '0'),
                originalInvoice.Enterprise.environment.ToString(),
                $"{originalInvoice.Branch.code?.PadLeft(3, '0')}{originalInvoice.EmissionPoint.code.PadLeft(3, '0')}",
                nextSequenceNumber.PadLeft(9, '0'),
                GenerarCodigoNumerico(),
                "1" // Emisión normal
            );

            // 7. Procesar según el tipo de nota de crédito
            var (details, totalSinImpuestos, totalDescuento, totalImpuestos, valorModificacion) =
                await ProcessCreditNoteDetails(creditNoteDto, originalInvoice);

            // 8. Crear la nota de crédito
            var creditNote = new CreditNote
            {
                EmissionDate = DateTime.Now,
                TotalWithoutTaxes = Math.Round(totalSinImpuestos, 2),
                TotalDiscount = Math.Round(totalDescuento, 2),
                Tip = 0, // Las notas de crédito no manejan propinas
                TotalAmount = Math.Round(totalSinImpuestos + totalImpuestos, 2),
                Currency = originalInvoice.currency,
                CodDocModificado = "01", // Código para factura
                NumDocModificado =
                    $"{originalInvoice.Branch.code?.PadLeft(3, '0')}-{originalInvoice.EmissionPoint.code.PadLeft(3, '0')}-{originalInvoice.sequence}",
                EmissionDateDocSustento = originalInvoice.emission_date,
                ModificationValue = Math.Round(valorModificacion, 2),
                Motive = creditNoteDto.Motive,
                AccessKey = claveAcceso,
                AuthorizationNumber = "", // Se llenará cuando se autorice
                AuthorizationDate = null,
                ElectronicStatus = "PENDIENTE",
                AditionalInfo = creditNoteDto.AdditionalInfo,
                Message = creditNoteDto.Message,
                Sequence = nextSequenceNumber,
                SequenceId = creditNoteSequence.id_sequence,
                IdEmissionPoint = originalInvoice.id_emission_point,
                CompanyId = originalInvoice.company_id,
                ClientId = originalInvoice.client_id,
                BranchId = originalInvoice.branch_id,
                ReceiptId = creditNoteDocumentType.id_d_t,
                InvoiceOriginalId = originalInvoice.inv_id
            };

            _context.CreditNotes.Add(creditNote);
            await _context.SaveChangesAsync();

            // 9. Agregar los detalles
            foreach (var detail in details)
            {
                detail.NoteCreditId = creditNote.IdCreditNote;
                _context.CreditNoteDetails.Add(detail);
            }

            await _context.SaveChangesAsync();

            if (creditNoteDto.CreditNoteType == CreditNoteType.ANULAR_TODA_FACTURA ||
                creditNoteDto.CreditNoteType == CreditNoteType.ANULAR_PRODUCTOS_PARCIAL)
                await ActualizarStockPorDevolucion(details, creditNoteDto.CreditNoteType);

            // 10. Generar XML
            var xml = await GenerateCreditNoteXmlAsync(creditNote.IdCreditNote);
            creditNote.Xml = xml;
            await _context.SaveChangesAsync();

            return await GetCreditNoteDtoById(creditNote.IdCreditNote);
        }
        catch (EntityNotFoundException)
        {
            throw;
        }
        catch (BusinessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error al crear la nota de crédito: {ex.Message}", ex);
        }
    }

    public async Task<CreditNoteDTO> GetCreditNoteDtoById(int creditNoteId)
    {
        var creditNote = await _context.CreditNotes
                             .Include(cn => cn.Enterprise)
                             .Include(cn => cn.Branch)
                             .Include(cn => cn.EmissionPoint)
                             .Include(cn => cn.Client)
                             .Include(cn => cn.DocumentType)
                             .Include(cn => cn.InvoiceOriginal)
                             .Include(cn => cn.CreditNoteDetails)
                             .ThenInclude(d => d.Article)
                             .Include(cn => cn.CreditNoteDetails)
                             .ThenInclude(d => d.Tariff)
                             .FirstOrDefaultAsync(cn => cn.IdCreditNote == creditNoteId)
                         ?? throw new EntityNotFoundException($"Nota de crédito con ID {creditNoteId} no encontrada");

        return new CreditNoteDTO
        {
            IdCreditNote = creditNote.IdCreditNote,
            EmissionDate = creditNote.EmissionDate,
            TotalWithoutTaxes = creditNote.TotalWithoutTaxes,
            TotalDiscount = creditNote.TotalDiscount,
            Tip = creditNote.Tip,
            TotalAmount = creditNote.TotalAmount,
            Currency = creditNote.Currency,
            CodDocModificado = creditNote.CodDocModificado,
            NumDocModificado = creditNote.NumDocModificado,
            EmissionDateDocSustento = creditNote.EmissionDateDocSustento,
            ModificationValue = creditNote.ModificationValue,
            Motive = creditNote.Motive,
            AccessKey = creditNote.AccessKey,
            AuthorizationNumber = creditNote.AuthorizationNumber,
            AuthorizationDate = creditNote.AuthorizationDate,
            ElectronicStatus = creditNote.ElectronicStatus,
            AditionalInfo = creditNote.AditionalInfo,
            Message = creditNote.Message,
            Sequence = creditNote.Sequence,
            Xml = creditNote.Xml,
            Enterprise = new EnterpriseDTO
            {
                IdEnterprise = creditNote.Enterprise.id_en,
                ComercialName = creditNote.Enterprise.comercial_name,
                Ruc = creditNote.Enterprise.ruc
            },
            Branch = new BranchDTO
            {
                IdBranch = creditNote.Branch.id_br,
                Code = creditNote.Branch.code,
                Address = creditNote.Branch.address
            },
            EmissionPoint = new EmissionPointDto
            {
                IdEmissionPoint = creditNote.EmissionPoint.id_e_p,
                Code = creditNote.EmissionPoint.code
            },
            Client = new ClientDTO
            {
                RazonSocial = creditNote.Client.razon_social,
                Dni = creditNote.Client.dni
            },
            DocumentType = new DocumentTypeDTO
            {
                IdDocumentType = creditNote.DocumentType.id_d_t,
                NameDocument = creditNote.DocumentType.name_document,
                Code = creditNote.DocumentType.code
            },
            Details = creditNote.CreditNoteDetails.Select(d => new CreditNoteDetailDTO
            {
                IdCreditNoteDetail = d.IdCreditNoteDetail,
                CodeStub = d.CodeStub,
                Description = d.Description,
                Amount = d.Amount,
                PriceUnit = d.PriceUnit,
                Discount = d.Discount,
                Neto = d.Neto,
                IvaPorc = d.IvaPorc,
                IcePorc = d.IcePorc,
                IvaValor = d.IvaValor,
                IceValor = d.IceValor,
                Subtotal = d.Subtotal,
                Total = d.Total,
                Nota1 = d.Nota1,
                Nota2 = d.Nota2,
                Nota3 = d.Nota3
            }).ToList()
        };
    }

    public async Task<string> GenerateCreditNoteXmlAsync(int creditNoteId)
    {
        return await _xmlCreditNoteService.GenerarXmlNotaCreditoAsync(creditNoteId);
    }

    private async Task ActualizarStockPorDevolucion(List<CreditNoteDetail> details, CreditNoteType creditNoteType)
    {
        _logger.LogInformation($"Iniciando actualización de stock por devolución. Tipo: {creditNoteType}");

        foreach (var detail in details)
        {
            if (!detail.IdArticle.HasValue) continue;

            var article = await _context.Articles.FindAsync(detail.IdArticle.Value);
            if (article == null)
            {
                _logger.LogWarning($"No se encontró el artículo con ID {detail.IdArticle} para actualizar stock");
                continue;
            }

            switch (article.type)
            {
                // Solo actualizar stock para artículos normales (tipo 'N'), no para servicios (tipo 'S')
                case 'N':
                {
                    var stockAnterior = article.stock;
                    article.stock += detail.Amount; // Incrementar stock por devolución

                    _context.Articles.Update(article);

                    _logger.LogInformation(
                        $"Stock actualizado para artículo '{article.name}' (ID: {article.id_ar}): " +
                        $"{stockAnterior} + {detail.Amount} = {article.stock}");
                    break;
                }
                case 'S':
                    _logger.LogInformation(
                        $"Artículo de servicio '{article.name}' (ID: {article.id_ar}) - No se actualiza stock");
                    break;
                default:
                    _logger.LogWarning(
                        $"Tipo de artículo desconocido '{article.type}' para artículo '{article.name}' (ID: {article.id_ar})");
                    break;
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Actualización de stock por devolución completada");
    }

    private async Task<(List<CreditNoteDetail> details, decimal totalSinImpuestos, decimal totalDescuento, decimal
            totalImpuestos, decimal valorModificacion)>
        ProcessCreditNoteDetails(CreateCreditNoteDTO creditNoteDto, Invoice originalInvoice)
    {
        var details = new List<CreditNoteDetail>();
        decimal totalSinImpuestos = 0;
        decimal totalDescuento = 0;
        decimal totalImpuestos = 0;
        decimal valorModificacion = 0;

        switch (creditNoteDto.CreditNoteType)
        {
            case CreditNoteType.ANULAR_TODA_FACTURA:

                var notasPrevias = await _context.CreditNotes
                    .Where(cn => cn.InvoiceOriginalId == originalInvoice.inv_id &&
                                 cn.ElectronicStatus != "RECHAZADO")
                    .CountAsync();

                if (notasPrevias > 0)
                    throw new BusinessException(
                        $"No se puede anular toda la factura porque ya existen {notasPrevias} nota(s) de crédito " +
                        "asociada(s) a esta factura. Verifique las anulaciones previas.");


                // Anular todos los productos de la factura original
                foreach (var originalDetail in originalInvoice.InvoiceDetails)
                {
                    var detail = CreateCreditNoteDetailFromInvoice(originalDetail);
                    details.Add(detail);

                    totalSinImpuestos += detail.Neto;
                    totalDescuento += detail.Discount;
                    totalImpuestos += detail.IvaValor;
                }

                valorModificacion = originalInvoice.total_amount;
                break;

            case CreditNoteType.ANULAR_PRODUCTOS_PARCIAL:
                if (creditNoteDto.Details == null || !creditNoteDto.Details.Any())
                    throw new BusinessException("Para anulación parcial debe especificar los productos");

                foreach (var detailRequest in creditNoteDto.Details)
                {
                    var originalDetail = originalInvoice.InvoiceDetails
                                             .FirstOrDefault(d => d.id_article == detailRequest.ArticleId)
                                         ?? throw new BusinessException(
                                             $"El artículo {detailRequest.ArticleId} no existe en la factura original");

                    // ✅ VALIDACIÓN: Verificar cantidades disponibles considerando notas de crédito previas
                    var cantidadYaAnulada =
                        await ObtenerCantidadYaAnulada(originalInvoice.inv_id, detailRequest.ArticleId);
                    var cantidadDisponible = originalDetail.amount - cantidadYaAnulada;

                    if (detailRequest.Amount > cantidadDisponible)
                        throw new BusinessException(
                            $"No puede anular {detailRequest.Amount} unidades del artículo '{originalDetail.description}'. " +
                            $"Cantidad original: {originalDetail.amount}, Ya anulada: {cantidadYaAnulada}, " +
                            $"Disponible para anular: {cantidadDisponible}");

                    var detail = CreateCreditNoteDetailFromRequest(detailRequest, originalDetail);
                    details.Add(detail);

                    totalSinImpuestos += detail.Neto;
                    totalDescuento += detail.Discount;
                    totalImpuestos += detail.IvaValor;
                }

                valorModificacion = totalSinImpuestos + totalImpuestos;
                break;

            case CreditNoteType.CORREGIR_DESCUENTOS_PRECIOS:
                if (creditNoteDto.Details == null || !creditNoteDto.Details.Any())
                    throw new BusinessException("Para corrección debe especificar los productos y nuevos valores");

                foreach (var detail in from detailRequest in creditNoteDto.Details
                         let originalDetail = originalInvoice.InvoiceDetails
                                                  .FirstOrDefault(d => d.id_article == detailRequest.ArticleId)
                                              ?? throw new BusinessException(
                                                  $"El artículo {detailRequest.ArticleId} no existe en la factura original")
                         select CreateCorrectionDetail(detailRequest, originalDetail))
                {
                    details.Add(detail);

                    totalSinImpuestos += detail.Neto;
                    totalDescuento += detail.Discount;
                    totalImpuestos += detail.IvaValor;
                }

                valorModificacion = totalSinImpuestos + totalImpuestos;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return (details, totalSinImpuestos, totalDescuento, totalImpuestos, valorModificacion);
    }

    private async Task<int> ObtenerCantidadYaAnulada(int invoiceId, int articleId)
    {
        var cantidadAnulada = await _context.CreditNoteDetails
            .Where(cnd => cnd.CreditNote.InvoiceOriginalId == invoiceId &&
                          cnd.IdArticle == articleId &&
                          cnd.CreditNote.ElectronicStatus != "RECHAZADO") // Solo contar las no rechazadas
            .SumAsync(cnd => cnd.Amount);

        return cantidadAnulada;
    }

    private CreditNoteDetail CreateCreditNoteDetailFromInvoice(InvoiceDetail originalDetail)
    {
        return new CreditNoteDetail
        {
            CodeStub = originalDetail.code_stub,
            Description = originalDetail.description,
            Amount = originalDetail.amount,
            PriceUnit = originalDetail.price_unit,
            Discount = originalDetail.discount,
            Neto = originalDetail.neto,
            IvaPorc = originalDetail.iva_porc,
            IcePorc = originalDetail.ice_porc,
            IvaValor = originalDetail.iva_valor,
            IceValor = originalDetail.ice_valor,
            Subtotal = originalDetail.subtotal,
            Total = originalDetail.total,
            Nota1 = originalDetail.note1,
            Nota2 = originalDetail.note2,
            Nota3 = originalDetail.note3,
            IdTariff = originalDetail.id_tariff,
            IdArticle = originalDetail.id_article
        };
    }

    private CreditNoteDetail CreateCreditNoteDetailFromRequest(CreditNoteDetailRequestDTO request,
        InvoiceDetail originalDetail)
    {
        // Calcular proporcionalmente según la cantidad solicitada
        var proportion = (decimal)request.Amount / originalDetail.amount;

        return new CreditNoteDetail
        {
            CodeStub = originalDetail.code_stub,
            Description = originalDetail.description,
            Amount = request.Amount,
            PriceUnit = originalDetail.price_unit,
            Discount = Math.Round(originalDetail.discount * proportion, 2),
            Neto = Math.Round(originalDetail.neto * proportion, 2),
            IvaPorc = originalDetail.iva_porc,
            IcePorc = originalDetail.ice_porc,
            IvaValor = Math.Round(originalDetail.iva_valor * proportion, 2),
            IceValor = Math.Round(originalDetail.ice_valor * proportion, 2),
            Subtotal = Math.Round(originalDetail.subtotal * proportion, 2),
            Total = Math.Round(originalDetail.total * proportion, 2),
            Nota1 = request.Note1 ?? originalDetail.note1,
            Nota2 = request.Note2 ?? originalDetail.note2,
            Nota3 = request.Note3 ?? originalDetail.note3,
            IdTariff = originalDetail.id_tariff,
            IdArticle = originalDetail.id_article
        };
    }

    private CreditNoteDetail CreateCorrectionDetail(CreditNoteDetailRequestDTO request, InvoiceDetail originalDetail)
    {
        // Usar nuevos valores si se proporcionan, sino usar los originales
        var newPriceUnit = request.NewPriceUnit ?? originalDetail.price_unit;
        var newDiscount = request.NewDiscount ?? originalDetail.discount;

        // Calcular la diferencia (lo que se está corrigiendo)
        var priceDifference = originalDetail.price_unit - newPriceUnit;
        var discountDifference = originalDetail.discount - newDiscount;

        var netoDifference = priceDifference * request.Amount + discountDifference;
        var ivaValorDifference = Math.Round(netoDifference * (originalDetail.iva_porc / 100), 2);

        return new CreditNoteDetail
        {
            CodeStub = originalDetail.code_stub,
            Description = $"CORRECCIÓN - {originalDetail.description}",
            Amount = request.Amount,
            PriceUnit = priceDifference,
            Discount = discountDifference,
            Neto = Math.Round(netoDifference, 2),
            IvaPorc = originalDetail.iva_porc,
            IcePorc = originalDetail.ice_porc,
            IvaValor = ivaValorDifference,
            IceValor = 0,
            Subtotal = Math.Round(netoDifference, 2),
            Total = Math.Round(netoDifference + ivaValorDifference, 2),
            Nota1 = request.Note1,
            Nota2 = request.Note2,
            Nota3 = request.Note3,
            IdTariff = originalDetail.id_tariff,
            IdArticle = originalDetail.id_article
        };
    }

    private async Task<string> GenerateNextSequenceNumber(int sequenceId)
    {
        var sequence = await _context.Sequences.FindAsync(sequenceId);
        var lastCreditNote = await _context.CreditNotes
            .Where(cn => cn.SequenceId == sequenceId)
            .OrderByDescending(cn => cn.IdCreditNote)
            .FirstOrDefaultAsync();

        if (lastCreditNote != null && !string.IsNullOrEmpty(lastCreditNote.Sequence))
        {
            var lastSequenceValue = int.Parse(lastCreditNote.Sequence);
            return (lastSequenceValue + 1).ToString().PadLeft(lastCreditNote.Sequence.Length, '0');
        }

        var initialSequenceValue = int.Parse(sequence.code);
        return (initialSequenceValue + 1).ToString().PadLeft(sequence.code.Length, '0');
    }

    // Métodos auxiliares - Reutilizamos la misma lógica del InvoiceService
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
        int[] pesos = [2, 3, 4, 5, 6, 7];
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

        digito = digito switch
        {
            11 => 0,
            10 => 1,
            _ => digito
        };

        return digito;
    }
}