using DynamicData;
using DynamicData.Binding;
using LSMEmprunts.Data;
using MvvmDialogs;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed class SettingsViewModel : ReactiveObject, IDisposable
    {
        private readonly Context _Context;

        public SettingsViewModel()
        {
            #region load data
            _Context = ContextFactory.OpenContext();
            Users.AddRange(_Context.Users.AsEnumerable().Select(u => BuildProxy(u)));
            Gears.AddRange(_Context.Gears.AsEnumerable().Select(g => BuildProxy(g)));
            #endregion

            #region setup commands
            var gearsObervableChangeSet = Gears.ToObservableChangeSet(x => x.Id);
            var usersObervableChangeSet = Users.ToObservableChangeSet(x => x.Id);
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

            ValidateCommand = ReactiveCommand.Create(ValidateCmd, canValidate);
            CancelCommand = ReactiveCommand.Create(GoBackToHomeView);
            ShowBorrowOnPeriodCommand = ReactiveCommand.Create(ShowBorrowOnPeriod);

            CreateGearCommand = ReactiveCommand.Create(CreateGear);
            DeleteGearCommand = ReactiveCommand.Create<GearProxy>(DeleteGear);
            GearHistoryCommand = ReactiveCommand.Create<GearProxy>(ShowGearHistory);
            GearsCsvCommand = ReactiveCommand.Create(GearsCsv);

            CreateUserCommand = ReactiveCommand.Create(CreateUser);
            DeleteUserCommand = ReactiveCommand.Create<UserProxy>(DeleteUser);
            UserHistoryCommand = ReactiveCommand.Create<UserProxy>(ShowUserHistory);
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

        public ObservableCollection<UserProxy> Users { get; } = new();
        public ObservableCollection<GearProxy> Gears { get; } = new();

        private DateTime _StatisticsStartDate = new(2020, 1, 1);
        public DateTime StatisticsStartDate
        {
            get => _StatisticsStartDate;
            set => this.RaiseAndSetIfChanged(ref _StatisticsStartDate, value);
        }

        #region commands

        public ICommand ValidateCommand { get; }
        private void ValidateCmd()
        {
            _Context.SaveChanges();
            GoBackToHomeView();
        }

        public ICommand CancelCommand { get; }
        private async void GoBackToHomeView() => await MainWindowViewModel.Instance.SetCurrentPage(new HomeViewModel());

        public ICommand ShowBorrowOnPeriodCommand { get; }
        private void ShowBorrowOnPeriod()
        {
            var vm = new BorrowOnPeriodViewModel(_Context);
            MainWindowViewModel.Instance.Dialogs.Add(vm);
        }

        public ICommand CreateUserCommand { get; }
        private void CreateUser()
        {
            var user = new User();
            _Context.Users.Add(user);
            Users.Add(BuildProxy(user));
        }

        public ReactiveCommand<UserProxy, Unit> DeleteUserCommand { get; }
        private void DeleteUser(UserProxy u)
        {
            _Context.Users.Remove(u.WrappedElt);
            Users.Remove(u);
        }

        public ReactiveCommand<UserProxy, Unit> UserHistoryCommand { get; }
        private async void ShowUserHistory(UserProxy u)
        {
            var vm = new UserHistoryDlgViewModel(u.WrappedElt, _Context);
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.HasModifiedData)
            {
                u.SetDirty();  //the user has logically changed
            }
        }

        public ICommand UsersCsvCommand { get; }
        private async void UsersCsv()
        {
            var vm = new SaveFileDialogViewModel
            {
                Filter = "(*.csv)|*.csv"
            };
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.Completion)
            {
                using var writer = new StreamWriter(vm.FileName, false, Encoding.UTF8);
                writer.WriteLine("Nom;Téléphone;#Emprunts");
                foreach (var user in Users)
                {
                    writer.WriteLine($"{user.Name};{user.Phone};{user.StatsBorrowsCount}");
                }
            }
        }

        public ICommand CreateGearCommand { get; }
        private void CreateGear()
        {
            var gear = new Gear();
            _Context.Gears.Add(gear);
            Gears.Add(BuildProxy(gear));
        }

        public ReactiveCommand<GearProxy, Unit> DeleteGearCommand { get; }
        private void DeleteGear(GearProxy g)
        {
            _Context.Gears.Remove(g.WrappedElt);
            Gears.Remove(g);
        }

        public ReactiveCommand<GearProxy, Unit> GearHistoryCommand { get; }
        private async void ShowGearHistory(GearProxy g)
        {
            var vm = new GearHistoryDlgViewModel(g.WrappedElt, _Context);
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.HasModifiedData)
            {
                g.SetDirty();  //the gear has logically changed
            }
        }

        public ICommand GearsCsvCommand { get; }
        private async void GearsCsv()
        {
            var vm = new SaveFileDialogViewModel
            {
                Filter = "(*.csv)|*.csv"
            };
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.Completion)
            {
                using var writer = new StreamWriter(vm.FileName, false, Encoding.UTF8);
                writer.WriteLine("Type;Nom;Code;Taille;#Emprunts;Durée total emprunts");
                var converter = new GearTypeToStringConverter();
                foreach (var gear in Gears)
                {
                    writer.WriteLine($"{converter.Convert(gear.Type, typeof(string), null, Thread.CurrentThread.CurrentUICulture)};{gear.Name};{gear.BarCode};{gear.Size};{gear.StatsBorrowsCount};{gear.StatsBorrowsDuration}");
                }
            }
        }
        #endregion

        private UserProxy BuildProxy(User u) => new(u, Users);

        private GearProxy BuildProxy(Gear g) => new(g, Gears);

        private void UpdateProxiesHistoryStats(DateTime dt)
        {
            var now = DateTime.Now;

            var borrowingQuery = from borrowing in _Context.Borrowings
                                 where borrowing.BorrowTime >= dt
                                 orderby borrowing.BorrowTime
                                 select borrowing;
            foreach (var gearHistory in borrowingQuery.AsEnumerable().GroupBy(borrowing => borrowing.GearId))
            {
                var gearProxy = Gears.FirstOrDefault(e => e.Id == gearHistory.Key);
                if (gearProxy != null)
                {
                    gearProxy.UpdateStats(gearHistory, now);
                }
            }

            foreach (var userHistory in borrowingQuery.AsEnumerable().GroupBy(e => e.UserId))
            {
                var userProxy = Users.FirstOrDefault(e => e.Id == userHistory.Key);
                if (userProxy != null)
                {
                    userProxy.UpdateStats(userHistory, now);
                }
            }
        }
    }
}