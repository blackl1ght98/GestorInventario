using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Repositories;
using GestorInventario.Infrastructure.Repositories;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using GestorInventario.Application.Politicas_Resilencia;
using GestorInventario.PaginacionLogica;
using GestorInventario.Middlewares;
using GestorInventario.Configuracion;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.IO.Pipelines;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Sockets;
using System.Collections;

var builder = WebApplication.CreateBuilder(args);
// Agregar variables de entorno a la configuración
builder.Configuration.AddEnvironmentVariables();

string secret = builder.Configuration["ClaveJWT"];
//Para usar las variables de entorno ejecutar el archivo SetEnvironmentVariable.ps1
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles); ;

var isDocker = Environment.GetEnvironmentVariable("IS_DOCKER") == "true";
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var dbPassword = Environment.GetEnvironmentVariable("DB_SA_PASSWORD");
string connectionString;

if (isDocker)
{
    connectionString = $"Data Source={dbHost};Initial Catalog={dbName};User ID=sa;Password={dbPassword};TrustServerCertificate=True";
}
else
{
    connectionString = $"Data Source={dbHost};Initial Catalog={dbName};Integrated Security=True;TrustServerCertificate=True";
}
builder.Services.AddDbContext<GestorInventarioContext>(options =>
{
    options.UseSqlServer(connectionString);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
}
);
builder.Services.AddMvc();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Logging.AddLog4Net();

//Servicios
builder.Services.AddTransient<GenerarPaginas>();
builder.Services.AddTransient<PaginacionMetodo>();
builder.Services.AddTransient<IGestorArchivos, GestorArchivosService>();
builder.Services.AddTransient<INewStringGuid, NewStringGuid>();
builder.Services.AddTransient<HashService>();
builder.Services.AddTransient<ICarritoRepository, CarritoRepository>();
builder.Services.AddTransient<IChangePassService, ChangePassService>();
builder.Services.AddTransient<IConfirmEmailService, ConfirmEmailService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<TokenService>();
builder.Services.AddTransient<ITokenGenerator, TokenGenerator>();
builder.Services.AddTransient<TokenGenerator>();
builder.Services.AddTransient<IAdminRepository,AdminRepository>();
builder.Services.AddTransient<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();
builder.Services.AddTransient<IPedidoRepository,PedidoRepository>();
builder.Services.AddTransient<PolicyHandler>();
builder.Services.AddTransient<IProductoRepository, ProductoRepository>();
builder.Services.AddTransient<IProveedorRepository,ProveedorRepository>();  
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    string redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
//    if (string.IsNullOrEmpty(redisConnectionString))
//    {

//        redisConnectionString = builder.Configuration["Redis:ConnectionString"];
//    }
//    options.Configuration = redisConnectionString;
//    options.InstanceName = "SampleInstance";
//});

bool useRedis = bool.Parse(Environment.GetEnvironmentVariable("USE_REDIS") ?? "false");

if (useRedis)
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        string redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            redisConnectionString = builder.Configuration["Redis:ConnectionString"];
        }
        options.Configuration = redisConnectionString;
        options.InstanceName = "SampleInstance";
    });
}

/* ¿Como cambiar el metodo de autenticación?
 * 1º Cerrar todas las sesiones activas
 * 2º Comentar la obción actual y descomantar la deseada
 * 3º En la parte de middleware se efectuara lo mismo conforme a la configuración elegida
 * 4º Vamos al archivo TokenService.cs ubicado en Application-->Services-->TokenService.cs y ahi hacemos lo mismo comentamos la opcion
 * actual y descomentamos la que se vaya a usar
 */
//builder.ConfiguracionSimetrica(builder.Configuration);
//builder.ConfiguracionAsimetricaFija(builder.Configuration);
builder.ConfiguracionAsimetricaDinamica(builder.Configuration);
//builder.ConfiguracionSimetricaSencilla(builder.Configuration);


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
    options.IdleTimeout = TimeSpan.FromMinutes(50);
    //evita que las operaciones lentas bloqueen el servidor
    options.IOTimeout = TimeSpan.FromMinutes(30);
    //La cookie solo es accesible por el servidor
    options.Cookie.HttpOnly = true;
    //Cuando el usuario rechaza las cookies esto no se vera afectado
    options.Cookie.IsEssential = true;
    //Cuando el usuario rechaza las cookies esto no se vera afectado
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseRouting();
//Identifica quien es el usuario
app.UseAuthentication();
//Determina que puede hacer o no el usuario
app.UseAuthorization();
app.UseSession();


//app.MiddlewareAutenticacionSimetrica(builder);
//app.MiddlewareAutenticacionAsimetricaFija(builder);
app.MiddlewareAutenticacionAsimetricaDinamica(builder);
//app.MiddlewareAutenticacionSimetricaSencilla(builder);


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
