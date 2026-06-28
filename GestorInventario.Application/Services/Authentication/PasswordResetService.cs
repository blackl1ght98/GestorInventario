using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.Utilities;
using GestorInventario.Utilities;
using System.Security.Cryptography;

namespace GestorInventario.Application.Services.Authentication
{
    public class PasswordResetService: IPasswordResetService
    {
        private readonly IHashService _hashService;
        private readonly IUserRepository _userRepository;

        public PasswordResetService(IHashService hashService, IUserRepository userRepository)
        {
            _hashService = hashService;
            _userRepository = userRepository;
        }

        public async Task<OperationResult<string > >GenerarPasswordTemporalAsync(string email)
        {
            // Lógica de generación y hash aquí
            var contrasenaTemporal = GenerarContrasenaTemporal();
            var resultadoHash = _hashService.Hash(contrasenaTemporal);
            var fechaExpiracion = DateTime.UtcNow.AddMinutes(5);

            // El repositorio solo guarda
            var resultado = await _userRepository.GuardarPasswordTemporalAsync(
                email, resultadoHash.Hash, resultadoHash.Salt, fechaExpiracion);

            if (!resultado.Success)
                return OperationResult<string>.Fail(resultado.Message);

            return OperationResult<string>.Ok("Password temporal generada",
                contrasenaTemporal);
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
