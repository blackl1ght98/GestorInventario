using GestorInventario.Application.Navigation;

namespace GestorInventario.Application.Interfaces.Web;

public interface INavigationService
{
    List<MenuItemDto> ObtenerGestionUsuariosItems();
    List<MenuItemDto> ObtenerInventarioItems();
    List<MenuItemDto> ObtenerTransaccionesItems();
    List<MenuItemDto> ObtenerOperacionesPayPalItems();
    List<MenuItemDto> ObtenerUserMenuItems();
    List<MenuItemDto> ObtenerProductosItem();
    List<MenuItemDto> ObtenerDashboardItem();
    List<MenuItemDto> ObtenerLoginItem();
}