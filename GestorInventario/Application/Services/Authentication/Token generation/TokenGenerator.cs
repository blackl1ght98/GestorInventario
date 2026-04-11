using GestorInventario.Application.DTOs.User;
using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Application;
using Microsoft.EntityFrameworkCore;

public class TokenGenerator : ITokenGenerator
{
    private readonly GestorInventarioContext _context;
    private readonly ITokenStrategy _tokenStrategy;   // ← Aquí guardamos la estrategia elegida

    public TokenGenerator(GestorInventarioContext context, ITokenStrategyFactory factory)
    {
        _context = context;
        _tokenStrategy = factory.CreateStrategy();   // ← Aquí decido qué estrategia usar
    }

    public async Task<LoginResponseDto> GenerateTokenAsync(Usuario credencialesUsuario)
    {
        // 1. Busco el usuario completo en la base de datos (con su Rol)
        var usuarioDB = await _context.Usuarios
            .Include(u => u.IdRolNavigation)
            .FirstOrDefaultAsync(x => x.Id == credencialesUsuario.Id);

        if (usuarioDB == null)
            throw new ArgumentException("El usuario no existe en la base de datos.");

        // 2. Le entrego el usuario a la estrategia que corresponde
        //    (Symmetric, AsymmetricFixed o AsymmetricDynamic)
        return await _tokenStrategy.GenerateTokenAsync(usuarioDB);
    }
}