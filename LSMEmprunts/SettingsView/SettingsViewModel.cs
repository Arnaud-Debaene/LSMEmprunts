using DynamicData;
using DynamicData.Binding;
using LSMEmprunts.Data;
using LSMEmprunts.Dialogs;
using Microsoft.EntityFrameworkCore;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;
using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSMEmprunts
{
    /// <summary>
    /// ViewModel that manages the available gears and users in the application, and provide statistics/reports concerning those entities.
    /// </summary>
    /// <remarks>
    /// Provides lists and commands for managing users and gears, handles import/export
    /// of CSV data, coordinates dialogs for history and period selection, and
    /// exposes validation/cancel actions to persist or discard changes in the
    /// underlying <see cref="Context"/> instance.
    /// </remarks>
    public sealed partial class SettingsViewModel : ReactiveObject, IDisposable, IRoutableViewModel
    {
        public string UrlPathSegment => "settings";

        public IScreen HostScreen { get; }

        private readonly Context _Context;

        public SettingsViewModel(IScreen screen)
        {
            HostScreen = screen;

            #region load data
            _Context = ContextFactory.OpenContext();

            Users = new UsersListViewModel(_Context.Users.AsEnumerable());
            Gears = new GearsListViewModel(_Context.Gears.AsEnumerable());
            #endregion

            #region setup commands
            var gearsObervableChangeSet = Gears.Items.ToObservableChangeSet(x => x.Id);
            var usersObervableChangeSet = Users.Items.ToObservableChangeSet(x => x.Id);
            var oneCollectionHasChangedObservable = Observable.Return(false) //initial state of the observable : no changes have occured to the collections
                .Concat(gearsObervableChangeSet.Skip(1).Select(_ => true))   //note that ToObservableChangeSet() emits the initial state of the collection as its 1st changeset, so we need to skip it
                .Concat(usersObervableChangeSet.Skip(1).Select(_ => true));

            var gearsHasErrorsObservable = gearsObervableChangeSet.AutoRefresh(gear => gear.HasErrors).ToCollection().Select(x => x.Any(y => y.HasErrors));
            var usersHasErrorsObservable = usersObervableChangeSet.AutoRefresh(user => user.HasErrors).ToCollection().Select(x => x.Any(y => y.HasErrors));

            var gearsHasIsDirtyObservable = gearsObervableChangeSet.AutoRefresh(gear=>gear.IsDirty).ToCollection().Select(x=>x.Any(y => y.IsDirty));
            var usersHasIsDirtyObservable = usersObervableChangeSet.AutoRefresh(gear => gear.IsDirty).ToCollection().Select(x => x.Any(y => y.IsDirty));

            /*the validate command is active when :
             * - there is no error in eithr users nor gears
             * - either one of the collections (gear/user) has changed, or one gear or user itself has set itself to dirty
             */
            var canValidate = from hasError in gearsHasErrorsObservable.Merge(usersHasErrorsObservable)
                              from isDirty in gearsHasIsDirtyObservable.Merge(usersHasIsDirtyObservable)
                              from oneCollectionHasChanged in oneCollectionHasChangedObservable
                              select (oneCollectionHasChanged || isDirty) && !hasError;

            ValidateCommand = ReactiveCommand.CreateFromTask(async() => { 
                await _Context.SaveChangesAsync();
                await GoBackToHomeViewAsync();
            }, canValidate);
            CancelCommand = ReactiveCommand.CreateFromTask(GoBackToHomeViewAsync);
            ShowBorrowOnPeriodCommand = ReactiveCommand.Create(ShowBorrowOnPeriod);

            CreateGearCommand = ReactiveCommand.Create(CreateGear);
            DeleteGearCommand = ReactiveCommand.CreateFromTask<GearProxy>(DeleteGearAsync);
            GearHistoryCommand = ReactiveCommand.CreateFromTask<GearProxy>(ShowGearHistoryAsync);
            GearsCsvCommand = ReactiveCommand.Create(GearsCsv);

            CreateUserCommand = ReactiveCommand.Create(CreateUser);
            DeleteUserCommand = ReactiveCommand.CreateFromTask<UserProxy>(DeleteUserAsync);
            UserHistoryCommand = ReactiveCommand.CreateFromTask<UserProxy>(ShowUserHistoryAsync);
            UsersCsvCommand = ReactiveCommand.Create(UsersCsv);
            #endregion

            #region handle properties changes
            this.WhenAnyValue(x => x.StatisticsStartDate).Subscribe(x => UpdateProxiesHistoryStats(x));
            #endregion
        }

        public void Dispose()
        {
            _Context.Dispose();
        }

        #region Ok/Cancel management

        public ICommand ValidateCommand { get; }

        public ICommand CancelCommand { get; }

        private async Task GoBackToHomeViewAsync()
        {
            Dispose();
            await HostScreen.Router.NavigateBack.Execute();
        }

        #endregion

        #region Users List management

        public UsersListViewModel Users { get; }

        public ICommand CreateUserCommand { get; }
        private void CreateUser()
        {
            var user = new User();
            _Context.Users.Add(user);
            Users.Add(user);
        }

        public ReactiveCommand<UserProxy, Unit> DeleteUserCommand { get; }
        private async Task DeleteUserAsync(UserProxy u)
        {
            if (await _Context.Borrowings.AnyAsync(x=>x.User==u.WrappedElt))
            {
                var vm = new ConfirmWindowViewModel("Cet utilisateur a un historique d'emprunt. Etes vous sûr de vouloir l'effacer?");
                if (await Locator.Current.GetService<IDialogManager>().ConfirmWindow.Handle(vm) == false)
                {
                    return;
                }
            }
            _Context.Users.Remove(u.WrappedElt);
            Users.Remove(u);
        }
        public ReactiveCommand<UserProxy, Unit> UserHistoryCommand { get; }
        private async Task ShowUserHistoryAsync(UserProxy u)
        {
            var vm = new UserHistoryDlgViewModel(u.WrappedElt, _Context);
            await ShowUserHistoryDialog.Handle(vm);
            // MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (vm.HasModifiedData)
            {
                u.SetDirty();  //the user has logically changed
            }
        }

        public Interaction<UserHistoryDlgViewModel, Unit> ShowUserHistoryDialog { get; } = new();

        public ICommand UsersCsvCommand { get; }
        private async Task UsersCsv()
        {
            var vm = new SaveFileDialogViewModel
            {
                Filter = "(*.csv)|*.csv"
            };
            //MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await Locator.Current.GetService<IDialogManager>().SaveFile.Handle(vm))
            {
                using var writer = new StreamWriter(vm.FileName, false, Encoding.UTF8);
                writer.WriteLine("Nom;Téléphone;#Emprunts");
                foreach (var user in Users.Items)
                {
                    writer.WriteLine($"{user.Name};{user.Phone};{user.StatsBorrowsCount}");
                }
            }
        }

        #endregion        

        #region BorrowOnPeriod dialog management
        public ICommand ShowBorrowOnPeriodCommand { get; }        
        private async Task ShowBorrowOnPeriod()
        {
            var vm = new BorrowOnPeriodViewModel(_Context);
            await ShowBorrowOnPeriodDialog.Handle(vm);
        }

        public Interaction<BorrowOnPeriodViewModel, Unit> ShowBorrowOnPeriodDialog { get; } = new();
        #endregion

        #region Gears List management

        public GearsListViewModel Gears { get; }

        public ICommand CreateGearCommand { get; }
        private void CreateGear()
        {
            var gear = new Gear();
            _Context.Gears.Add(gear);
            Gears.Add(gear);
        }

        public ReactiveCommand<GearProxy, Unit> DeleteGearCommand { get; }
        private async Task DeleteGearAsync(GearProxy g)
        {
            if (await _Context.Borrowings.AnyAsync(x => x.Gear == g.WrappedElt))
            {
                var vm = new ConfirmWindowViewModel("Ce matériel a un historique d'emprunt. Etes vous sûr de vouloir l'effacer?");
                if (await Locator.Current.GetService<IDialogManager>().ConfirmWindow.Handle(vm) == false)
                {
                    return;
                }
            }
            _Context.Gears.Remove(g.WrappedElt);
            Gears.Remove(g);
        }

        public ReactiveCommand<GearProxy, Unit> GearHistoryCommand { get; }
        private async Task ShowGearHistoryAsync(GearProxy g)
        {
            var vm = new GearHistoryDlgViewModel(g.WrappedElt, _Context);
            await ShowGearHistoryDialog.Handle(vm);            
            if (vm.HasModifiedData)
            {
                g.SetDirty();  //the gear has logically changed
            }
        }

        public Interaction<GearHistoryDlgViewModel, Unit> ShowGearHistoryDialog { get; } = new();

        public ICommand GearsCsvCommand { get; }
        private async Task GearsCsv()
        {
            var vm = new SaveFileDialogViewModel
            {
                Filter = "(*.csv)|*.csv"
            };
            if (await Locator.Current.GetService<IDialogManager>().SaveFile.Handle(vm))
            {
                using var writer = new StreamWriter(vm.FileName, false, Encoding.UTF8);
                writer.WriteLine("Type;Nom;Code;Taille;#Emprunts;Durée total emprunts");
                var converter = new GearTypeToStringConverter();
                foreach (var gear in Gears.Items)
                {
                    writer.WriteLine($"{converter.Convert(gear.Type, typeof(string), null, Thread.CurrentThread.CurrentUICulture)};{gear.Name};{gear.BarCode};{gear.Size};{gear.StatsBorrowsCount};{gear.StatsBorrowsDuration}");
                }
            }
        }
        #endregion

        #region historic statistics management

        [Reactive]
        private DateTime _StatisticsStartDate = new(2020, 1, 1);

        private void UpdateProxiesHistoryStats(DateTime dt)
        {
            var now = DateTime.Now;

            var borrowingQuery = from borrowing in _Context.Borrowings
                                 where borrowing.BorrowTime >= dt.ToUniversalTime()
                                 orderby borrowing.BorrowTime
                                 select borrowing;
            foreach (var gearHistory in borrowingQuery.AsEnumerable().GroupBy(borrowing => borrowing.GearId))
            {
                var gearProxy = Gears.Items.FirstOrDefault(e => e.Id == gearHistory.Key);
                if (gearProxy != null)
                {
                    gearProxy.UpdateStats(gearHistory, now);
                }
            }

            foreach (var userHistory in borrowingQuery.AsEnumerable().GroupBy(e => e.UserId))
            {
                var userProxy = Users.Items.FirstOrDefault(e => e.Id == userHistory.Key);
                if (userProxy != null)
                {
                    userProxy.UpdateStats(userHistory, now);
                }
            }
        }
        #endregion
    }
}