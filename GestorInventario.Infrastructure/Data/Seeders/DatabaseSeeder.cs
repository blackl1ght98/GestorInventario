using GestorInventario.Application.Interfaces.Repositories;
using GestorInventario.Application.Services.User;
using GestorInventario.Domain.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GestorInventario.Infrastructure.Data.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedInitialRolesAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseSeeder");

        try
        {
            var rolesRepo = services.GetRequiredService<IRoleRepository>();
                
            var existing = await rolesRepo.GetAllAsync();
            if (existing.Any())
            {
                logger.LogInformation(
                    "Roles already seeded ({Count} found). Skipping.",
                    existing.Count);
                return;
            }

            logger.LogInformation("No roles found. Seeding initial roles...");

            var admin = new Role { Nombre = Roles.Administrador };
            var user = new Role { Nombre = Roles.Usuario };

            await rolesRepo.AddAsync(admin);
            await rolesRepo.AddAsync(user);

            logger.LogInformation("Seeded 2 roles.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding initial roles");
            throw; 
        }
    }
}