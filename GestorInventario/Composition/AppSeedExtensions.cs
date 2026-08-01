using GestorInventario.Domain.Models;

using GestorInventario.Interfaces.Infraestructure.Repositories;

namespace GestorInventario.Composition
{
    public static class AppSeedExtensions
    {
        public static async Task SeedInitialRolesAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger("AppSeed");

            try
            {
                var rolesRepo = services.GetRequiredService<IUserRepository>();
                

                var existing = await rolesRepo.GetAllAsync();
                if (existing.Any())
                {
                    logger.LogInformation(
                        "Roles already seeded ({Count} found). Skipping.",
                        existing.Count);
                    return;
                }

                logger.LogInformation("No roles found. Seeding initial roles...");

                var admin = new Role { Nombre = "Administrador" };
                var user = new Role { Nombre = "Usuario" };

                await rolesRepo.AddAsync(admin);
                await rolesRepo.AddAsync(user);
             

                logger.LogInformation("Seeded 2 roles.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding initial roles");
               
            }
        }
    }
}