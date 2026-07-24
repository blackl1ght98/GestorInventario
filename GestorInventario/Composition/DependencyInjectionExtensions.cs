using GestorInventario.Application.MetodosPaginacion;
using GestorInventario.Application.RetryPolicy;
using GestorInventario.Application.Services;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Application.Services.Authentication.Resolvers;
using GestorInventario.Application.Services.Authentication.Services;
using GestorInventario.Application.Services.Authentication.Strategies.Login;
using GestorInventario.Application.Services.Authentication.Token_generation;
using GestorInventario.Application.Services.BackgroundServices;
using GestorInventario.Application.Services.Carrito;
using GestorInventario.Application.Services.Common;
using GestorInventario.Application.Services.ExternalServices;
using GestorInventario.Application.Services.ExternalServices.Refunds;
using GestorInventario.Application.Services.Files;
using GestorInventario.Application.Services.Mapping;
using GestorInventario.Application.Services.Notification;
using GestorInventario.Application.Services.Orders;
using GestorInventario.Application.Services.Payment;
using GestorInventario.Application.Services.Paypal.Plans;
using GestorInventario.Application.Services.Paypal.Subscription;
using GestorInventario.Application.Services.PDFService;
using GestorInventario.Application.Services.Products;
using GestorInventario.Application.Services.Syncs;
using GestorInventario.Application.Services.User;
using GestorInventario.Context;
using GestorInventario.Files;
using GestorInventario.Infrastructure;
using GestorInventario.Infrastructure.Repositories.AdminRepository;
using GestorInventario.Infrastructure.Repositories.CarritoRepository;
using GestorInventario.Infrastructure.Repositories.NotificacionRepository;
using GestorInventario.Infrastructure.Repositories.PaymentRepository;
using GestorInventario.Infrastructure.Repositories.PaypalRepository;
using GestorInventario.Infrastructure.Repositories.PedidoRepository;
using GestorInventario.Infrastructure.Repositories.ProductoRepository;
using GestorInventario.Infrastructure.Repositories.ProveedorRepository;
using GestorInventario.Infrastructure.Repositories.RembolsoRepository;
using GestorInventario.Infrastructure.Repositories.UserRepository;
using GestorInventario.Interfaces.Application.Email;
using GestorInventario.Interfaces.Application.RetryPolicy;
using GestorInventario.Interfaces.Application.Services.Authentication;
using GestorInventario.Interfaces.Application.Services.Background;
using GestorInventario.Interfaces.Application.Services.Common;
using GestorInventario.Interfaces.Application.Services.ExternalServices;
using GestorInventario.Interfaces.Application.Services.Files;
using GestorInventario.Interfaces.Application.Services.Notification;
using GestorInventario.Interfaces.Application.Services.Order;
using GestorInventario.Interfaces.Application.Services.Payment;
using GestorInventario.Interfaces.Application.Services.Paypal;
using GestorInventario.Interfaces.Application.Services.Products;
using GestorInventario.Interfaces.Application.Services.ShopCart;
using GestorInventario.Interfaces.Application.Services.Sync;
using GestorInventario.Interfaces.Application.Services.User;
using GestorInventario.Interfaces.Infraestructure.Common;
using GestorInventario.Interfaces.Infraestructure.Repositories;
using GestorInventario.Interfaces.Renderer;
using GestorInventario.Notifications.EmailServices;
using GestorInventario.Notifications.Mensajes.Telegram;
using GestorInventario.Renderer.Barcode;
using GestorInventario.Renderer.Images;
using GestorInventario.Renderer.PDF;
using GestorInventario.Shared.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;


namespace GestorInventario.Composition
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
            services.AddScoped<TokenStrategyResolver>();
            services.AddScoped<LoginStrategyResolver>();
            services.AddScoped<MidlewareResolver>();
           
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
            services.AddScoped<IRembolsoNotification, RembolsoNotification>();
            services.AddScoped<IPlanService, PlanService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
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
            services.AddSingleton<IPayPalInvoiceRenderer, PayPalInvoicePdfRenderer>();
           
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
         
            services.AddSingleton<ICurrentUserAccessor, CurrentUserAccessor>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddSingleton<IUrlService, UrlService>();
            services.AddSingleton<IBarCodeImageRenderer, BarCodeImageRenderer>();
            services.AddSingleton<IBarCodeImageStorage, BarCodeImageStorage>();


            //-- Tokens --
            services.AddSingleton<ITokenClaimsBuilder,TokenClaimsBuilder>();
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
        public static IServiceCollection AddHybridCacheService(this IServiceCollection services, bool useRedis)
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