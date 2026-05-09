using ReactiveUI;
using System.Reactive.Disposables.Fluent;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for BorrozOnPeriodDlg.xaml
    /// </summary>
    public partial class BorrowOnPeriodDlg : ReactiveUserControl<BorrowOnPeriodViewModel>
    {
        public BorrowOnPeriodDlg()
        {
            InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.Bind(ViewModel, x=>x.FromDateTime, x=>x.FromDatePicker.Value).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.ToDateTime, x => x.ToDatePicker.Value).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.InclusivePeriods, x => x.IsInclusiveCheckBox.IsChecked).DisposeWith(disposables);
                this.Bind(ViewModel, x => x.Borrows, x => x.BorrowsGrid.ItemsSource).DisposeWith(disposables);
                
                this.BindCommand(ViewModel, x => x.ExportCsvCommand, x => x.ExportCsvBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.CloseCommand, x => x.CloseBtn).DisposeWith(disposables);

            });
        }
    }
}
