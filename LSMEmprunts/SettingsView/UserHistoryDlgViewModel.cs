using LSMEmprunts.Data;
using LSMEmprunts.Dialogs;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed class UserHistoryDlgViewModel : ModalDialogViewModelBase<Unit>
    {
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
            HasModifiedData = true;
        }

        public bool HasModifiedData { get; private set; } = false;
    }
}