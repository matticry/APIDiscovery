namespace APIDiscovery.Exceptions;

public class ArticleTariffException : BusinessException
{
    public int ArticleId { get; }
    public int TariffId { get; }
        
    public ArticleTariffException(string message) 
        : base(message, "ARTICLE_TARIFF_ERROR")
    {
    }
        
    public ArticleTariffException(int articleId, int tariffId) 
        : base($"No existe relación entre el artículo {articleId} y la tarifa {tariffId}", "ARTICLE_TARIFF_ERROR")
    {
        ArticleId = articleId;
        TariffId = tariffId;
    }
        
    public ArticleTariffException(int articleId, int tariffId, string additionalInfo) 
        : base($"Error en la relación entre el artículo {articleId} y la tarifa {tariffId}: {additionalInfo}", "ARTICLE_TARIFF_ERROR")
    {
        ArticleId = articleId;
        TariffId = tariffId;
    }
}