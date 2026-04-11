
using GestorInventario.MetodosExtension.Metodos_program.cs;
using GestorInventario.Middlewares;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Net.Http.Headers;
using QuestPDF.Infrastructure;
using System.Text.Json.Serialization;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

var builder = WebApplication.CreateBuilder(args);


// Agregar variables de entorno a la configuración
builder.Configuration.AddEnvironmentVariables();

//Variables compartidas
bool useRedis = bool.Parse(Environment.GetEnvironmentVariable("USE_REDIS") ?? "false");
string authStrategy = builder.Configuration["AuthMode"] ?? "Symmetric";

//Para que no salte una excepcion en consultas que son recursivas
builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

//Configuracion DB
builder.Services.AddDatabaseContext(builder.Configuration);



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
builder.Logging.AddLog4Net();
builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();

//Servicios especificos y transient
builder.Services.AddTransientServices();
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddHttpClientPayPal();
builder.Services.AddAutoMapper(builder.Configuration);
builder.Services.AddWebOptimizer();
//Para futuro cuando agregue autenticacion por google
//builder.Services.AddAuthentication().AddGoogle(cfg => {
//    cfg.ClientId = "";
//cfg.ClientSecret = "password";

//});
// Si estamos usando Redis
if (useRedis)
{
   builder.Services.AddRedisCache(builder.Configuration);
}

builder.Services.AddTokenServices(useRedis);
builder.Services.ConfigureAuthentication(builder.Configuration, authStrategy);
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // permite HTTP y HTTPS no confiable
    options.Cookie.SameSite = SameSiteMode.None; // permite envío cross-site
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
//app.UseHttpMethodOverride(); //middleware para alterar la forma en la que se envian los formularios su uso es asi <input type="hidden" name="_method" value="PUT" />
app.UseRouting();
app.UseAuthentication(); // Identifica al usuario
app.UseSession();
app.UseAuthProcessing( authStrategy); 
app.UseAuthorization(); // Evalúa políticas y roles

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();