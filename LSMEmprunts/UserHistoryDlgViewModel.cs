using LSMEmprunts.Data;
using Microsoft.EntityFrameworkCore;
using MvvmDialogs.ViewModels;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSMEmprunts
{
    internal sealed class UserHistoryDlgViewModel : ModalDialogViewModelBase
    {
        private readonly TaskCompletionSource<bool> _ResultTask = new TaskCompletionSource<bool>();
        public Task<bool> HasModifiedData => _ResultTask.Task;

        private readonly Context _Context;

        public ObservableCollection<Borrowing> Borrowings { get; }

        public string Title { get; }

        public UserHistoryDlgViewModel(User user, Context context)
        {
            ClearHistoryCommand = ReactiveCommand.Create(ClearHistory);

            _Context = context;

            Title = "Matériel emprunté par " + user.Name;

            Borrowings = new ObservableCollection<Borrowing>(context.Borrowings.Include(e => e.Gear)
                .Where(e => e.User == user).OrderByDescending(e => e.BorrowTime));
        }

        public ICommand ClearHistoryCommand { get; }

        private void ClearHistory()
        {
            _Context.Borrowings.RemoveRange(Borrowings);
            Borrowings.Clear();
            _HasModifiedData = true;
        }

        private bool _HasModifiedData = false;

        public override void RequestClose()
        {
            _ResultTask.SetResult(_HasModifiedData);
            base.RequestClose();
        }
    }
}