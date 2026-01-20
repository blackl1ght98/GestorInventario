using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Application.Services.Authentication;

using GestorInventario.Application.Services.Generic_Services;
using GestorInventario.Infraestructure.Repositories;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.PaginacionLogica;

namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static  class TransientServices
    {
        public static IServiceCollection AddTransientServices(this IServiceCollection services) {

            services.AddTransient<GenerarPaginas>();
            services.AddTransient<PaginacionMetodo>();
            services.AddTransient<PolicyExecutor>();
            services.AddTransient<UtilityClass>();
            services.AddTransient<IGestorArchivos, GestorArchivosService>();
            services.AddTransient<HashService>();
            services.AddTransient<ICarritoRepository, CarritoRepository>();
            services.AddTransient<IConfirmEmailService, ConfirmEmailService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<TokenService>();
            services.AddTransient<IAdminRepository, AdminRepository>();
            services.AddTransient<IAuthRepository, AuthRepository>();
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient<IPaypalService, PaypalService>();
            services.AddTransient<IPedidoRepository, PedidoRepository>();
            services.AddTransient<PolicyHandler>();
            services.AddTransient<IProductoRepository, ProductoRepository>();
            services.AddTransient<IRembolsoRepository, RembolsoRepository>();
            services.AddTransient<IProveedorRepository, ProveedorRepository>();
            services.AddTransient<IPdfService, PdfService>();
            services.AddTransient<IPaypalRepository, PaypalRepository>();
            services.AddTransient<IEncryptionService, EncryptionService>();
            services.AddTransient<IPaypalService, PaypalService>();
            services.AddTransient<IPaymentRepository, PaymentRepository>();
            services.AddTransient<ImageOptimizerService>();
            services.AddTransient<ITokenGenerator, TokenGenerator>();
           
            services.AddTransient<IBarCodeService, BarCodeService>();
           
            return services;

        }
    }
}
