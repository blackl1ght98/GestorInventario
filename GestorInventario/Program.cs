
using GestorInventario.Composition;
using GestorInventario.Configuracion;
using GestorInventario.MetodosExtension;
using GestorInventario.Middlewares;
using GestorInventario.Shared.DTOS;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.Net.Http.Headers;
using QuestPDF.Infrastructure;
using System.Text.Json.Serialization;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

var builder = WebApplication.CreateBuilder(args);


// Agregar variables de entorno a la configuración
builder.Configuration.AddEnvironmentVariables();

//Variables compartidas
bool useRedis = bool.Parse(Environment.GetEnvironmentVariable("USE_REDIS") ?? "false");

//Para que no salte una excepcion en consultas que son recursivas
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

//Configuracion DB
builder.Services.AddDatabaseContext(builder.Configuration);
//CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

//Servicios generales
builder.Services.AddMemoryCache();
builder.Services.AddMvc();
builder.Services.AddLogging(b =>
{
    b.AddConsole();
    b.AddLog4Net();
});
builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();

//Servicios especificos 
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();
builder.Services.AddSingletonServices();
builder.Services.AddBackgroundServices();
//Construccion de la URL para el email
builder.Services.Configure<AppSettings>(
builder.Configuration.GetSection("App"));
//Configuracion de licencia para automapper
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddPayPalHttpClient(builder.Configuration);
builder.Services.AddAutoMapper(builder.Configuration);
builder.Services.AddWebOptimizer();

// Si estamos usando Redis lo configuramos
if (useRedis)
{
   builder.Services.ConfigureRedis(builder.Configuration);
}

builder.Services.AddHybridCacheService(useRedis);
//Servicios personalizados de autenticacion
builder.Services.AddConfigureAuthentication(builder.Configuration);

//Fin de los servicios personalizados de autenticacion 

builder.Services.ConfigureAntiforgery();

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);

});


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
    // Cambios para túnel
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
    options.Cookie.SameSite = SameSiteMode.None; // permite envío cross-site
});
builder.Services.AddDataProtection()
    .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
    {
        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
    });



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
app.Use(async (context, next) =>
{
    if (!context.Request.IsHttps)
    {
        context.Response.StatusCode = 400; 
        context.Response.ContentType = "text/html";

        var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var fileInfo = env.ContentRootFileProvider.GetFileInfo("Views/Shared/HttpNotAllowed.cshtml");

        if (fileInfo.Exists)
        {
            using var stream = fileInfo.CreateReadStream();
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();
            await context.Response.WriteAsync(html);
        }
        else
        {
            await context.Response.WriteAsync("<h1>HTTPS requerido</h1><p>Esta página solo funciona con HTTPS.</p>");
        }

        return;
    }

    await next();
});
app.UseImageProcessing();
app.UseCors();

app.UseRouting();
app.UseAuthentication(); // Identifica al usuario
app.UseSession();
app.UseJwtAuthStrategy(); // pone en uso el middleware de autenticacion
app.UseAuthorization(); // Evalúa políticas y roles

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();