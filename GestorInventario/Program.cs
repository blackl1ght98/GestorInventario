using GestorInventario.Application.Services;
using GestorInventario.Domain.Models;
using GestorInventario.Infraestructure.Repositories;
using GestorInventario.Infrastructure.Repositories;
using GestorInventario.Interfaces;
using GestorInventario.Interfaces.Application;
using GestorInventario.Interfaces.Infraestructure;
using GestorInventario.Interfaces.Infrastructure;
using GestorInventario.MetodosExtension;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;

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
    connectionString = builder.Configuration["CONNECTION_STRING"];
    secret = builder.Configuration["ClaveJWT"];
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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
builder.Services.AddTransient<IChangePassService, ChangePassService>();
builder.Services.AddTransient<IConfirmEmailService, ConfirmEmailService>();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<TokenService>();
builder.Services.AddTransient<IAdminRepository,AdminRepository>();
builder.Services.AddTransient<IAdminCrudOperation, CrudOperation>();
builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();
//------------------------------------------------------
builder.Services.AddHttpContextAccessor();
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

//    // options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//})
//    .AddCookie(options =>
//    {
//        options.Cookie.HttpOnly = true;                    //la cookie solo puede ser accedida por el servidor
//        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);//tiempo de expiracion de la cookie
//        options.LoginPath = "/User/Login";//Donde se reedirige al usuario cuando intenta acceder a algo que no tiene permisos
//        options.AccessDeniedPath = "/Home/AccessDenied";
//        options.SlidingExpiration = true;//mientras exista actividad en la pagina la sesion no cerrara 
//        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;//con esto solo se envia a traves de conexiones https
//        options.Cookie.SameSite = SameSiteMode.Strict;//Esta opci�n previene el env�o de la cookie en solicitudes de sitios cruzados. Esto ayuda a prevenir ataques de falsificaci�n de solicitudes entre sitios (CSRF).
//        options.Cookie.MaxAge = TimeSpan.FromMinutes(60);//Cuanto dura la sesion al usuario
//    });
builder.Services.AddAuthentication(options =>
{
    // Establece el esquema de autenticación predeterminado que se utilizará para autenticar al usuario.
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    // Establece el esquema de desafío predeterminado que se utilizará para desafiar al usuario.
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configura las opciones para la validación del token JWT.
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Activa la validación del emisor del token.
        /*En un token JWT, el emisor (o “issuer”) es quien emite el token*/
        /*ValidateIssuer = true: Esto verifica que el token fue creado por tu aplicación. Es como comprobar la 
         * firma en una carta para asegurarte de que fue escrita por la persona correcta.*/
        /*
            El emisor (Issuer) es quien crea y firma el token, similar a la persona que escribe y envía una carta.
            El público (Audience) es la entidad para la que se destina el token, similar a la persona que recibe la carta.

        Al habilitar ValidateIssuer y ValidateAudience, estás asegurándote de que el token fue emitido por 
        la entidad correcta (JwtIssuer) y está destinado a la aplicación correcta (JwtAudience). Esto es similar a verificar
        el remitente y el destinatario de una carta para asegurarte de que fue enviada por la persona correcta 
        y a la dirección correcta.
         */
        ValidateIssuer = true,
        // Establece el emisor válido. El emisor es quien emite el token.
        ValidIssuer = builder.Configuration["JwtIssuer"],
        // Activa la validación del público del token.
        /*Esto verifica que el token está destinado a ser usado con tu aplicación. Es como comprobar la dirección en un paquete para asegurarte de que fue enviado al lugar correcto.*/
        ValidateAudience = true,
        // Establece el público válido. El público es a quién está destinado el token.
        ValidAudience = builder.Configuration["JwtAudience"],
        // Activa la validación de la vida útil del token.
        ValidateLifetime = true,
        // Activa la validación de la clave de firma del emisor.
        ValidateIssuerSigningKey = true,
        // Establece la clave de firma del emisor. La clave de firma se utiliza para verificar que el emisor
        // del token es quien dice ser y para asegurar que el token no ha sido alterado en tránsito.
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
    };
    // Configura los eventos que se pueden manejar durante el procesamiento del token JWT.
    options.Events = new JwtBearerEvents
    {
        // Maneja el evento de recepción del mensaje(token).
        OnMessageReceived = context =>
        {
            // Establece el token del contexto a partir de la cookie "auth".
            context.Token = context.Request.Cookies["auth"];
            // Completa la tarea.
            return Task.CompletedTask;
        },
    };
});
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
app.Use(async (context, next) =>
{
    try
    {
        var token = context.Request.Cookies["auth"];
        // Si el token existe...
        if (token != null)
        {
            // Crea un nuevo manejador de tokens JWT.
            var handler = new JwtSecurityTokenHandler();
            // Valida el token.
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                /*Esta parte se ejecuta cuando el usuario hace el login:
                ValidateIssuerSigningKey = true: Valida que la firma del token es correcta.
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)): establece el como se tiene que validar el token
                ValidateIssuer = true:  activa la validación del emisor del token para comprobar que el emisor es quien dice ser.
                ValidIssuer = builder.Configuration["JwtIssuer"]: esto hace posible esa comprobacion de emisor
                ValidateAudience = true:  Activa la validación del público del token, esto es para comprobar que el usuario tiene un token valido
                ValidAudience = builder.Configuration["JwtAudience"]:  esto es el valor que se comprueba para ver si el token que tiene el
                usuario es valido
                */
                // Valida la firma del token.
                ValidateIssuerSigningKey = true,
                // Establece la clave que se debe usar para validar la firma del token.
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["JwtIssuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["JwtAudience"],
                //Toda esta configuracion se almacena en la variable validatedToken
            }, out var validatedToken);

            // Establece el usuario del contexto actual a partir de la información del token.
            //Detecta que usuario esta logueado, permitiendo hacer esa verificacion
            context.User = principal;
            token = context.Session.GetString("auth") ?? context.Request.Cookies["auth"];
            context.Session.SetString("auth", token);


        }

        // Pasa el control al siguiente middleware en la cadena.
        await next.Invoke();
    }
    catch (SecurityTokenException ex)
    {

        var logger = log4net.LogManager.GetLogger(typeof(Program));
        logger.Error("Error al validar el token", ex);
    }
    // Obtiene el token de la cookie "auth".
   
});





app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
