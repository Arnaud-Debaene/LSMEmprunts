using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace LSMEmprunts
{
    
    /// <summary>
    /// A simple countdown ticker that publishes a tick when the remaining time reaches zero
    /// and exposes the current remaining seconds as a read-only property.
    /// </summary>
    public sealed class CountDownTicker : ReactiveObject
    {
        /// <summary>
        /// An observable sequence that produces a single <see cref="Unit"/> value when the
        /// countdown reaches zero. Subscribe or bind this to trigger actions on countdown completion.
        /// </summary>
        public IObservable<Unit> Tick { get; }

        private readonly int _InitialTime;

        private ObservableAsPropertyHelper<int> _RemainingTime;

        /// <summary>
        /// Gets the current remaining time in seconds. This value is updated each second by the
        /// internal countdown observable.
        /// </summary>
        /// <remarks>INotifyPropertyChanged.PropertyChanged is raised eacht time the value of this property changes</remarks>
        public int RemainingTime => _RemainingTime.Value;

        private IObservable<int> _Countdown;

        /// <summary>
        /// Initializes a new instance of the <see cref="CountDownTicker"/> class with the
        /// specified duration in seconds.
        /// </summary>
        /// <param name="duration">The countdown duration in seconds.</param>
        public CountDownTicker(int duration)
        {
            _InitialTime = duration;

            Reset();

            Tick = _Countdown.Where(x => x == 0).Select(x => Unit.Default);
        }

        /// <summary>
        /// Resets (or starts) the internal countdown to the initial duration and begins
        /// producing remaining-time updates once per second.
        /// </summary>
        public void Reset()
        {
            var ts = TimeSpan.FromSeconds(1);
            _Countdown = Observable.Timer(TimeSpan.Zero, ts, DispatcherScheduler.Current)
                .Select(currentSeconds => (int)(_InitialTime - currentSeconds))
                .TakeWhile(x => x >= 0);

            _RemainingTime = _Countdown.ToProperty(this, x => x.RemainingTime, _InitialTime);
        }

    }
}
