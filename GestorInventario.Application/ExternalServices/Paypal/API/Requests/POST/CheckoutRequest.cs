namespace GestorInventario.Application.ExternalServices.Paypal.API.Requests.POST;

public class CheckoutRequest
{
    public required List<ItemModelRequest> Items { get; set; }
    public required decimal TotalAmount { get; set; }      
    public required string ReturnUrl { get; set; }
    public required string CancelUrl { get; set; }
    public required string Currency { get; set; }
    public required string NombreCompleto { get; set; }
    public required string Telefono { get; set; }
    public  int CodigoPostal { get; set; }
    public required string  Ciudad { get; set; }
    public required string Line1 { get; set; }
    public required string Line2 { get; set; }
    public decimal Subtotal {  get; set; }
    public decimal Iva {  get; set; }
}

public class ItemModelRequest
{
    public required string Name { get; set; }
    public   string? Description { get; set; }
    public required decimal Price { get; set; }
    public required string Currency { get; set; }
    public required string Quantity { get; set; }
    public required string Sku { get; set; }
}