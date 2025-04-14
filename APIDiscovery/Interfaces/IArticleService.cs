using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface IArticleService
{
    Task<ResponseDto> CreateArticle(ArticleCreateDto articleDto);
    Task<ResponseDto> GetArticleById(int id);
    Task<ResponseDto> GetArticlesByEnterprise(int enterpriseId);
    Task<ResponseDto> UpdateArticle(int id, ArticleCreateDto articleDto);
    Task<ResponseDto> DeleteArticle(int id);
}