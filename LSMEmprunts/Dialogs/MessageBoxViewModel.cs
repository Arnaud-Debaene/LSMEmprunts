using System.Windows;

namespace LSMEmprunts.Dialogs
{
    public sealed class MessageBoxViewModel
    {
        public string Caption { get; set; } = "";

        public string Message { get; set; } = "";

        public MessageBoxButton Buttons { get; set; } = MessageBoxButton.OK;

        public MessageBoxImage Image { get; set; } = MessageBoxImage.None;

        public MessageBoxViewModel(string message = "", string caption = "")
        {
            Message = message;
            Caption = caption;
        }
    }
}
