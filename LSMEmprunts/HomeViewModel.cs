using LSMEmprunts.Data;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed class HomeViewModel : ReactiveObject
    {       
        public ObservableCollection<Borrowing> ActiveBorrowings { get; }

        public HomeViewModel()
        {
            using (var context = ContextFactory.OpenContext())
            {
                BorrowCommand = ReactiveCommand.Create(BorrowCmd);
                ReturnCommand = ReactiveCommand.Create(ReturnCmd);
                SettingsCommand = ReactiveCommand.Create(SettingsCmd);

                ActiveBorrowings = new ObservableCollection<Borrowing>(
                    context.Borrowings.Include(e=>e.User).Include(e=>e.Gear)
                    .Where(e => e.State == BorrowingState.Open));
            }
        }

        public ICommand BorrowCommand { get; }
        private void BorrowCmd()
        {
            MainWindowViewModel.Instance.CurrentPageViewModel = new BorrowViewModel();
        }

        public ICommand ReturnCommand { get; }
        private void ReturnCmd()
        {
            MainWindowViewModel.Instance.CurrentPageViewModel = new ReturnViewModel();
        }

        public ICommand SettingsCommand { get; }
        private async void SettingsCmd()
        {
            var vm = new PasswordDlgViewModel();
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.Result == ConfigurationManager.AppSettings["AdminPassword"])
            {
                MainWindowViewModel.Instance.CurrentPageViewModel = new SettingsViewModel();
            }
        }

    }
}
