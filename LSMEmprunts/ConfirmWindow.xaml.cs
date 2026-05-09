using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for ConfirmWindow.xaml
    /// </summary>
    public partial class ConfirmWindow : ReactiveUserControl<ConfirmWindowViewModel>
    {
        public ConfirmWindow()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.BindCommand(ViewModel, x=>x.OkCommand, x=>x.ConfirmBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x=>x.CancelCommand, x=>x.CloseBtn).DisposeWith(disposables);
                this.Bind(ViewModel, x=>x.Message, x=>x.MessageTextBlock.Text).DisposeWith(disposables);
            });
        }
    }
}
