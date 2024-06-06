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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles); ;
string connectionString;
string secret;

//Este proyecto esta usando la plantilla MVC que quiere decir modelo vista controlador
//El agregado de la cadena de conexion no cambia la forma de hacerlo con esto me refiero que se hace igual en un proyecto web api y uno mvc
//La cadena de conexion se encuentra en el archivo de secretos de usuario
bool isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
if (!isDevelopment)
{
    connectionString = builder.Configuration["ConnectionStrings:CONNECTION_STRING"];
    secret = builder.Configuration["ClaveJWT"];
}
else
{
    connectionString = builder.Configuration.GetConnectionString("CONNECTION_STRING");
    secret = builder.Configuration["ClaveJWT"];
}
//Al igual que en un proyecto web api se pone la inyecion de dependencias
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


/* 1ºCerrar todas las sesiones activas: Antes de cambiar la estrategia de autenticación, es necesario que todos los usuarios cierren sesión.
   2ºSeleccionar el método de autenticación: Descomenta la línea de código que corresponde al método de autenticación que deseas utilizar. 
     Por ejemplo, si quieres cambiar de simétrico a asimétrico fijo, debes comentar la línea que dice 
     builder.ConfiguracionSimetrica(builder.Configuration); y descomentar la línea que dice 
     builder.ConfiguracionAsimetricaFija(builder.Configuration);.
   3ºSeleccionar el middleware: De manera similar, debes seleccionar el middleware correspondiente. Si has elegido 
     autenticación asimétrica fija, debes descomentar la línea que dice app.MiddlewareAutenticacionAsimetricaFija(builder);.
   4ºCambiar la generación de tokens: Finalmente, debes ir a TokenService y cambiar el modo en que se generan los tokens. Si estás usando 
     la generación de tokens simétrica y quieres cambiar a la asimétrica, debes comentar la línea correspondiente a la simétrica y descomentar 
     la asimétrica.
 */
//builder.ConfiguracionSimetrica(builder.Configuration);
builder.ConfiguracionAsimetricaFija(builder.Configuration);
//builder.ConfiguracionAsimetricaDinamica(builder.Configuration);


/*

    options.Preload = true;: Esta opción indica que quieres incluir tu sitio en la lista de precarga de HSTS. 
    Los navegadores mantienen esta lista de sitios que deben ser accedidos solo a través de HTTPS.

    options.IncludeSubDomains = true;: Esta opción indica que la política de HSTS debe aplicarse a todos los subdominios 
    del dominio actual.

    options.MaxAge = TimeSpan.FromDays(60);: Esta opción establece la cantidad de tiempo que los navegadores deben recordar 
    que este sitio solo debe ser accedido usando HTTPS.

    options.ExcludedHosts.Add("example.com"); y options.ExcludedHosts.Add("www.example.com");: Estas opciones permiten excluir 
    ciertos hosts de la política de HSTS. En este caso, “example.com” y “www.example.com” están excluidos.
*/
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);
    //Hace que no se le aplique la politica hsts 
    //options.ExcludedHosts.Add("example.com");
    //options.ExcludedHosts.Add("www.example.com");
});
//Cogido de la documentacion de microsoft esto permite manejar los casos en la que la pagina se cambia de sitio
//builder.Services.AddHttpsRedirection(options =>
//{
//    options.RedirectStatusCode = Status307TemporaryRedirect;
//    options.HttpsPort = 5001;
//});

builder.Services.AddMvc();
builder.Services.AddSession(options =>
{
    //Si el usuario esta inactivo 20 minutos la sesion se cierra automaticamente
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.IOTimeout = TimeSpan.FromMinutes(30); //evita que las operaciones lentas bloqueen el servidor
    options.Cookie.HttpOnly = true;//La cookie solo es accesible por el servidor
    options.Cookie.IsEssential = true;//Cuando el usuario rechaza las cookies esto no se vera afectado
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;//la cookie solo se envia por https
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
app.MiddlewareAutenticacionAsimetricaFija(builder);
//app.MiddlewareAutenticacionAsimetricaDinamica(builder);



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
