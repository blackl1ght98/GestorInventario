namespace GestorInventario.Infrastructure.Notifications.Email.EmailViewmodel
{
    public class ResetPasswordEmailViewmodel
    {
        public string? RecoveryLink { get; set; }
        public string? TemporaryPassword { get; set; }
    }
}
