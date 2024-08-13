using GestorInventario.Domain.Models;
using GestorInventario.Interfaces.Infraestructure;

namespace GestorInventario.Application.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IConfiguration _configuration;
        private readonly GestorInventarioContext _context;
        private readonly ILogger<PaypalServices> _logger;
        public UnitOfWork(IConfiguration configuration, ILogger<PaypalServices> logger, GestorInventarioContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            PaypalService = new PaypalServices(_configuration, _context,_logger);
            
        }
        public IPaypalService PaypalService { get; private set; }
    }
}
