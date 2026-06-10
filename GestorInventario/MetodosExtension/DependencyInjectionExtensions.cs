using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Application.Services.Common;
using GestorInventario.Application.Services.External_Sevices;
using GestorInventario.Application.Services.External_Sevices.Refunds;
using GestorInventario.Application.Services.Generic_Services;
using GestorInventario.Application.Services.Notifications;
using GestorInventario.Application.Services.Products;
using GestorInventario.Application.Services.User;
using GestorInventario.Infraestructure.Repositories.AdminRepository;
using GestorInventario.Infraestructure.Repositories.CarritoRepository;
using GestorInventario.Infraestructure.Repositories.PaymentRepository;
using GestorInventario.Infraestructure.Repositories.PaypalRepository;
using GestorInventario.Infraestructure.Repositories.PedidoRepository;
using GestorInventario.Infraestructure.Repositories.ProductoRepository;
using GestorInventario.Infraestructure.Repositories.ProveedorRepository;
using GestorInventario.Infraestructure.Repositories.RembolsoRepository;
using GestorInventario.Infraestructure.Repositories.UserRepository;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.PaginacionLogica;


namespace GestorInventario.MetodosExtension
{
    public static class DependencyInjectionExtensions
    {
        // 1. CAPA DE DATOS: Solo Repositorios y Unit of Work
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ICarritoRepository, CarritoRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<IPedidoRepository, PedidoRepository>();
            services.AddScoped<IProductoRepository, ProductoRepository>();
            services.AddScoped<IRembolsoRepository, RembolsoRepository>();
            services.AddScoped<IProveedorRepository, ProveedorRepository>();
            services.AddScoped<IPaypalRepository, PaypalRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            return services;
        }

        // 2. CAPA de LÓGICA DE NEGOCIO: Servicios de Aplicación
        // Estos coordinan los repositorios y aplican reglas de negocio
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Servicios de Auth y Seguridad
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IAuthService, AuthService>();

            // Servicios de Notificaciones y Documentos
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddScoped<IBarCodeService, BarCodeService>();
            services.AddScoped<IStockNotificationService, StockNotificationService>();
            services.AddScoped<IReembolsoNotificationService, ReembolsoNotificationService>();

            // Servicios de Gestión (Core Business)
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IProductManagementService, ProductManagementService>();
            services.AddScoped<IPedidoManagementService, PedidoManagementService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICarritoService, CarritoService>();

            // Otros servicios de aplicación
            services.AddScoped<IPaypalService, PaypalService>();
            services.AddScoped<ICreateSunscription, CreateSunscription>();
            services.AddScoped<ISyncService, SyncService>();

            return services;
        }

        // 3. SERVICIOS SINGLETON (Sin estado / Utilidades globales)
        public static IServiceCollection AddSingletonServices(this IServiceCollection services)
        {
            services.AddSingleton<IHashService, HashService>();
            services.AddSingleton<IGestorArchivos, GestorArchivosService>();
            services.AddSingleton<IConversionUtils, ConversionUtils>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddSingleton<CultureHelper>();
            services.AddSingleton<IPaypalOrderTrackingService, PaypalOrderTrackingService>();
            services.AddSingleton<IPaypalOrderService, PaypalOrderService>();
            services.AddSingleton<IPaypalSubscriptionService, PaypalSubscriptionService>();
            services.AddSingleton<IPayPalOrderMappingService, PayPalOrderMappingService>();
            services.AddSingleton<IPayPalHttpClient, PayPalHttpClient>();
            services.AddSingleton<IPaypalRefundService, PaypalRefundService>();
            services.AddSingleton<IPageLinkGenerator, PageLinkGenerator>();
            services.AddSingleton<IPaginationHelper, PaginationHelper>();
            services.AddSingleton<IPolicyExecutor, PolicyExecutor>();
            services.AddSingleton<IPolicyHandler, PolicyHandler>();
            services.AddSingleton<IImageOptimizerService, ImageOptimizerService>();
            services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            return services;
        }

        // 4. SERVICIOS DE FONDO
        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<StockCheckBackgroundService>();
            return services;
        }
    }
}