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
            CodigoPostal = int.Parse(dto.CodigoPostal),
            BajaUsuario = false,
            ConfirmacionEmail = false,
            EmailVerificationToken = null,
            ResetTokenSalt = null,
            ResetTokenUsed = null,
            TemporaryPassword = null,
            FechaExpiracionContrasenaTemporal = null,
        };
    }
}