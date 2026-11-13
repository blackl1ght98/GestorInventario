using GestorInventario.Application.DTOS.User;
using GestorInventario.Domain.Models;

namespace GestorInventario.Application.Mappers;

public static class UserMapper
{
    public static Usuario MapToDto(RegisterUserDto dto)
    {
        return new Usuario
        {
            Email = dto.Email,
            NombreCompleto = dto.NombreCompleto,
            FechaNacimiento = dto.FechaNacimiento ?? default,
            Telefono = dto.Telefono,
            Direccion = dto.Direccion,
            Ciudad = dto.Ciudad,
            CodigoPostal = dto.CodigoPostal,
            BajaUsuario = false,
            ConfirmacionEmail = false,
            EmailVerificationToken = null,
            ResetTokenSalt = null,
            ResetTokenUsed = null,
            TemporaryPassword = null,
            FechaExpiracionContrasenaTemporal = null,
        };
    }

   
    /*
     *  private static void UpdateUsuarioFromDto(Usuario usuario, EditUserDto dto)
        {
            int.TryParse(dto.CodigoPostal, out int cp);
            usuario.Email = dto.Email;
            usuario.NombreCompleto = dto.NombreCompleto;
            usuario.FechaNacimiento = dto.FechaNacimiento ?? default;
            usuario.Telefono = dto.Telefono ?? string.Empty;
            usuario.Direccion = dto.Direccion;
            usuario.Ciudad = dto.Ciudad;
            usuario.CodigoPostal = cp;
        }
     */
}