namespace GestorInventario.Domain.enums.Email
{
    public enum EmailView
    {
        RegisterConfirmation,
        PasswordReset,
        LowStock,
        Invoice,
        RefundRequest,        // ViewRembolso
        RefundApproved,       // ViewRembolsoAprobado
        OtpCode
    }
}
