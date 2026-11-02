namespace GestorInventario.Application.DTOS.Role;

public class ListRolDto
{
    public List<SelectRolDto> Roles { get; set; }
}
public class SelectRolDto
{
    public int Id { get; set; }
    public string Nombre { get; set; }
}