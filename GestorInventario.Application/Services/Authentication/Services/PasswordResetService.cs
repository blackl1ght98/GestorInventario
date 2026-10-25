using GestorInventario.Shared.Utilities;
using System.Security.Cryptography;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;

namespace GestorInventario.Application.Services.Authentication.Services
{
    public class PasswordResetService: IPasswordResetService
    {
        private readonly IHashService _hashService;
        private readonly IAdminRepository _adminRepository;

        
        public PasswordResetService(IHashService hashService, IAdminRepository adminRepository)
        {
            _hashService = hashService;
            _adminRepository = adminRepository;
        }

        public async Task<OperationResult<string > >GenerarPasswordTemporalAsync(string email)
        {
            var correo = await _adminRepository.ExisteEmailAsync(email);
            if (!correo)
            {
                return OperationResult<string>.Fail("El correo electronico no existe");
            }
            var usuario = await _adminRepository.ObtenerEmail(email);
            // Lógica de generación y hash aquí
            var contrasenaTemporal = GenerarContrasenaTemporal();
            var resultadoHash = _hashService.Hash(contrasenaTemporal);
            var fechaExpiracion = DateTime.UtcNow.AddMinutes(5);
            if (usuario == null)
                return OperationResult<string>.Fail();
            usuario.TemporaryPassword = resultadoHash.Hash;
            usuario.ResetTokenSalt = resultadoHash.Salt;       
            usuario.FechaExpiracionContrasenaTemporal = fechaExpiracion;
            usuario.ResetTokenUsed = false;
            
            await _adminRepository.ActualizarUsuarioAsync(usuario);

            return OperationResult<string>.Ok(contrasenaTemporal,"Restablecida");
        }

        private string GenerarContrasenaTemporal()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
            return new string(Enumerable.Range(0, 12)
                .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
                .ToArray());
        }
    }
}
