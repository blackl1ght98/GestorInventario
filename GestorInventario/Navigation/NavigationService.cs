using GestorInventario.Application.Interfaces.Web;
using GestorInventario.ViewModels.Navigation;

namespace GestorInventario.Navigation;

public class NavigationService : INavigationService
{
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public NavigationService(ICurrentUserAccessor currentUserAccessor)
    {
        _currentUserAccessor = currentUserAccessor;
    }

    public bool IsAdmin => _currentUserAccessor.IsAdmin(); 
    public bool IsAuthenticated => _currentUserAccessor.IsAuthenticated();
  

    public List<MenuItemViewModel> ObtenerDashboardItem()
    {
        if (!IsAdmin) return new();

        return new List<MenuItemViewModel>
        {
            new() { Controller = "Home", Action = "DashBoard", Text = "Dashboard", Title = "Página principal", Icon = "bi-house" }
        };
    }

    public List<MenuItemViewModel> ObtenerGestionUsuariosItems()
    {
        // Solo administradores
        if (!IsAdmin) return new();

        return new List<MenuItemViewModel>
        {
            new() { Controller = "Admin", Action = "Index", Text = "Panel de Administración", Title = "Panel de administración" },
            new() { Controller = "Role", Action = "ObtenerRoles", Text = "Roles", Title = "Gestionar roles" },
        };
    }

    public List<MenuItemViewModel> ObtenerInventarioItems()
    {
        // Solo usuarios autenticados (Admin o Usuario)
        if (!IsAuthenticated) return new();

        var items = new List<MenuItemViewModel>
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

    public List<MenuItemViewModel> ObtenerProductosItem()
    {
        // Visible solo para autenticados
        if (!IsAuthenticated) return new();

        return new List<MenuItemViewModel>
        {
            new() { Controller = "Productos", Action = "Index", Text = "Productos", Icon = "bi-box", Title = "Ver productos" }
        };
    }

    public List<MenuItemViewModel> ObtenerTransaccionesItems()
    {
        // Visible solo para autenticados
        if (!IsAuthenticated) return new();

        return new List<MenuItemViewModel>
        {
            new() { Controller = "Rembolso", Action = "FormularioRembolso", Text = "Formulario rembolso", Icon = "bi-cash", Title = "Gestionar reembolsos" },
            new() { Controller = "Carrito", Action = "Index", Text = "Carrito", Icon = "bi-cart3", Title = "Ver carrito" }
        };
    }

    public List<MenuItemViewModel> ObtenerOperacionesPayPalItems()
    {
        if (!IsAuthenticated) return new();

        var items = new List<MenuItemViewModel>
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

    public List<MenuItemViewModel> ObtenerUserMenuItems()
    {
        if (!IsAuthenticated) return new();

        return new List<MenuItemViewModel>
        {
            new() { Controller = "Auth", Action = "Logout", Text = "Cerrar sesión" },
            new() { Controller = "Admin", Action = "Edit", Text = "Actualizar Perfil" },
            new() { Controller = "Auth", Action = "ChangePassword", Text = "Cambiar Contraseña" }
        };
    }
  
    public List<MenuItemViewModel> ObtenerLoginItem()
    {
        if (IsAuthenticated) return new();

        return new List<MenuItemViewModel>
        {
            new() { Controller = "Auth", Action = "Login", Text = "Iniciar sesión", Title = "Iniciar sesión" }
        };
    }
}