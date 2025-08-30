namespace GestorInventario.Application.DTOs.User
{
    //NO CAMBIAR-> INFORMACION QUE CONTIENE EL TOKEN QUE SE GENERA
    public class LoginResponseDto
    {
        public int Id { get; set; }

        public string Token { get; set; }
        public string? Rol { get; set; }
       
        public string RefreshToken { get; set; }
    }
}
