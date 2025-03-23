using System.Diagnostics;
using System.Net;
using System.Text.Json;
using APIDiscovery.Exceptions;
using APIDiscovery.Models.DTOs;

namespace APIDiscovery.Controllers.Middleware;

    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var watch = Stopwatch.StartNew();
            
            try
            {
                await _next(context);
                watch.Stop();
            }
            catch (Exception ex)
            {
                watch.Stop();
                await HandleExceptionAsync(context, ex, watch.ElapsedMilliseconds);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, long elapsedMs)
        {
            var code = HttpStatusCode.InternalServerError;
            var message = "Ha ocurrido un error en el servidor.";
            string fix = null;

            if (exception is BadRequestException badRequestEx)
            {
                code = HttpStatusCode.BadRequest;
                message = badRequestEx.Message;
                
                // Añadir mensaje de solución específico según el error
                if (message.Contains("no tiene el rol especificado"))
                {
                    fix = "Pruebe con estos roles: Enfermero/a, Padre/Madre de familia";
                }
                // Puedes añadir más condiciones para otros tipos de errores
            }
            else if (exception is NotFoundException notFoundEx)
            {
                code = HttpStatusCode.NotFound;
                message = notFoundEx.Message;
            }

            var result = JsonSerializer.Serialize(new ApiErrorResponse
            {
                Message = message,
                ResponseTimeMs = elapsedMs,
                Fix = fix
            });

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;
            
            return context.Response.WriteAsync(result);
        }
    }