using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for GearHistoryDlg.xaml
    /// </summary>
    public partial class GearHistoryDlg : ReactiveUserControl<GearHistoryDlgViewModel>
    {
        public GearHistoryDlg()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                DataContext = ViewModel;
                this.BindCommand(ViewModel, x => x.ClearHistoryCommand, x => x.ClearHistoryBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.CloseCommand, x => x.CloseBtn).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.Borrowings, x => x.BorrowingsGrid.ItemsSource).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.Title, x => x.Title.Text).DisposeWith(disposables);
            });
        }
    }
}
