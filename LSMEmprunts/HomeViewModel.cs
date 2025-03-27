using DynamicData.Binding;
using LSMEmprunts.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReactiveUI;
using System;
using System.Configuration;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed class HomeViewModel : ReactiveObject, IAsyncDisposable
    {
        public ObservableCollectionExtended<Borrowing> ActiveBorrowings { get; } = new();

        private readonly Context _Context;

        public HomeViewModel()
        {
            _Context = ContextFactory.OpenContext();            
            BorrowCommand = ReactiveCommand.Create(BorrowCmd);
            ReturnCommand = ReactiveCommand.Create(ReturnCmd);
            SettingsCommand = ReactiveCommand.Create(SettingsCmd);

            //fill the ActiveBorrowings collection with current borrowings
            RefreshBorrowings();

            //register for postgres notifications about Borrowing table changes to refresh ActiveBorrowings 
            var connection = (NpgsqlConnection)_Context.Database.GetDbConnection();
            var notifications = Observable.FromEventPattern<NotificationEventHandler, NpgsqlNotificationEventArgs>
                (h => connection.Notification += h, h => connection.Notification -= h);
            _NotificationObservableSubscription = notifications.Select(e => e.EventArgs).ObserveOn(DispatcherScheduler.Current.Dispatcher)
                .Subscribe(evt =>
                {
                    if (evt.Channel == nameof(Borrowing))
                    {
                        RefreshBorrowings();
                    }
                });

            //start to listen for DB notifications
            _NotificationWaitTask = ListenDbNotificationsAsync(connection);            
        }

        #region listen for DB notifications about borrowings changes

        /// <summary>
        /// the ListenDbNotificationsAsync task. Required because we need to await for it to have finished before closing the DB connection
        /// </summary>
        private readonly Task _NotificationWaitTask;

        /// <summary>
        /// Cancellation token source used to break out of the listen DB notification task
        /// </summary>
        private readonly CancellationTokenSource _NotificationWaitCts = new();
        
        /// <summary>
        /// subscription to the observable of DB notificaitons
        /// </summary>
        private readonly IDisposable _NotificationObservableSubscription;

        /// <summary>
        /// task to listen for DB notifications
        /// </summary>
        private async Task ListenDbNotificationsAsync(NpgsqlConnection connection)
        {            
            await connection.OpenAsync();
            try
            {
                await connection.WaitAsync(_NotificationWaitCts.Token);  
            }
            catch(OperationCanceledException) 
            {
                return;
            }
        }

        #endregion

        public async ValueTask DisposeAsync()
        {
            _NotificationObservableSubscription.Dispose();
            _NotificationWaitCts.Cancel();
            //wait for the notifications task to have exited BEFORE closing the _Context / db connection
            await _NotificationWaitTask;   
            await _Context.DisposeAsync();
        }

        public ICommand BorrowCommand { get; }
        private async void BorrowCmd()
        {
            await MainWindowViewModel.Instance.SetCurrentPage(new BorrowViewModel());
        }

        public ICommand ReturnCommand { get; }
        private async void ReturnCmd()
        {
            await MainWindowViewModel.Instance.SetCurrentPage(new ReturnViewModel());
        }

        public ICommand SettingsCommand { get; }
        private async void SettingsCmd()
        {
            var vm = new PasswordDlgViewModel();
            MainWindowViewModel.Instance.Dialogs.Add(vm);
            if (await vm.Result == ConfigurationManager.AppSettings["AdminPassword"])
            {
                await MainWindowViewModel.Instance.SetCurrentPage(new SettingsViewModel());
            }
        }

        private void RefreshBorrowings()
        {
            ActiveBorrowings.Load(_Context.Borrowings.Include(e => e.User).Include(e => e.Gear)
                    .Where(e => e.State == BorrowingState.Open)
                    .OrderBy(e => e.BorrowTime));
        }

    }
}
