using DynamicData.Binding;
using LSMEmprunts.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReactiveUI;
using System;
using System.Configuration;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LSMEmprunts
{
    public sealed class HomeViewModel : ReactiveObject, IAsyncDisposable
    {
        public ObservableCollectionExtended<Borrowing> ActiveBorrowings { get; } = new();

        public HomeViewModel()
        {
            BorrowCommand = ReactiveCommand.Create(BorrowCmd);
            ReturnCommand = ReactiveCommand.Create(ReturnCmd);
            SettingsCommand = ReactiveCommand.Create(SettingsCmd);

            //start to listen for DB notifications
            _NotificationWaitTask = Task.Run(ListenDbNotificationsAsync);

            //fill the ActiveBorrowings collection with current borrowings
            RefreshBorrowings();

        }

        #region listen for DB notifications about borrowings changes

        /// <summary>
        /// the ListenDbNotificationsAsync task.
        /// </summary>
        private readonly Task _NotificationWaitTask;

        /// <summary>
        /// Cancellation token source used to break out of the listen DB notification loop
        /// </summary>
        private readonly CancellationTokenSource _NotificationWaitCts = new();


        /// <summary>
        /// loop to listen for DB notifications
        /// </summary>
        private async Task ListenDbNotificationsAsync()
        {
            using var connection = ContextFactory.OpenConnection();

            //configure handling of Notification events
            var notifications = Observable.FromEventPattern<NotificationEventHandler, NpgsqlNotificationEventArgs>
                (h => connection.Notification += h, h => connection.Notification -= h);
            var subscription = notifications.Select(e => e.EventArgs).ObserveOn(Application.Current.Dispatcher)
                .Subscribe(evt =>
                {
                    if (string.Compare(evt.Channel, nameof(Borrowing), StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        RefreshBorrowings();
                    }
                });

            //the LISTEN command shall be run within a transaction (see postgres documentation)
            using (var transaction = connection.BeginTransaction())
            {
                using var command = connection.CreateCommand();
                command.CommandText = $"LISTEN {nameof(Borrowing)}";
                await command.ExecuteNonQueryAsync();
                transaction.Commit();
            }

            while (true)
            {
                try
                {
                    await connection.WaitAsync(_NotificationWaitCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    subscription.Dispose();
                    await connection.CloseAsync();
                    break;
                }
            }
        }

        #endregion

        public async ValueTask DisposeAsync()
        {
            _NotificationWaitCts.Cancel();
            //wait for the notifications task to have exited
            await _NotificationWaitTask;
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
            using var context = ContextFactory.OpenContext();
            ActiveBorrowings.Load(context.Borrowings.Include(e => e.User).Include(e => e.Gear)
                    .Where(e => e.State == BorrowingState.Open)
                    .OrderBy(e => e.BorrowTime));
        }

    }
}
