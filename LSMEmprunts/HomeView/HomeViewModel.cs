using DynamicData.Binding;
using LSMEmprunts.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LSMEmprunts
{
    public sealed class HomeViewModel : ReactiveObject, IAsyncDisposable, IRoutableViewModel
    {
        public string UrlPathSegment => "home";

        public IScreen HostScreen { get; }

        public ObservableCollectionExtended<Borrowing> ActiveBorrowings { get; } = new();

        public HomeViewModel(IScreen screen = null)
        {
            HostScreen = screen ?? AppLocator.Current.GetService<IScreen>();

            BorrowCommand = ReactiveCommand.CreateFromObservable(()=> HostScreen.Router.Navigate.Execute(new BorrowViewModel(HostScreen)));
            ReturnCommand = ReactiveCommand.CreateFromObservable(() => HostScreen.Router.Navigate.Execute(new ReturnViewModel(HostScreen)));
            SettingsCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialogVm = new PasswordDlgViewModel();
                var result = await ShowPasswordDlg.Handle(dialogVm);
                if (result == ConfigurationManager.AppSettings["AdminPassword"])
                {
                    await HostScreen.Router.Navigate.Execute(new SettingsViewModel(HostScreen));
                }
            });


            //start to listen for DB notifications
            _NotificationWaitTask = Task.Run(ListenDbNotificationsAsync);
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
        /// loop to listen for DB notifications and update ActiveBorrowings
        /// </summary>
        private async Task ListenDbNotificationsAsync()
        {
            using var context = ContextFactory.OpenContext();
            await context.Database.OpenConnectionAsync();
            var connection = context.Database.GetDbConnection() as NpgsqlConnection;

            //register for notifications with the postgres DB
            await context.Database.ExecuteSqlRawAsync($"LISTEN {nameof(Borrowing)}");

            IEnumerable<Borrowing> ReadBorrowings() => context.Borrowings.Include(e => e.User).Include(e => e.Gear)
                    .Where(e => e.State == BorrowingState.Open)
                    .OrderBy(e => e.BorrowTime)
                    .ToList();

            // create an observable of IEnumerable<Borrowing>
            Subject<IEnumerable<Borrowing>> observableBorrowings = new();
            //observe this obsevable to refresh ActiveBorrowings
            using var subscription = observableBorrowings.ObserveOn(Application.Current.Dispatcher)
                .Subscribe(borrowings => ActiveBorrowings.Load(borrowings));

            while (true)
            {
                try
                {
                    observableBorrowings.OnNext(ReadBorrowings());  //read the current content of Borrowing tables and push it on the observable
                    await connection.WaitAsync(_NotificationWaitCts.Token); //wait for notification of postgres                    
                }
                catch (OperationCanceledException)
                {
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

        public ReactiveCommand<Unit, IRoutableViewModel> BorrowCommand { get; }       
        public ReactiveCommand<Unit, IRoutableViewModel> ReturnCommand { get; }
        public ReactiveCommand<Unit, Unit> SettingsCommand { get; }

        public Interaction<PasswordDlgViewModel, string> ShowPasswordDlg { get; } = new();

    }
}
