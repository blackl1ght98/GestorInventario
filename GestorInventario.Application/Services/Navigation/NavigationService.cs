using GestorInventario.Application.Interfaces.Web;
using GestorInventario.Application.Navigation;

namespace GestorInventario.Application.Services.Navigation;

public class NavigationService : INavigationService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public NavigationService(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    public bool IsAdmin => _currentUserAccessor.IsAdmin(); 
    public bool IsAuthenticated => _currentUserAccessor.IsAuthenticated();
  

    public List<MenuItemDto> ObtenerDashboardItem()
    {
        if (!IsAdmin) return new();

        return new List<MenuItemDto>
        {
            new() { Controller = "Home", Action = "DashBoard", Text = "Dashboard", Title = "Página principal", Icon = "bi-house" }
        };
    }

    public List<MenuItemDto> ObtenerGestionUsuariosItems()
    {
        // Solo administradores
        if (!IsAdmin) return new();

        return new List<MenuItemDto>
        {
            new() { Controller = "Admin", Action = "Index", Text = "Panel de Administración", Title = "Panel de administración" },
            new() { Controller = "Role", Action = "ObtenerRoles", Text = "Roles", Title = "Gestionar roles" },
        };
    }

    public List<MenuItemDto> ObtenerInventarioItems()
    {
        // Solo usuarios autenticados (Admin o Usuario)
        if (!IsAuthenticated) return new();

        var items = new List<MenuItemDto>
        {
            new() { Controller = "Pedidos", Action = "Index", Text = "Pedidos", Icon = "bi-cart", Title = "Ver pedidos" }
        };

        if (IsAdmin)
        {
            items.Insert(0, new() { Controller = "Proveedor", Action = "Index", Text = "Proveedores", Icon = "bi-truck", Title = "Gestionar proveedores" });
            items.Insert(1, new() { Controller = "Rembolso", Action = "Index", Text = "Rembolsos", Icon = "bi-cart", Title = "Ver pedidos" });
        }

        return items;
    }

    public List<MenuItemDto> ObtenerProductosItem()
    {
        // Visible solo para autenticados
        if (!IsAuthenticated) return new();

        return new List<MenuItemDto>
        {
            new() { Controller = "Productos", Action = "Index", Text = "Productos", Icon = "bi-box", Title = "Ver productos" }
        };
    }

    public List<MenuItemDto> ObtenerTransaccionesItems()
    {
        // Visible solo para autenticados
        if (!IsAuthenticated) return new();

        return new List<MenuItemDto>
        {
            new() { Controller = "Rembolso", Action = "FormularioRembolso", Text = "Formulario rembolso", Icon = "bi-cash", Title = "Gestionar reembolsos" },
            new() { Controller = "Carrito", Action = "Index", Text = "Carrito", Icon = "bi-cart3", Title = "Ver carrito" }
        };
    }

    public List<MenuItemDto> ObtenerOperacionesPayPalItems()
    {
        if (!IsAuthenticated) return new();

        var items = new List<MenuItemDto>
        {
            new() { Controller = "PaypalPlan", Action = "MostrarPlanes", Text = "Planes PayPal" },
            new() { Controller = "PaypalSubscription", Action = "ObtenerSuscripcionUsuario", Text = "Suscripción Usuario" }
        };

        if (IsAdmin)
        {
            items.Insert(0, new() { Controller = "PaypalProduct", Action = "MostrarProductos", Text = "Productos PayPal" });
            items.Insert(1, new() { Controller = "PaypalSubscription", Action = "TodasSuscripciones", Text = "Suscriptores" });
        }

        return items;
    }

    public List<MenuItemDto> ObtenerUserMenuItems()
    {
        if (!IsAuthenticated) return new();

        return new List<MenuItemDto>
        {
            new() { Controller = "Auth", Action = "Logout", Text = "Cerrar sesión" },
            new() { Controller = "Admin", Action = "Edit", Text = "Actualizar Perfil" },
            new() { Controller = "Auth", Action = "ChangePassword", Text = "Cambiar Contraseña" }
        };
    }
  
    public List<MenuItemDto> ObtenerLoginItem()
    {
        if (IsAuthenticated) return new();

        return new List<MenuItemDto>
        {
            new() { Controller = "Auth", Action = "Login", Text = "Iniciar sesión", Title = "Iniciar sesión" }
        };
    }
}