using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Application.Services.External_Sevices;
using GestorInventario.Application.Services.Generic_Services;
using GestorInventario.Infraestructure.Repositories;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Utils;
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
            services.AddTransient<HashService>();
            services.AddTransient<ICarritoRepository, CarritoRepository>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<TokenService>();
            services.AddTransient<IAdminRepository, AdminRepository>();
            services.AddTransient<IAuthRepository, AuthRepository>();
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
            services.AddTransient<IPaypalSubscriptionDetailService, PaypalSubscriptionDetailService>();
            services.AddTransient<CultureHelper>();
            services.AddTransient<IPayPalMappingUtils, PayPalMappingUtils>();
            services.AddTransient<IPaypalOrderService, PaypalOrderService>();
            services.AddTransient<IPaypalRefundService, PaypalRefundService>();
            services.AddTransient<IPaypalSubscriptionService, PaypalSubscriptionService>();
            services.AddTransient<IAuditService, AuditService>();
            return services;

        }
    }
}
