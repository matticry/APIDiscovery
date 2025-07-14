using System.Text;
using System.Text.Json;
using APIDiscovery.Core;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs.IADTOs;
using Microsoft.EntityFrameworkCore;

namespace APIDiscovery.Services;

public class AiService : IAiService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiService> _logger;

    public AiService(ApplicationDbContext context, IConfiguration configuration, ILogger<AiService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AIStockReportResponse> GetLowStockReportAsync(int enterpriseId)
    {
        try
        {
            _logger.LogInformation($"Generando reporte de stock con IA para empresa {enterpriseId}");

            // 1. Obtener artículos con bajo stock
            var lowStockArticles = await _context.Articles
                .Where(a => a.id_enterprise == enterpriseId &&
                            a.stock < 10 &&
                            a.type == 'N' &&
                            a.status == 'A')
                .Select(a => new LowStockItem
                {
                    Id = a.id_ar,
                    Name = a.name,
                    Code = a.code,
                    CurrentStock = a.stock,
                    UnitPrice = a.price_unit,
                    Description = a.description,
                    Category = a.Category.name
                })
                .OrderBy(a => a.CurrentStock)
                .ToListAsync();

            if (!lowStockArticles.Any())
                return new AIStockReportResponse
                {
                    Success = true,
                    Message = "✅ Todos los productos tienen suficiente stock",
                    EnterpriseId = enterpriseId,
                    TotalLowStockItems = 0,
                    Items = new List<LowStockItem>(),
                    AIRecommendation =
                        "No se requieren reposiciones en este momento. El inventario está en niveles óptimos."
                };

            // 2. Obtener contexto de la empresa
            var enterprise = await _context.Enterprises
                .FirstOrDefaultAsync(e => e.id_en == enterpriseId);

            if (enterprise == null) throw new Exception($"Empresa con ID {enterpriseId} no encontrada");

            // 3. Construir prompt para IA
            var prompt = BuildAiPrompt(enterprise, lowStockArticles);

            // 4. Llamar a OpenRouter AI
            var aiRecommendation = await CallOpenRouterApi(prompt);

            // 5. Agregar recomendaciones individuales usando IA
            await AddIndividualRecommendations(lowStockArticles);

            return new AIStockReportResponse
            {
                Success = true,
                Message = $"📊 Reporte generado: {lowStockArticles.Count} productos con stock bajo",
                EnterpriseId = enterpriseId,
                EnterpriseName = enterprise.comercial_name ?? throw new InvalidOperationException(),
                TotalLowStockItems = lowStockArticles.Count,
                Items = lowStockArticles,
                AIRecommendation = aiRecommendation,
                GeneratedAt = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error al generar reporte de stock para empresa {enterpriseId}");
            return new AIStockReportResponse
            {
                Success = false,
                Message = $"❌ Error: {ex.Message}",
                EnterpriseId = enterpriseId,
                Items = []
            };
        }
    }

    private string BuildAiPrompt(Enterprise enterprise, List<LowStockItem> lowStockItems)
    {
        var itemsText = string.Join("\n", lowStockItems.Select(item =>
            $"• {item.Name} (Código: {item.Code}) - Stock: {item.CurrentStock} - Precio: ${item.UnitPrice:F2} - Categoría: {item.Category}"));

        return $@"
            Eres un consultor experto en gestión de inventarios. Analiza el siguiente reporte de stock bajo:

            EMPRESA: {enterprise.comercial_name}
            RUC: {enterprise.ruc}

            PRODUCTOS CON STOCK BAJO:
            {itemsText}

            INSTRUCCIONES:
            1. Analiza cada producto y su nivel de stock
            2. Para cada producto, recomienda cuántas unidades reponer
            3. Prioriza por criticidad (productos con stock más bajo)
            4. Considera el precio unitario para el presupuesto
            5. Da recomendaciones prácticas y específicas

            FORMATO DE RESPUESTA:
            Proporciona un análisis claro con:
            - Resumen general de la situación
            - Productos más críticos
            - Recomendaciones específicas de reposición
            - Presupuesto estimado total

            Sé conciso pero específico en tus recomendaciones.";
    }

    private async Task<string> CallOpenRouterApi(string prompt)
    {
        try
        {
            // Obtener API Key del .env
            var apiKey = _configuration["OPENROUTER_API_KEY"];

            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("OPENROUTER_API_KEY no configurada en el archivo .env");

            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("HTTP-Referer", "https://matticry.online");
            client.DefaultRequestHeaders.Add("X-Title", "Sistema de Inventario IA");

            // Lista de modelos gratuitos para intentar en orden de preferencia
            var freeModels = new[]
            {
                "deepseek/deepseek-r1-distill-llama-70b:free",
            };

            string? aiResponse = null;
            Exception? lastException = null;

            // Intentar con cada modelo hasta encontrar uno que funcione
            foreach (var model in freeModels)
            {
                try
                {
                    _logger.LogDebug($"Intentando con modelo: {model}");
                    
                    var requestBody = new
                    {
                        model,
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = prompt
                            }
                        },
                        max_tokens = 1000,
                        temperature = 0.7
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("chat/completions", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(responseJson);
                        aiResponse = doc.RootElement.GetProperty("choices")[0]
                            .GetProperty("message").GetProperty("content").GetString();
                        
                        _logger.LogInformation($"Éxito con modelo: {model}");
                        break; // Salir del bucle si tuvo éxito
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        _logger.LogWarning($"Modelo {model} falló: {response.StatusCode} - {errorContent}");
                        lastException = new Exception($"Error con modelo {model}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Error al probar modelo {model}");
                    lastException = ex;
                }
            }

            if (aiResponse != null)
            {
                return aiResponse;
            }

            // Si ningún modelo funcionó, devolver mensaje de fallback
            _logger.LogError(lastException, "Todos los modelos gratuitos fallaron");
            return "⚠️ Servicio de IA temporalmente no disponible. Utilizando recomendaciones básicas del sistema.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error general al llamar OpenRouter API");
            return $"Error de conexión con IA: {ex.Message}";
        }
    }

    private async Task AddIndividualRecommendations(List<LowStockItem> items)
    {
        // Agregar recomendaciones simples basadas en reglas de negocio
        await Task.Run(() =>
        {
            foreach (var item in items)
            {
                switch (item.CurrentStock)
                {
                    case <= 2:
                        item.RecommendedQuantity = 50;
                        item.Priority = "CRÍTICO";
                        item.Recommendation =
                            $"⚠️ URGENTE: Reponer {item.RecommendedQuantity} unidades inmediatamente. Stock crítico.";
                        break;
                    case <= 5:
                        item.RecommendedQuantity = 30;
                        item.Priority = "ALTO";
                        item.Recommendation =
                            $"📢 PRIORITARIO: Reponer {item.RecommendedQuantity} unidades en los próximos 3 días.";
                        break;
                    default:
                        item.RecommendedQuantity = 20;
                        item.Priority = "MODERADO";
                        item.Recommendation =
                            $"📋 PROGRAMAR: Reponer {item.RecommendedQuantity} unidades en la próxima semana.";
                        break;
                }

                item.EstimatedCost = item.RecommendedQuantity * item.UnitPrice;
            }
        });
    }
}