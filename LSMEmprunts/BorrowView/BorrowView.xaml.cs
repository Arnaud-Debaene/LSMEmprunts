using LSMEmprunts.Behaviors;
using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for BorrowView.xaml
    /// </summary>
    public partial class BorrowView : ReactiveUserControl<BorrowViewModel>
    {
        public BorrowView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                DataContext = ViewModel;

                this.BindCommand(ViewModel, x => x.CancelCommand, x => x.CancelBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.ValidateCommand, x => x.ValidateBtn).DisposeWith(disposables);

                this.Bind(ViewModel, x => x.Users, x=>x.ListUsers.ItemsSource).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.CurrentUser, x => x.ListUsers.SelectedItem).DisposeWith(disposables);
                
                this.Bind(ViewModel, x=>x.SelectedUserText, x=>x.UserNameInput.Text).DisposeWith(disposables);

                this.Bind(ViewModel, x=>x.CurrentUser.Name, x=>x.SelectedUserLbl.Text).DisposeWith(disposables);

                this.Bind(ViewModel, x=>x.Gears, x=>x.ListGears.ItemsSource).DisposeWith(disposables);
                this.Bind(ViewModel, x=>x.UserSelected, x=>x.ListGears.IsEnabled).DisposeWith(disposables);

                this.Bind(ViewModel, x=>x.SelectedGearId, x=>x.GearIdInputTxt.Text).DisposeWith(disposables);
                this.Bind(ViewModel, x=>x.UserSelected, x=> x.GearIdInputTxt.IsEnabled).DisposeWith(disposables);

                this.Bind(ViewModel,  x=>x.Comment, x=>x.CommentTxt.Text).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.UserSelected, x => x.CommentTxt.IsEnabled).DisposeWith(disposables);

                this.Bind(ViewModel, x=>x.BorrowedGears, x=>x.BorrowedGearGrid.ItemsSource).DisposeWith(disposables);

                this.Bind(ViewModel, x=>x.AutoValidateTicker.RemainingTime, x=>x.TickerLbl.Text).DisposeWith(disposables);

                this.OneWayBind(ViewModel, x=>x.UserSelected, x=>x.UsersPart.IsEnabled, (bool x)=>!x).DisposeWith(disposables);

                
                GearIdInputTxt.ConfigureRfidInput(disposables);
                
            });
        }
    }
}
