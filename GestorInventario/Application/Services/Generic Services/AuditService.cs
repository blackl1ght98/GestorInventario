using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;

namespace GestorInventario.Application.Services.Generic_Services
{
    public class AuditService : IAuditService
    {
        private readonly GestorInventarioContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;
        private readonly ICurrentUserAccessor _current;

        public AuditService(
            GestorInventarioContext context,
            ICurrentUserAccessor current,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _current = current;
        }

        public async Task RegistrarAuditoriaAsync(
            string tabla,
            string operacion,
            int registroId,
            int? usuarioId = null,
            string? campo = null,
            string? valorAnterior = null,
            string? valorNuevo = null,
            string? descripcion = null)
        {
            try
            {
                var audit = new AuditLog
                {
                    Tabla = tabla,
                    Operacion = operacion,
                    RegistroId = registroId,
                    UsuarioId = usuarioId ?? _current?.GetCurrentUserId(), 
                    Campo = campo,
                    ValorAnterior = valorAnterior,
                    ValorNuevo = valorNuevo,
                    Descripcion = descripcion,
                    IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
                };

                await _context.AuditLogs.AddAsync(audit);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría en tabla {Tabla} - Operación {Operacion}", tabla, operacion);
                // No lanzamos excepción para no romper el flujo principal
            }
        }
    }
}
