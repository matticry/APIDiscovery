namespace APIDiscovery.Models.DTOs;

public class RucData
{
    public string numeroRuc { get; set; }
    public string razonSocial { get; set; }
    public string estadoContribuyenteRuc { get; set; }
    public string actividadEconomicaPrincipal { get; set; }
    public string tipoContribuyente { get; set; }
    public string regimen { get; set; }
    public string categoria { get; set; }
    public string obligadoLlevarContabilidad { get; set; }
    public string agenteRetencion { get; set; }
    public string contribuyenteEspecial { get; set; }
    public InformacionFechasContribuyente informacionFechasContribuyente { get; set; }
    public object representantesLegales { get; set; }
    public object motivoCancelacionSuspension { get; set; }
    public string contribuyenteFantasma { get; set; }
    public string transaccionesInexistente { get; set; }
}