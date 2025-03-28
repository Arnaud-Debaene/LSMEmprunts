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
            var gearsObervableChangeSet = Gears.ToObservableChangeSet(x => x.Id);
            var usersObervableChangeSet = Users.ToObservableChangeSet(x => x.Id);

            var gearsHasErrorsObservable = gearsObervableChangeSet.AutoRefresh(gear => gear.HasErrors).ToCollection().Select(x => x.Any(y => y.HasErrors));
            var usersHasErrorsObservable = usersObervableChangeSet.AutoRefresh(user => user.HasErrors).ToCollection().Select(x => x.Any(y => y.HasErrors));
            var hasErrorsObervable = gearsHasErrorsObservable.Merge(usersHasErrorsObservable);
            _HasErrors = hasErrorsObervable.ToProperty(this, x => x.HasErrors);

            var canValidate = this.WhenAnyValue(x => x.IsDirty, x => x.HasErrors, (dirty, hasError) => {
                return dirty && !hasError;
                });            
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

            _Context = ContextFactory.OpenContext();

            foreach (var user in _Context.Users)
            {
                Users.Add(BuildProxy(user));
            }
            foreach (var gear in _Context.Gears)
            {
                Gears.Add(BuildProxy(gear));
            }

            //UpdateProxiesHistoryStats();
            this.WhenAnyValue(x => x.StatisticsStartDate).Subscribe(x => UpdateProxiesHistoryStats(x));

            //set IsDirty flag whenever gear OR users change
            gearsObervableChangeSet.Subscribe(_ => IsDirty =true);
            usersObervableChangeSet.Subscribe(_ => IsDirty = true);
            IsDirty = false;
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

        private bool _IsDirty = false;
        public bool IsDirty
        {
            get => _IsDirty;
            private set => this.RaiseAndSetIfChanged(ref _IsDirty, value);
        }

        private readonly ObservableAsPropertyHelper<bool> _HasErrors;
        public bool HasErrors => _HasErrors.Value;

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
            IsDirty = true;
        }

        public ReactiveCommand<UserProxy, Unit> UserHistoryCommand { get; }

        private async void ShowUserHistory(UserProxy u)
        {
            var vm = new UserHistoryDlgViewModel(u.WrappedElt, _Context);
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.HasModifiedData)
            {
                IsDirty = true;
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
            IsDirty = true;
        }

        public ReactiveCommand<GearProxy, Unit> GearHistoryCommand { get; }

        private async void ShowGearHistory(GearProxy g)
        {
            var vm = new GearHistoryDlgViewModel(g.WrappedElt, _Context);
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.HasModifiedData)
            {
                IsDirty = true;
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