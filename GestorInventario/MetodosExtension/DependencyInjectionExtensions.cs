using GestorInventario.Application.PaginacionLogica;
using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Application.Services.Authentication.Strategies;
using GestorInventario.Application.Services.Authentication.Strategies.Login;
using GestorInventario.Application.Services.Authentication.Token_generation;
using GestorInventario.Application.Services.Background_Services;
using GestorInventario.Application.Services.Common;
using GestorInventario.Application.Services.Email_Services;
using GestorInventario.Application.Services.External_Sevices;
using GestorInventario.Application.Services.External_Sevices.Refunds;
using GestorInventario.Application.Services.Generic_Services;
using GestorInventario.Application.Services.Mapping;
using GestorInventario.Application.Services.Notifications;
using GestorInventario.Application.Services.Products;
using GestorInventario.Application.Services.Repository_Services;
using GestorInventario.Application.Services.Syncs;
using GestorInventario.Application.Services.User;
using GestorInventario.Context;
using GestorInventario.Files;
using GestorInventario.Images;
using GestorInventario.Infraestructure.Repositories.PedidoRepository;
using GestorInventario.Infraestructure.Repositories.ProveedorRepository;
using GestorInventario.Infrestructura;
using GestorInventario.Infrestructura.Repositories.AdminRepository;
using GestorInventario.Infrestructura.Repositories.CarritoRepository;
using GestorInventario.Infrestructura.Repositories.NotificacionRepository;
using GestorInventario.Infrestructura.Repositories.PaymentRepository;
using GestorInventario.Infrestructura.Repositories.PaypalRepository;
using GestorInventario.Infrestructura.Repositories.ProductoRepository;
using GestorInventario.Infrestructura.Repositories.RembolsoRepository;
using GestorInventario.Infrestructura.Repositories.UserRepository;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Application.Authentication;
using GestorInventario.Interfaces.Application.Common;
using GestorInventario.Interfaces.Application.ExternalServices;
using GestorInventario.Interfaces.Application.Services;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Renderer;
using GestorInventario.Shared.Utilities;
using GestorInventario.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;


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
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IBaseEmail, BaseEmail>();
            return services;
        }

        // 2. CAPA de LÓGICA DE NEGOCIO: Servicios de Aplicación
   
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Servicios de Auth y Seguridad
            services.AddScoped<ITokenService, TokenService>();
            services.AddScoped<ITokenGenerator, TokenGenerator>();
            services.AddScoped<IAuthService, AuthService>();           
            services.AddScoped<IRefreshTokenGenerator, RefreshTokenGenerator>();
         
            services.AddScoped<ILoginGenerator, LoginGenerator>();
           services.AddHttpClient<ICallMeBotService, CallMeBotService>();

            // Servicios de Notificaciones y Documentos
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddScoped<IBarCodeService, BarCodeService>();
            services.AddScoped<IStockCheckService, StockCheckService>();
           

            // Servicios de Gestión (Core Business)
            services.AddScoped<IUserManagementService, UserManagementService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IProductManagementService, ProductManagementService>();
            services.AddScoped<IPedidoManagementService, PedidoManagementService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICarritoService, CarritoService>();
            services.AddScoped<INotificationService, NotificacionService>();
            // Otros servicios de aplicación
            services.AddScoped<IPaypalService, PaypalService>();
            services.AddScoped<ICreateSunscription, CreateSunscription>();
            services.AddScoped<ISyncService, SyncService>();

            return services;
        }

     
        // 3. SERVICIOS SINGLETON (Sin estado)
        public static IServiceCollection AddSingletonServices(this IServiceCollection services)
        {
            // --- Integraciones de Pagos (PayPal) ---
            services.AddSingleton<IPayPalHttpClient, PayPalHttpClient>();
            services.AddSingleton<IPayPalOrderMappingService, PayPalOrderMappingService>();
            services.AddSingleton<IPaypalOrderService, PaypalOrderService>();
            services.AddSingleton<IPaypalOrderTrackingService, PaypalOrderTrackingService>();
            services.AddSingleton<IPaypalSubscriptionService, PaypalSubscriptionService>();
            services.AddSingleton<IPaypalRefundService, PaypalRefundService>();
         
            // --- Resiliencia y Manejo de Fallos (Políticas) ---
            services.AddSingleton<IPolicyExecutor, PolicyExecutor>();
            services.AddSingleton<IPolicyHandler, PolicyHandler>();

            // --- UI, Paginación y Localización ---
            services.AddSingleton<IPaginationHelper, PaginationHelper>();
            services.AddSingleton<IPageLinkGenerator, PageLinkGenerator>();
            services.AddSingleton<CultureHelper>();

            // --- Seguridad y Gestión de Archivos/Media ---
            services.AddSingleton<IHashService, HashService>();
            services.AddSingleton<IGestorArchivos, GestorArchivosService>();
            services.AddSingleton<IImageOptimizerService, ImageOptimizerService>();

            // --- Infraestructura y Utilidades del Sistema ---
            services.AddSingleton<IConversionUtils, ConversionUtils>();
            services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<IUrlService, UrlService>();
            services.AddSingleton<IBarCodeImageRenderer, BarCodeImageRenderer>();
            services.AddSingleton<IBarCodeImageStorage, BarCodeImageStorage>();
            return services;
        }

        // 4. SERVICIOS DE FONDO
        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            services.AddHostedService<StockCheckBackgroundService>();
            services.AddHostedService<NotificacionCleanupService>();
            return services;
        }
        //5. SERVICIOS QUE REQUIEREN CONFIGURACION MANUAL
        public static IServiceCollection AddCacheServices(this IServiceCollection services, bool useRedis)
        {
           
            services.AddSingleton<ICacheService, HybridCacheService>(provider =>
            {
                var redis = provider.GetRequiredService<IDistributedCache>();
                var memoryCache = provider.GetRequiredService<IMemoryCache>();
             
                IConnectionMultiplexer connectionMultiplexer = null;
                if (useRedis)
                {
                    connectionMultiplexer = provider.GetService<IConnectionMultiplexer>();
                }
                return new HybridCacheService(redis,memoryCache,connectionMultiplexer);

            });
       
            return services;
        }
    }
}