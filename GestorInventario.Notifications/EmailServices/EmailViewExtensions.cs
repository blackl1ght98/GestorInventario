using GestorInventario.enums.Email;

namespace GestorInventario.Notifications.EmailServices
{
    public static  class EmailViewExtensions
    {
        // Tabla única: cualquier cambio de nombre/ruta se hace aquí y solo aquí
        private static readonly Dictionary<EmailView, string> _viewNames = new()
        {
            [EmailView.RegisterConfirmation] = "ViewsEmailService/ViewRegisterEmail",
            [EmailView.PasswordReset] = "ViewsEmailService/ViewResetPasswordEmail",
            [EmailView.LowStock] = "ViewsEmailService/ViewLowStock",
            [EmailView.Invoice] = "ViewsEmailService/ViewDownloadFactura",
            [EmailView.RefundRequest] = "ViewsEmailService/ViewRembolso",
            [EmailView.RefundApproved] = "ViewsEmailService/ViewRembolsoAprobado",
            [EmailView.OtpCode] = "ViewsEmailService/ViewOtpCode"
        };

        public static string ToViewName(this EmailView view)
        {
            if (_viewNames.TryGetValue(view, out var name))
                return name;

            throw new ArgumentOutOfRangeException(nameof(view), view,
                $"No hay una vista Razor registrada para {view}.");
        }
    }
}
