using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Interfaces;

public interface ISequenceService
{
    Task<ResponseDto> CreateSequence(SequenceDto sequenceDto);
    Task<ResponseDto> GetByIdAsync(int id);
    Task<ResponseDto> GetAllAsync();
    Task<ResponseDto> GetByEmissionPointIdAsync(int emissionPointId);
    Task<ResponseDto> UpdateAsync(int id, SequenceDto sequenceDto);
    Task<ResponseDto> DeleteAsync(int id);
}