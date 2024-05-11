namespace GestorInventario.Application.Services
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IConfiguration _configuration;

        public UnitOfWork(IConfiguration configuration)
        {
            _configuration = configuration;
            PaypalService = new PaypalServices(_configuration);
        }
        public IPaypalService PaypalService { get; private set; }
    }
}
