
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Shared.DTOS.User;
using Microsoft.EntityFrameworkCore;

public class TokenGenerator : ITokenGenerator
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenStrategy _tokenStrategy;

    public TokenGenerator(IUserRepository userRepository, ITokenStrategy tokenStrategy)
    {
        _userRepository = userRepository;
        _tokenStrategy = tokenStrategy;
    }

    public async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
    {
        var usuarioDB = await _userRepository.ObtenerUsuarioPorId(credencialesUsuario.Id);

        if (usuarioDB is null)
            throw new ArgumentException("El usuario no existe en la base de datos.");

        return await _tokenStrategy.GenerateTokenAsync(usuarioDB);
    }
}