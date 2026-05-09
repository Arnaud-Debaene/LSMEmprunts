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
    public sealed class GearHistoryDlgViewModel : ModalDialogViewModelBase<Unit>
    { 
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
            HasModifiedData = true;
        }

        public bool HasModifiedData { get; private set; } = false;

       
    }
}