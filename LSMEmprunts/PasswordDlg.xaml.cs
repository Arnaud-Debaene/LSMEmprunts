using ReactiveUI;
using ReactiveMarbles.ObservableEvents;
using System;
using System.Reactive.Disposables.Fluent;
using System.Windows.Controls;

namespace LSMEmprunts
{
    /// <summary>
    /// Interaction logic for PasswordDlg.xaml
    /// </summary>
    public partial class PasswordDlg : ReactiveUserControl<PasswordDlgViewModel>
    {
        public PasswordDlg()
        {
            InitializeComponent();
            this.WhenActivated(disposables =>
            {
                this.BindCommand(ViewModel, x => x.OkCommand, x => x.OkBtn).DisposeWith(disposables);
                this.BindCommand(ViewModel, x => x.CancelCommand, x => x.CancelBtn).DisposeWith(disposables);
                
                this.PasswordBox.Events().PasswordChanged.Subscribe(_ => ViewModel.Password = PasswordBox.Password).DisposeWith(disposables);

                PasswordBox.Focus();

            });
        }
    }
}
