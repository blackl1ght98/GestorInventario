namespace GestorInventario.Application.DTOS.Auth;

public record SetNewPasswordDto
{
    public int UserId { get; init; }
    public required string Token { get; init; }
    public required string TemporaryPassword { get; init; }
    public required string Password { get; init; }
}