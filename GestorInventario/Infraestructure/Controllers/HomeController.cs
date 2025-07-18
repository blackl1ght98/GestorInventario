using Microsoft.AspNetCore.Mvc;


namespace GestorInventario.Infraestructure.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (!User.Identity.IsAuthenticated) 
            {
                // Obtiene las cookies del navegador.
                var cookieCollection = Request.Cookies;

                // Recorre todas las cookies y las elimina.
                foreach (var cookie in cookieCollection)
                {
                    Response.Cookies.Delete(cookie.Key);
                }

            }
            return View();
        }
        public IActionResult Error()
        {
            return View();
        }


    }
}
