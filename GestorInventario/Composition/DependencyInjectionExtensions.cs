using GestorInventario.Application.Interfaces.Application.Services.Authentication.Jwt;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Resolvers;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.Services;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.TokenGeneration;
using GestorInventario.Application.Interfaces.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Application.Interfaces.Application.Services.BackgroundServices;
using GestorInventario.Application.Interfaces.Application.Services.Common;
using GestorInventario.Application.Interfaces.Application.Services.Email;
using GestorInventario.Application.Interfaces.Application.Services.Files;
using GestorInventario.Application.Interfaces.Application.Services.Orders;
using GestorInventario.Application.Interfaces.Application.Services.Payment;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.Mapping;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.PaypalApi.Subscriptions;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.Plans;
using GestorInventario.Application.Interfaces.Application.Services.Paypal.Subscription;
using GestorInventario.Application.Interfaces.Application.Services.PDFService;
using GestorInventario.Application.Interfaces.Application.Services.Products;
using GestorInventario.Application.Interfaces.Application.Services.Provider;
using GestorInventario.Application.Interfaces.Application.Services.Refunds;
using GestorInventario.Application.Interfaces.Application.Services.ShopCart;
using GestorInventario.Application.Interfaces.Application.Services.Syncs;
using GestorInventario.Application.Interfaces.Application.Services.User;
using GestorInventario.Application.Interfaces.Infrastructure.Common;
using GestorInventario.Application.Interfaces.Infrastructure.Repositories;
using GestorInventario.Application.Interfaces.Infrastructure.RetryPolicy;
using GestorInventario.Application.Interfaces.MetodosPaginacion;
using GestorInventario.Application.Interfaces.Notifications.EmailServices;
using GestorInventario.Application.Interfaces.Notifications.InternalNotification;
using GestorInventario.Application.Interfaces.Notifications.SendNotification.Email;
using GestorInventario.Application.Interfaces.Notifications.SendNotification.Telegram;
using GestorInventario.Application.Interfaces.Renderer.Barcode;
using GestorInventario.Application.Interfaces.Renderer.Images;
using GestorInventario.Application.Interfaces.Renderer.PDF;
using GestorInventario.Application.Interfaces.Services.Authentication.Resolvers;
using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Application.PaginationMethods;
using GestorInventario.Application.Services.Authentication.Jwt;
using GestorInventario.Application.Services.Authentication.Resolvers;
using GestorInventario.Application.Services.Authentication.Services;
using GestorInventario.Application.Services.Authentication.TokenGeneration;
using GestorInventario.Application.Services.Authentication.TokenGeneration.Generators;
using GestorInventario.Application.Services.BackgroundServices;
using GestorInventario.Application.Services.Common;
using GestorInventario.Application.Services.Files;
using GestorInventario.Application.Services.Orders;
using GestorInventario.Application.Services.Payment;
using GestorInventario.Application.Services.Paypal.Mapping;
using GestorInventario.Application.Services.Paypal.PaypalApi;
using GestorInventario.Application.Services.Paypal.PaypalApi.Order;
using GestorInventario.Application.Services.Paypal.PaypalApi.Refunds;
using GestorInventario.Application.Services.Paypal.PaypalApi.Subscriptions;
using GestorInventario.Application.Services.Paypal.Plans;
using GestorInventario.Application.Services.Paypal.Subscription;
using GestorInventario.Application.Services.PDFService;
using GestorInventario.Application.Services.Products;
using GestorInventario.Application.Services.Provider;
using GestorInventario.Application.Services.Refunds;
using GestorInventario.Application.Services.ShopCart;
using GestorInventario.Application.Services.Syncs;
using GestorInventario.Application.Services.User;
using GestorInventario.Context;
using GestorInventario.Files;
using GestorInventario.Infrastructure;
using GestorInventario.Infrastructure.Handlers.RetryPolicy;
using GestorInventario.Infrastructure.Helpers.Pagination;
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
using GestorInventario.Notifications.EmailServices;
using GestorInventario.Notifications.InternalNotification;
using GestorInventario.Notifications.SendNotification.Email;
using GestorInventario.Notifications.SendNotification.Telegram;
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
            services.AddScoped<ITokenStrategyResolver, TokenStrategyResolver>();
            services.AddScoped<ILoginStrategyResolver, LoginStrategyResolver>();
            services.AddScoped<IMidlewareResolver, MidlewareResolver>();
           
            services.AddHttpClient<ICallMeBotService, CallMeBotService>();
            
            // Servicios de Notificaciones y Documentos
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPdfService, PdfService>();
            services.AddScoped<IBarCodeService, BarCodeService>();
            services.AddScoped<IStockCheckService, StockCheckService>();
           

            // Servicios de Gestión (Core Business)
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IShopCartService, ShopCartService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IRefundService,RefundService>();
            services.AddScoped<IProviderService, ProviderService>();
            // Otros servicios de aplicación
            services.AddScoped<IRefundNotification, RefundNotification>();
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
            services.AddSingleton<PolicyHandler>();

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
            services.AddSingleton<IJwtTokenSettings,JwtTokenSettings>();
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
           
            services.AddSingleton<IHybridCacheService, HybridCacheService>(provider =>
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