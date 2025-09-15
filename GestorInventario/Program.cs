using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.Application.Services;
using GestorInventario.Application.Services.Authentication;
using GestorInventario.Application.Services.Generic_Services;
using GestorInventario.Configuracion;
using GestorInventario.Configuracion.Strategies;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Repositories;
using GestorInventario.Infraestructure.Utils;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Middlewares;
using GestorInventario.Middlewares.Strategis;
using GestorInventario.PaginacionLogica;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Net.Http.Headers;
using StackExchange.Redis;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json.Serialization;
using AspNetCoreHttp = Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
// Agregar variables de entorno a la configuración
builder.Configuration.AddEnvironmentVariables();
string secret = Environment.GetEnvironmentVariable("ClaveJWT")?? builder.Configuration["ClaveJWT"];
//Para que no salte una excepcion en consultas que son recursivas
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);
var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? builder.Configuration["DataBaseConection:DBHost"];
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? builder.Configuration["DataBaseConection:DBName"];
var dbUserName = Environment.GetEnvironmentVariable("DB_USERNAME") ?? builder.Configuration["DataBaseConection:DBUserName"];
var dbPassword = Environment.GetEnvironmentVariable("DB_SA_PASSWORD") ?? builder.Configuration["DataBaseConection:DBPassword"];

string connectionString=isDocker ? $"Data Source={dbHost};Initial Catalog={dbName};User ID=sa;Password={dbPassword};TrustServerCertificate=True" 
                                 : $"Data Source={dbHost};Initial Catalog={dbName};User ID={dbUserName};Password={dbPassword};TrustServerCertificate=True";
if (dbUserName == null || dbUserName == "" || dbPassword == null || dbPassword == "")
{
    connectionString = $"Data Source={dbHost};Initial Catalog={dbName};Integrated Security=True;TrustServerCertificate=True";


}
builder.Services.AddDbContext<GestorInventarioContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

builder.Services.AddMvc();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Logging.AddLog4Net();
builder.Services.AddAntiforgery();
// Servicio usado para peticiones HTTP
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<GenerarPaginas>();
builder.Services.AddTransient<PaginacionMetodo>();
builder.Services.AddTransient<PolicyExecutor>();
builder.Services.AddTransient<UtilityClass>();
builder.Services.AddTransient<IGestorArchivos, GestorArchivosService>();
builder.Services.AddTransient<HashService>();
builder.Services.AddTransient<ICarritoRepository, CarritoRepository>();
builder.Services.AddTransient<IConfirmEmailService, ConfirmEmailService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<TokenService>();
builder.Services.AddTransient<IAdminRepository, AdminRepository>();
builder.Services.AddTransient<IAuthRepository, AuthRepository>();
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();
builder.Services.AddTransient<IPaypalService,PaypalService>();
builder.Services.AddTransient<IPedidoRepository, PedidoRepository>();
builder.Services.AddTransient<PolicyHandler>();
builder.Services.AddTransient<IProductoRepository, ProductoRepository>();
builder.Services.AddTransient<IRembolsoRepository, RembolsoRepository>();
builder.Services.AddTransient<IProveedorRepository, ProveedorRepository>();
builder.Services.AddTransient<IPdfService, PdfService>();
builder.Services.AddTransient<IPaypalRepository, PaypalRepository>();
builder.Services.AddTransient<IEncryptionService, EncryptionService>();
builder.Services.AddHttpClient<IPaypalService, PaypalService>(client =>
{
    client.BaseAddress = new Uri("https://api-m.sandbox.paypal.com/");
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddTransient<IBarCodeService, BarCodeService>();
builder.Services.AddAutoMapper(cfg =>
{
    cfg.LicenseKey = Environment.GetEnvironmentVariable("LicenseKeyAutoMapper") ?? builder.Configuration["LicenseKeyAutoMapper"]; ;
    // Configurar AutoMapper para escanear los perfiles en el ensamblado actual
    cfg.AddMaps(Assembly.GetExecutingAssembly());
});
builder.Services.AddTransient<IPaymentRepository, PaymentRepository>();

builder.Services.AddWebOptimizer(pipeline =>
{
    // Minificar CSS y JS
    pipeline.MinifyCssFiles("css/**/*.css");
    pipeline.MinifyJsFiles("js/**/*.js");

    // Agrupar archivos (bundle)
    pipeline.AddCssBundle("/css/bundle.css", "css/*.css");
    pipeline.AddJavaScriptBundle("/js/bundle.js", "js/*.js");
});
builder.Services.AddTransient<ImageOptimizerService>();




// Comprobamos si Redis se está usando...
bool useRedis = bool.Parse(Environment.GetEnvironmentVariable("USE_REDIS") ?? "false");

// Si estamos usando Redis
if (useRedis)
{
    // Guardamos en una variable las cadenas de conexión
    string redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING")
                                    ?? builder.Configuration["Redis:ConnectionString"]
                                    ?? builder.Configuration["Redis:ConnectionStringLocal"]!;

    // Comprobamos qué cadena de conexión se está usando
    Console.WriteLine($"Attempting to use Redis connection string: {redisConnectionString}");
   
    try
    {
        // Intenta crear la conexión con Redis
        var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
       
        // Llama al servicio de Redis y configura la conexión
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "SampleInstance";
        });
        builder.Services.AddDataProtection().PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect("redis:6379"), "DataProtection-Keys");
        // Al servicio IConnectionMultiplexer se le pasa la conexión creada con Redis
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider => connectionMultiplexer);
        // Se muestra la cadena de conexión que se ha usado
        Console.WriteLine($"Using Redis connection string: {redisConnectionString}");
    }
    catch (Exception ex)
    {
        // Si la conexión falla, muestra la cadena de conexión y el error producido
        Console.WriteLine($"Failed to connect using Redis connection string: {redisConnectionString}. Error: {ex.Message}");
        // Usa la cadena de conexión local como alternativa
        string redisConnectionStringLocal = builder.Configuration["Redis:ConnectionStringLocal"];
        // Muestra la cadena de conexión local que se va a usar
        Console.WriteLine($"Attempting to use local Redis connection string: {redisConnectionStringLocal}");
        // Intenta crear la conexión con la cadena de conexión local
        var connectionMultiplexerLocal = ConnectionMultiplexer.Connect(redisConnectionStringLocal);
        // Configura el servicio de Redis con la cadena de conexión local
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionStringLocal;
            options.InstanceName = "SampleInstance";
        });
        // Al servicio IConnectionMultiplexer se le pasa la conexión local creada
        builder.Services.AddSingleton<IConnectionMultiplexer>(provider => connectionMultiplexerLocal);
        // Se muestra la cadena de conexión local que se ha usado
        Console.WriteLine($"Using local Redis connection string: {redisConnectionStringLocal}");
    }
}
// Servicio usado para guardar datos en memoria
builder.Services.AddMemoryCache();

// Se configura el servicio ITokenGenerator con sus dependencias
builder.Services.AddTransient<ITokenGenerator, TokenGenerator>(provider =>
{
    //Se resuelve las dependencias necesarias para crear una instancia de "TokenGenerator", esto asegura que se configure correctamente
    var redis = provider.GetRequiredService<IDistributedCache>();
    var memoryCache = provider.GetRequiredService<IMemoryCache>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    var context = provider.GetRequiredService<GestorInventarioContext>();
    var logger = provider.GetRequiredService<ILogger<TokenGenerator>>();
    var httpContextAccessor = provider.GetRequiredService<IHttpContextAccessor>();
    // Inicialmente se establece en null ya que el valor se asignará si se está usando Redis
    IConnectionMultiplexer connectionMultiplexer = null;
    // Si se está usando Redis...
    if (useRedis)
    {
        // Se obtiene la conexión de Redis del proveedor de servicios
        connectionMultiplexer = provider.GetService<IConnectionMultiplexer>();
    }
    // Devuelve una nueva instancia de TokenGenerator con sus dependencias
    return new TokenGenerator(context, configuration, httpContextAccessor, memoryCache, logger, redis, connectionMultiplexer);

});

builder.Services.AddTransient<IRefreshTokenMethod, RefreshTokenMethod>(provider =>
{
    var redis = provider.GetRequiredService<IDistributedCache>();
    var memoryCache = provider.GetRequiredService<IMemoryCache>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    var context = provider.GetRequiredService<GestorInventarioContext>();
    var logger = provider.GetRequiredService<ILogger<RefreshTokenMethod>>();
    // Inicialmente se establece en null ya que el valor se asignará si se está usando Redis
    IConnectionMultiplexer connectionMultiplexer = null;
    // Si se está usando Redis...
    if (useRedis)
    {
        // Se obtiene la conexión de Redis del proveedor de servicios
        connectionMultiplexer = provider.GetService<IConnectionMultiplexer>();
    }
    return new RefreshTokenMethod(context, configuration, memoryCache, redis, connectionMultiplexer, logger);
});



// Selecciona la estrategia según una configuración (puedes usar appsettings.json o variables de entorno)
string authStrategy = builder.Configuration["AuthMode"] ?? "Symmetric";
IAuthenticationStrategy strategy = authStrategy switch
{
    "AsymmetricDynamic" => new AsymmetricDynamicAuthenticationStrategy(),
    "AsymmetricFixed" => new AsymmetricFixedAuthenticationStrategy(),
    "Symmetric" => new SymmetricAuthenticationStrategy(),
    _ => throw new ArgumentException("Estrategia de autenticación no válida")
};

var configurator = new AuthenticationConfigurator(strategy);
configurator.Configure(builder, builder.Configuration);
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SameSite = AspNetCoreHttp.SameSiteMode.None;
   
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});


/*

    * options.Preload = true;: Esta opción indica que quieres incluir tu sitio en la lista de precarga de HSTS. 
    Los navegadores mantienen esta lista de sitios que deben ser accedidos solo a través de HTTPS.

    * options.IncludeSubDomains = true;: Esta opción indica que la política de HSTS debe aplicarse a todos los subdominios 
    del dominio actual.

    * options.MaxAge = TimeSpan.FromDays(60);: Esta opción establece la cantidad de tiempo que los navegadores deben recordar 
    que este sitio solo debe ser accedido usando HTTPS.

    * options.ExcludedHosts.Add("example.com"); y options.ExcludedHosts.Add("www.example.com");: Estas opciones permiten excluir 
    ciertos hosts de la política de HSTS. En este caso, “example.com” y “www.example.com” están excluidos.
*/
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);
    //options.ExcludedHosts.Add("example.com");
    //options.ExcludedHosts.Add("www.example.com");
});
builder.Services.AddMvc();

builder.Services.AddSession(options =>
{
    //Si el usuario esta inactivo 20 minutos la sesion se cierra automaticamente
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    //evita que las operaciones lentas bloqueen el servidor
    options.IOTimeout = TimeSpan.FromMinutes(30);
    //La cookie solo es accesible por el servidor
    options.Cookie.HttpOnly = true;
    //Cuando el usuario rechaza las cookies esto no se vera afectado
    options.Cookie.IsEssential = true;
    //Cuando el usuario rechaza las cookies esto no se vera afectado
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});



// Seleccionar la estrategia usando un switch expression
IAuthProcessingStrategy strategyMiddleware = authStrategy switch
{
    "AsymmetricDynamic" => new DynamicAsymmetricAuthStrategy(),
    "AsymmetricFixed" => new FixedAsymmetricAuthStrategy(),
    "Symmetric" => new SymmetricAuthStrategy(),
    _ => throw new ArgumentException("Estrategia de autenticación no válida.")
};
var app = builder.Build();
app.UseWebOptimizer();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
  
    app.UseHsts();
}

app.UseHttpsRedirection();

// Configurar cache para archivos estáticos
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
       
          var extension = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
        if (extension is ".css" or ".js" or ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".woff2")
        {
            var durationInSeconds = 60 * 60 * 24 * 365; // 1 año
            ctx.Context.Response.Headers[HeaderNames.CacheControl] = $"public, max-age={durationInSeconds}, immutable";
        }
        else
        {
            ctx.Context.Response.Headers[HeaderNames.CacheControl] = "no-cache";
        }
        
    }
});
// Middleware para procesar imágenes con parámetros
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    if (path.StartsWith("/imagenes/") && context.Request.QueryString.HasValue)
    {
        var query = context.Request.Query;
        if (query.ContainsKey("width") || query.ContainsKey("height"))
        {
            int? width = query.TryGetValue("width", out var widthValue) &&
                         int.TryParse(widthValue, out var w) ? w : null;

            int? height = query.TryGetValue("height", out var heightValue) &&
                          int.TryParse(heightValue, out var h) ? h : null;

            var imagePath = path.Split('?')[0]; // Remover query string
            var imageService = context.RequestServices.GetService<ImageOptimizerService>();

            var processedImage = await imageService.ProcessImageOnDemand(imagePath, width, height);

            if (processedImage != null)
            {
                context.Response.ContentType = GetContentType(imagePath);
                await processedImage.CopyToAsync(context.Response.Body);
                return;
            }
        }
    }

    await next();
});

 string GetContentType(string path)
{
    return Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".webp" => "image/webp",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        _ => "application/octet-stream"
    };
}
app.UseCors();
app.UseRouting();
app.UseAuthentication(); // Identifica al usuario
app.UseSession();
app.UseAuthProcessing(builder, strategyMiddleware); // Establece HttpContext.User
app.UseAuthorization(); // Evalúa políticas y roles

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();