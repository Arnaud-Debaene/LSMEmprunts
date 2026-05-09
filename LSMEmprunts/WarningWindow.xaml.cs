using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for WarningWindow.xaml
    /// </summary>
    public partial class WarningWindow : ReactiveUserControl<WarningWindowViewModel>
    {
        public WarningWindow()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.Bind(ViewModel, x=> x.Message, x => x.MessageTextBlock.Text).DisposeWith(disposables);
            });
        }
    }
}
