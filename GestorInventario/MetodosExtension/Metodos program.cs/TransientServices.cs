
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


namespace GestorInventario.MetodosExtension.Metodos_program.cs
{
    public static  class TransientServices
    {
        public static IServiceCollection AddTransientServices(this IServiceCollection services) {
            services.AddTransient<IPageLinkGenerator,PageLinkGenerator>();           
            services.AddTransient<IPolicyExecutor, PolicyExecutor>();
            services.AddTransient<IUserRepository,UserRepository>();
            services.AddTransient<IGestorArchivos, GestorArchivosService>();
            services.AddTransient<IHashService,HashService>();
            services.AddTransient<ICarritoRepository, CarritoRepository>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<ITokenService,TokenService>();
            services.AddTransient<IAdminRepository, AdminRepository>();         
            services.AddTransient<IUnitOfWork, UnitOfWork>();
            services.AddTransient<IPaypalOrderTrackingService, PaypalOrderTrackingService>();
            services.AddTransient<IPedidoRepository, PedidoRepository>();
            services.AddTransient<IPolicyHandler,PolicyHandler>();
            services.AddTransient<IProductoRepository, ProductoRepository>();
            services.AddTransient<IRembolsoRepository, RembolsoRepository>();
            services.AddTransient<IProveedorRepository, ProveedorRepository>();
            services.AddTransient<IPdfService, PdfService>();
            services.AddTransient<IPaypalRepository, PaypalRepository>();
            services.AddTransient<IEncryptionService, EncryptionService>();
            services.AddTransient<IPaypalOrderTrackingService, PaypalOrderTrackingService>();
            services.AddTransient<IPaymentRepository, PaymentRepository>();
            services.AddTransient<IImageOptimizerService,ImageOptimizerService>();
            services.AddTransient<ITokenGenerator, TokenGenerator>();           
            services.AddTransient<IBarCodeService, BarCodeService>();
            services.AddTransient<ICurrentUserAccessor,CurrentUserAccessor>();
            services.AddTransient<IPaginationHelper, PaginationHelper>();
            services.AddTransient<ICarritoService, CarritoService>();
            services.AddTransient<IConversionUtils, ConversionUtils>();
            services.AddTransient<IPayPalHttpClient, PayPalHttpClient>();
            services.AddTransient<ICreateSunscription, CreateSunscription>();
            services.AddTransient<CultureHelper>();
            services.AddTransient<IPayPalMappingUtils, PayPalMappingUtils>();
            services.AddTransient<IPaypalOrderService, PaypalOrderService>();
            services.AddTransient<IPaypalSubscriptionService, PaypalSubscriptionService>();
       
            services.AddTransient<IUserManagementService,UserManagementService>();
            services.AddTransient<IPasswordResetService, PasswordResetService>();
            services.AddTransient<IProductManagementService, ProductManagementService>();
            services.AddTransient<IPedidoManagementService, PedidoManagementService>();
            services.AddTransient<IPaymentService, PaymentService>();
            services.AddTransient<IPaypalService,PaypalService>();
            services.AddTransient<IAuthService, AuthService>();
            services.AddTransient<IPayPalOrderMappingService, PayPalOrderMappingService>();
            services.AddScoped<IStockNotificationService, StockNotificationService>();
            services.AddHostedService <StockCheckBackgroundService> ();
            services.AddTransient<ISyncService,SyncService>();
            services.AddTransient<IReembolsoNotificationService, ReembolsoNotificationService>();
            services.AddTransient<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddTransient<IPaypalPartialRefundService, PaypalPartialRefundService>();
            services.AddTransient<IPaypalFullRefundService, PaypalFullRefundService>();
            return services;

        }
    }
}
