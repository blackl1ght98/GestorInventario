namespace GestorInventario.Application.Classes
{
    public class ItemModel
    {
        public required string Name { get; set; }
        public   string? Description { get; set; }
        public required decimal Price { get; set; }
        public required string Currency { get; set; }
        public required string Quantity { get; set; }
        public required string Sku { get; set; }
        public string? ImageUrl { get; set; }         
    }
}
