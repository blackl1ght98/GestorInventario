using System.Security.Claims;
using GestorInventario.ViewModels.Navigation;

namespace GestorInventario.Navigation;

public interface INavigationService
{
    List<MenuItemViewModel> ObtenerGestionUsuariosItems();
    List<MenuItemViewModel> ObtenerInventarioItems();
    List<MenuItemViewModel> ObtenerTransaccionesItems();
    List<MenuItemViewModel> ObtenerOperacionesPayPalItems();
    List<MenuItemViewModel> ObtenerUserMenuItems();
    List<MenuItemViewModel> ObtenerProductosItem();
    List<MenuItemViewModel> ObtenerDashboardItem();
    List<MenuItemViewModel> ObtenerLoginItem();
}