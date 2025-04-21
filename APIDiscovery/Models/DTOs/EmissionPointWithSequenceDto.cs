namespace APIDiscovery.Models.DTOs;

public class EmissionPointWithSequenceDto
{
    public int IdEmissionPoint { get; set; }
    public string Code { get; set; }
    public string Details { get; set; }
    public bool Type { get; set; }
    public List<SequenceDto> Sequences { get; set; }
}