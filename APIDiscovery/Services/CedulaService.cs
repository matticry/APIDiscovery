using System.Diagnostics;
using APIDiscovery.Exceptions;
using APIDiscovery.Interfaces;
using APIDiscovery.Models;
using APIDiscovery.Models.DTOs;
using Newtonsoft.Json;

namespace APIDiscovery.Services;

public class CedulaService : ICedulaService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    
    public CedulaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _baseUrl = "http://datos.elixirsa.net/cedula/";
    }
    
    
    public async Task<RucResponse> ConsultarRucAsync(string numeroRuc, string ipSolicitante)
{
    var stopwatch = new Stopwatch();
    stopwatch.Start();
    
    try
    {
        // Validar que el RUC tenga 13 dígitos
        if (string.IsNullOrEmpty(numeroRuc) || numeroRuc.Length != 13)
        {
            return new RucResponse
            {
                StatusCode = 400,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = "El RUC debe tener 13 dígitos",
                Message = "Validación de RUC fallida: El RUC debe tener 13 dígitos",
                IpSolicitante = ipSolicitante
            };
        }
        
        // Validar que todos sean dígitos
        if (numeroRuc.Any(c => !char.IsDigit(c)))
        {
            return new RucResponse
            {
                StatusCode = 400,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = "El RUC debe contener solo dígitos",
                Message = "Validación de RUC fallida: El RUC debe contener solo dígitos",
                IpSolicitante = ipSolicitante
            };
        }
        
        // Validar que los últimos 3 dígitos sean 001
        if (numeroRuc.Substring(10) != "001")
        {
            return new RucResponse
            {
                StatusCode = 400,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = "Los últimos 3 dígitos del RUC deben ser 001",
                Message = "Validación de RUC fallida: Los últimos 3 dígitos del RUC deben ser 001",
                IpSolicitante = ipSolicitante
            };
        }
        
        // Validación de los primeros 10 dígitos (deben corresponder a una cédula válida)
        if (!VerificaCedula(numeroRuc.Substring(0, 10).ToCharArray()))
        {
            return new RucResponse
            {
                StatusCode = 400,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = "Los primeros 10 dígitos del RUC no corresponden a una cédula ecuatoriana válida",
                Message = "Validación de RUC fallida: Los primeros 10 dígitos no corresponden a una cédula ecuatoriana válida",
                IpSolicitante = ipSolicitante
            };
        }
        
        var response = await _httpClient.GetAsync($"http://datos.elixirsa.net/ruc/{numeroRuc}");
        var tiempoRespuesta = stopwatch.ElapsedMilliseconds;
        
        // Si la respuesta no es exitosa
        if (!response.IsSuccessStatusCode)
        {
            return new RucResponse
            {
                StatusCode = (int)response.StatusCode,
                TiempoRespuesta = $"{tiempoRespuesta} ms",
                Error = $"Error en la consulta: {response.ReasonPhrase}",
                Message = $"No se pudo consultar el RUC {numeroRuc}",
                IpSolicitante = ipSolicitante
            };
        }
        
        var responseBody = await response.Content.ReadAsStringAsync();
        
        // Comprobar si la respuesta está vacía o es "NaN" (lo que causa el error de parsing)
        if (string.IsNullOrWhiteSpace(responseBody) || responseBody.Trim() == "NaN" || responseBody.Contains("NaN"))
        {
            return new RucResponse
            {
                StatusCode = 404,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = "RUC no encontrado",
                Message = "Ups... Tuvimos dificultades al procesar tu RUC, no tenemos registros por el momento con tu RUC. Gracias.",
                IpSolicitante = ipSolicitante
            };
        }
        
        List<RucData>? rucDataList;
        try 
        {
            rucDataList = JsonConvert.DeserializeObject<List<RucData>>(responseBody);
            
            if (rucDataList == null || !rucDataList.Any())
            {
                return new RucResponse
                {
                    StatusCode = 404,
                    TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                    Error = "Datos no disponibles",
                    Message = "Ups... Tuvimos dificultades al procesar tu RUC, no tenemos registros por el momento con tu RUC. Gracias.",
                    IpSolicitante = ipSolicitante
                };
            }
        }
        catch (JsonException)
        {
            return new RucResponse
            {
                StatusCode = 404,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = "Error al procesar la respuesta",
                Message = "Ups... Tuvimos dificultades al procesar tu RUC, no tenemos registros por el momento con tu RUC. Gracias.",
                IpSolicitante = ipSolicitante
            };
        }
        
        var rucData = rucDataList.First();
        
        return new RucResponse
        {
            StatusCode = (int)response.StatusCode,
            TiempoRespuesta = $"{tiempoRespuesta} ms",
            Datos = new RucInfo
            {
                NumeroRuc = rucData.numeroRuc,
                RazonSocial = rucData.razonSocial,
                EstadoContribuyente = rucData.estadoContribuyenteRuc,
                ActividadEconomica = rucData.actividadEconomicaPrincipal,
                TipoContribuyente = rucData.tipoContribuyente,
                Regimen = rucData.regimen,
                Categoria = rucData.categoria,
                ObligadoLlevarContabilidad = rucData.obligadoLlevarContabilidad,
                AgenteRetencion = rucData.agenteRetencion,
                ContribuyenteEspecial = rucData.contribuyenteEspecial,
                FechaInicioActividades = rucData.informacionFechasContribuyente?.fechaInicioActividades,
                FechaCese = rucData.informacionFechasContribuyente?.fechaCese,
                FechaReinicioActividades = rucData.informacionFechasContribuyente?.fechaReinicioActividades,
                FechaActualizacion = rucData.informacionFechasContribuyente?.fechaActualizacion,
                ContribuyenteFantasma = rucData.contribuyenteFantasma,
                TransaccionesInexistente = rucData.transaccionesInexistente
            },
            Message = "Consulta realizada exitosamente",
            IpSolicitante = ipSolicitante
        };
    }
    catch (JsonException)
    {
        return new RucResponse
        {
            StatusCode = 404,
            TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
            Error = "Formato de respuesta inválido",
            Message = "Ups... Tuvimos dificultades al procesar tu RUC, no tenemos registros por el momento con tu RUC. Gracias.",
            IpSolicitante = ipSolicitante
        };
    }
    catch (Exception ex)
    {
        return new RucResponse
        {
            StatusCode = 500,
            TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
            Error = ex.Message,
            Message = "Error inesperado al procesar la solicitud",
            IpSolicitante = ipSolicitante
        };
    }
    finally
    {
        stopwatch.Stop();
    }
}
    
    public async Task<CedulaResponse> ConsultarCedulaAsync(string numeroCedula, string ipSolicitante)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        try
        {
            // Validar que la cédula tenga 10 dígitos
            if (string.IsNullOrEmpty(numeroCedula) || numeroCedula.Length != 10)
            {
                return new CedulaResponse
                {
                    StatusCode = 400,
                    TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                    Error = "La cédula debe tener 10 dígitos",
                    Message = "Validación de cédula fallida: La cédula debe tener 10 dígitos",
                    IpSolicitante = ipSolicitante
                };
            }
            
            // Validar que todos sean dígitos
            if (numeroCedula.Any(c => !char.IsDigit(c)))
            {
                return new CedulaResponse
                {
                    StatusCode = 400,
                    TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                    Error = "La cédula debe contener solo dígitos",
                    Message = "Validación de cédula fallida: La cédula debe contener solo dígitos",
                    IpSolicitante = ipSolicitante
                };
            }
            
            // Validación de cédula ecuatoriana
            if (!VerificaCedula(numeroCedula.ToCharArray()))
            {
                return new CedulaResponse
                {
                    StatusCode = 400,
                    TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                    Error = "Cédula no es ecuatoriana o no es válida",
                    Message = "Validación de cédula fallida: Cédula no es ecuatoriana o no es válida",
                    IpSolicitante = ipSolicitante
                };
            }
            
            var response = await _httpClient.GetAsync($"{_baseUrl}{numeroCedula}");
            var tiempoRespuesta = stopwatch.ElapsedMilliseconds;
            
            // Si la respuesta no es exitosa
            if (!response.IsSuccessStatusCode)
            {
                return new CedulaResponse
                {
                    StatusCode = (int)response.StatusCode,
                    TiempoRespuesta = $"{tiempoRespuesta} ms",
                    Error = $"Error en la consulta: {response.ReasonPhrase}",
                    Message = $"No se pudo consultar la cédula {numeroCedula}",
                    IpSolicitante = ipSolicitante
                };
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            
            // Comprobar si la respuesta está vacía o es "NaN" (lo que causa el error de parsing)
            if (string.IsNullOrWhiteSpace(responseBody) || responseBody.Trim() == "NaN" || responseBody.Contains("NaN"))
            {
                return new CedulaResponse
                {
                    StatusCode = 404,
                    TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                    Error = "Cédula no encontrada",
                    Message = "Ups... Tuvimos dificultades al procesar tu cédula, no tenemos registros por el momento con tu cédula. Gracias.",
                    IpSolicitante = ipSolicitante
                };
            }
            
            CedulaData? cedulaData;
            try 
            {
                cedulaData = JsonConvert.DeserializeObject<CedulaData>(responseBody);
                
                if (cedulaData == null)
                {
                    return new CedulaResponse
                    {
                        StatusCode = 404,
                        TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                        Error = "Datos no disponibles",
                        Message = "Ups... Tuvimos dificultades al procesar tu cédula, no tenemos registros por el momento con tu cédula. Gracias.",
                        IpSolicitante = ipSolicitante
                    };
                }
            }
            catch (JsonException)
            {
                return new CedulaResponse
                {
                    StatusCode = 404,
                    TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                    Error = "Error al procesar la respuesta",
                    Message = "Ups... Tuvimos dificultades al procesar tu cédula, no tenemos registros por el momento con tu cédula. Gracias.",
                    IpSolicitante = ipSolicitante
                };
            }
            
            return new CedulaResponse
            {
                StatusCode = (int)response.StatusCode,
                TiempoRespuesta = $"{tiempoRespuesta} ms",
                Datos = new CedulaInfo
                {
                    Nombres = cedulaData.nombres,
                    Apellidos = cedulaData.apellidos,
                    Edad = cedulaData.edad,
                    FechaNacimiento = cedulaData.fechaNacimiento,
                    Provincia = ObtenerProvinciaConCapital(cedulaData.provDomicilio),
                    EstadoCivil = ObtenerEstadoCivil(cedulaData.estadoCivilId),
                    Genero = ObtenerGenero(cedulaData.generoId),
                    Nacionalidad = ObtenerNacionalidad(cedulaData.nacionalidadId)
                },
                Message = "Consulta realizada exitosamente",
                IpSolicitante = ipSolicitante
            };
        }
        catch (CedulaInvalidaException ex)
        {
            return new CedulaResponse
            {
                StatusCode = ex.StatusCode,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = ex.Message,
                Message = $"Validación de cédula fallida: {ex.Message}"
            };
        }
        catch (HttpRequestException ex)
        {
            return new CedulaResponse
            {
                StatusCode = 500,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = ex.Message,
                Message = "Error en la conexión con el servicio de consulta de cédulas"
            };
        }
        catch (JsonException)
        {
            return new CedulaResponse
            {
                StatusCode = 404,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = "Formato de respuesta inválido",
                Message = "Ups... Tuvimos dificultades al procesar tu cédula, no tenemos registros por el momento con tu cédula. Gracias.",
                IpSolicitante = ipSolicitante
            };
        }
        catch (Exception ex)
        {
            // Verificar si el error es específicamente por el error de parsing NaN
            if (ex.Message.Contains("NaN"))
            {
                return new CedulaResponse
                {
                    StatusCode = 404,
                    TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                    Error = "Cédula no encontrada en el sistema",
                    Message = "Ups... Tuvimos dificultades al procesar tu cédula, no tenemos registros por el momento con tu cédula. Gracias.",
                    IpSolicitante = ipSolicitante
                };
            }
            
            return new CedulaResponse
            {
                StatusCode = 500,
                TiempoRespuesta = $"{stopwatch.ElapsedMilliseconds} ms",
                Error = ex.Message,
                Message = "Error inesperado al procesar la solicitud",
                IpSolicitante = ipSolicitante
            };
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    private static bool VerificaCedula(char[] validarCedula)
    {
        int aux = 0, par = 0, impar = 0, verifi;
        for (var i = 0; i < 9; i += 2)
        {
            aux = 2 * int.Parse(validarCedula[i].ToString());
            if (aux > 9)
                aux -= 9;
            par += aux;
        }
        for (var i = 1; i < 9; i += 2)
        {
            impar += int.Parse(validarCedula[i].ToString());
        }
        aux = par + impar;
        if (aux % 10 != 0)
        {
            verifi = 10 - (aux % 10);
        }
        else
            verifi = 0;
        return verifi == int.Parse(validarCedula[9].ToString());
    }

    private string ObtenerProvinciaConCapital(string codigoProvincia)
    {
        // Mapping de códigos de provincia a nombres y capitales
        return codigoProvincia switch
        {
            "01" => "Azuay - Cuenca",
            "02" => "Bolívar - Guaranda",
            "03" => "Cañar - Azogues",
            "04" => "Carchi - Tulcán",
            "05" => "Chimborazo - Riobamba",
            "06" => "Cotopaxi - Latacunga",
            "07" => "El Oro - Machala",
            "08" => "Esmeraldas - Esmeraldas",
            "09" => "Galápagos  - Puerto Baquerizo Moreno",
            "10" => "Guayas - Guayaquil",
            "11" => "Imbabura - Ibarra",
            "12" => "Loja - Loja",
            "13" => "Los Ríos - Babahoyo",
            "14" => "Manabi - Portoviejo",
            "15" => "Morona Santiago - Macas",
            "16" => "Napo - Tena",
            "17" => "Orellana  - Puerto Francisco de Orellana",
            "18" => "Pastaza - Puyo",
            "19" => "Pichincha - Quito",
            "20" => "Santa Elena - Santa Elena",
            "21" => "Santo Domingo de los Tsáchilas - Santo Domingo de los Colorados",
            "22" => "Sucumbíos  - Nueva Loja",
            "23" => "Tungurahua - Ambato",
            "24" => "Zamora Chinchipe - Zamora",
            _ => $"Provincia desconocida ({codigoProvincia})"
        };
    }

    private string ObtenerEstadoCivil(int estadoCivilId)
    {
        return estadoCivilId switch
        {
            1 => "Soltero/a",
            6 => "Soltero/a",
            2 => "Casado/a",
            3 => "Viudo/a",
            4 => "Unión de Hecho",
            5 => "Divorciado/a",
            _ => "Desconocido"
        };
    }

    private string ObtenerGenero(string generoId)
    {
        return generoId switch
        {
            "1" => "Masculino",
            "2" => "Femenino",
            _ => "No especificado"
        };
    }

    private string ObtenerNacionalidad(int nacionalidadId)
    {
        return nacionalidadId switch
        {
            1 => "Ecuatoriana",
            2 => "Extranjera",
            _ => "Desconocida"
        };
    }
}