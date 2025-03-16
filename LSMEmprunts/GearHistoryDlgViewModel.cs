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
    internal sealed class GearHistoryDlgViewModel : ModalDialogViewModelBase
    {
        private readonly TaskCompletionSource<bool> _ResultTask = new TaskCompletionSource<bool>();
        public Task<bool> HasModifiedData => _ResultTask.Task;

        private readonly Context _Context;
        private readonly Gear _Gear;

        public ObservableCollection<Borrowing> Borrowings { get; }

        public string Title { get; }

        public GearHistoryDlgViewModel(Gear gear, Context context)
        {
            ClearHistoryCommand = ReactiveCommand.Create(ClearHistory);

            _Context = context;
            _Gear = gear;

            var converter = new GearTypeToStringConverter();

            Title = "Historique d'emprunt " + converter.Convert(gear.Type, typeof(string), null, null) + " " + gear.Name;

            Borrowings = new ObservableCollection<Borrowing>(context.Borrowings.Include(e => e.User)
                .Where(e => e.Gear == gear).OrderByDescending(e => e.BorrowTime));
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