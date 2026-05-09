using LSMEmprunts.Dialogs;
using ReactiveUI;
using Splat;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView : ReactiveUserControl<HomeViewModel>
    {
        public HomeView()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                DataContext = ViewModel;
                this.BindCommand(ViewModel, x => x.BorrowCommand, x => x.BorrowBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.ReturnCommand, x => x.ReturnBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.SettingsCommand, x => x.SettingsBtn).DisposeWith(disposables);
                this.Bind(ViewModel, x=>x.ActiveBorrowings, x=>x.ActiveBorrowingsList.ItemsSource).DisposeWith(disposables);

                var dialogManager = Locator.Current.GetService<IDialogManager>();
                dialogManager.RegisterInteraction(ViewModel.ShowPasswordDlg);
            });
        }
    }
}
