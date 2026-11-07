namespace GestorInventario.ViewModels.Navigation;

public class MenuItemViewModel
{
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Title { get; set; }
}