using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace LSMEmprunts
{
    public sealed class CountDownTicker : ReactiveObject
    {
        public IObservable<Unit> Tick { get; }

        private readonly int _InitialTime;

        private ObservableAsPropertyHelper<int> _RemainingTime;
        public int RemainingTime => _RemainingTime.Value;

        private IObservable<int> _Countdown;


        public CountDownTicker(int duration)
        {
            _InitialTime = duration;

            Reset();

            
            Tick = _Countdown.Where(x => x == 0).Select(x => Unit.Default);
        }

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
