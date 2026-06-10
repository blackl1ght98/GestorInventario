namespace GestorInventario.MetodosExtension
{
    public static class Antiforgery
    {
        public static IServiceCollection ConfigureAntiforgery(this IServiceCollection services)
        {
           services.AddAntiforgery(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            });
            return services;
        }
    }
}
