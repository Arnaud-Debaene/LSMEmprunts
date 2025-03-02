using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LSMEmprunts.Data;
using MvvmDialogs;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace LSMEmprunts
{
    public sealed class SettingsViewModel : ObservableObject, IDisposable
    {
        private readonly Context _Context;

        public SettingsViewModel()
        {
            ValidateCommand = new RelayCommand(ValidateCmd, CanValidateCmd);
            CancelCommand = new RelayCommand(GoBackToHomeView);
            ShowBorrowOnPeriodCommand = new RelayCommand(ShowBorrowOnPeriod);

            CreateGearCommand = new RelayCommand(CreateGear);
            DeleteGearCommand = new RelayCommand<GearProxy>(DeleteGear);
            GearHistoryCommand = new RelayCommand<GearProxy>(ShowGearHistory);
            GearsCsvCommand = new RelayCommand(GearsCsv);

            CreateUserCommand = new RelayCommand(CreateUser);
            DeleteUserCommand = new RelayCommand<UserProxy>(DeleteUser);
            UserHistoryCommand = new RelayCommand<UserProxy>(ShowUserHistory);
            UsersCsvCommand = new RelayCommand(UsersCsv);

            _Context = ContextFactory.OpenContext();

            Users = new ObservableCollection<UserProxy>();
            foreach (var user in _Context.Users)
            {
                Users.Add(BuildProxy(user));
            }
            Gears = new ObservableCollection<GearProxy>();
            foreach (var gear in _Context.Gears)
            {
                Gears.Add(BuildProxy(gear));
            }

            UpdateProxiesHistoryStats();
        }

        public void Dispose()
        {
            foreach (var user in Users)
            {
                user.PropertyChanged -= OnProxyPropertyChanged;
                user.ErrorsChanged -= OnProxyErrorsChanged;
            }
            foreach (var gear in Gears)
            {
                gear.PropertyChanged -= OnProxyPropertyChanged;
                gear.ErrorsChanged -= OnProxyErrorsChanged;
            }

            _Context.Dispose();
        }

        public ObservableCollection<UserProxy> Users { get; }
        public ObservableCollection<GearProxy> Gears { get; }

        private DateTime _StatisticsStartDate = new(2020, 1, 1);
        public DateTime StatisticsStartDate
        {
            get => _StatisticsStartDate;
            set
            {
                if (SetProperty(ref _StatisticsStartDate, value))
                {
                    UpdateProxiesHistoryStats();
                }
            }
        }

        public RelayCommand ValidateCommand { get; }

        private void ValidateCmd()
        {
            _Context.SaveChanges();
            GoBackToHomeView();
        }

        private bool CanValidateCmd() => _IsDirty && !HasErrors;

        public RelayCommand CancelCommand { get; }

        private void GoBackToHomeView() => MainWindowViewModel.Instance.CurrentPageViewModel = new HomeViewModel();

        public RelayCommand ShowBorrowOnPeriodCommand { get; }

        private void ShowBorrowOnPeriod()
        {
            var vm = new BorrowOnPeriodViewModel(_Context);
            MainWindowViewModel.Instance.Dialogs.Add(vm);
        }


        private bool _DirtyWatchingSuspended = false;

        private bool _IsDirty = false;

        private void SetDirty()
        {
            if (!_DirtyWatchingSuspended)
            {
                _IsDirty = true;
                ValidateCommand.NotifyCanExecuteChanged();
            }
        }

        public bool HasErrors => Gears.Any(e => e.HasErrors) || Users.Any(e => e.HasErrors);

        public RelayCommand CreateUserCommand { get; }

        private void CreateUser()
        {
            var user = new User();
            _Context.Users.Add(user);
            Users.Add(BuildProxy(user));
        }

        public RelayCommand<UserProxy> DeleteUserCommand { get; }

        private void DeleteUser(UserProxy u)
        {
            _Context.Users.Remove(u.WrappedElt);
            Users.Remove(u);
            SetDirty();
        }

        public RelayCommand<UserProxy> UserHistoryCommand { get; }

        private async void ShowUserHistory(UserProxy u)
        {
            var vm = new UserHistoryDlgViewModel(u.WrappedElt, _Context);
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.HasModifiedData)
            {
                SetDirty();
            }
        }

        public RelayCommand UsersCsvCommand { get; }

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

        public RelayCommand CreateGearCommand { get; }

        private void CreateGear()
        {
            var gear = new Gear();
            _Context.Gears.Add(gear);
            Gears.Add(BuildProxy(gear));
        }

        public RelayCommand<GearProxy> DeleteGearCommand { get; }

        private void DeleteGear(GearProxy g)
        {
            _Context.Gears.Remove(g.WrappedElt);
            Gears.Remove(g);
            SetDirty();
        }

        public RelayCommand<GearProxy> GearHistoryCommand { get; }

        private async void ShowGearHistory(GearProxy g)
        {
            var vm = new GearHistoryDlgViewModel(g.WrappedElt, _Context);
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.HasModifiedData)
            {
                SetDirty();
            }
        }

        public RelayCommand GearsCsvCommand { get; }

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

        private UserProxy BuildProxy(User u)
        {
            var proxy = new UserProxy(u, Users);
            proxy.PropertyChanged += OnProxyPropertyChanged;
            proxy.ErrorsChanged += OnProxyErrorsChanged;
            return proxy;
        }

        private GearProxy BuildProxy(Gear g)
        {
            var proxy = new GearProxy(g, Gears);
            proxy.PropertyChanged += OnProxyPropertyChanged;
            proxy.ErrorsChanged += OnProxyErrorsChanged;
            return proxy;
        }

        private void OnProxyPropertyChanged(object source, PropertyChangedEventArgs args) => SetDirty();

        private void OnProxyErrorsChanged(object source, DataErrorsChangedEventArgs args) => ValidateCommand.NotifyCanExecuteChanged();

        private void UpdateProxiesHistoryStats()
        {
            try
            {
                _DirtyWatchingSuspended = true;

                var now = DateTime.Now;

                var borrowingQuery = from borrowing in _Context.Borrowings
                                     where borrowing.BorrowTime >= StatisticsStartDate
                                     orderby borrowing.BorrowTime
                                     select borrowing;
                foreach(var gearHistory in borrowingQuery.AsEnumerable().GroupBy(borrowing => borrowing.GearId)) 
                {
                    var gearProxy = Gears.FirstOrDefault(e => e.Id == gearHistory.Key);
                    if (gearProxy != null)
                    {
                        gearProxy.UpdateStats(gearHistory, now);
                    }
                }

                foreach(var userHistory in borrowingQuery.AsEnumerable().GroupBy(e=>e.UserId)) 
                {
                    var userProxy = Users.FirstOrDefault(e=>e.Id == userHistory.Key);
                    if (userProxy != null) 
                    {
                        userProxy.UpdateStats(userHistory, now);
                    }
                }
            }
            finally
            {
                _DirtyWatchingSuspended = false;
            }
        }
    }
}