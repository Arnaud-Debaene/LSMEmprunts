using LSMEmprunts.Behaviors;
using LSMEmprunts.Dialogs;
using ReactiveUI;
using Splat;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for ReturnView.xaml
    /// </summary>
    public partial class ReturnView : ReactiveUserControl<ReturnViewModel>
    {
        public ReturnView()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                DataContext = ViewModel;

                this.BindCommand(ViewModel, x => x.CancelCommand, x => x.CancelBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.ValidateCommand, x => x.ValidateBtn).DisposeWith(disposables);

                this.Bind(ViewModel, x => x.Gears, x => x.GearsList.ItemsSource).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.ClosingBorrowings, x => x.ClosingBorrowingsList.ItemsSource).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.SelectedGearId, x => x.SelectedGearIdTextBox.Text).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.AutoValidateTicker.RemainingTime, x => x.RemainingTimeTextBlock.Text).DisposeWith(disposables);

                SelectedGearIdTextBox.ConfigureRfidInput(disposables);
            });
        }
    }
}
