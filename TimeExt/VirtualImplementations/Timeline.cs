using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class RelativeTimeline
    {
        internal readonly DateTime Origin;

        int ticksCount;
        TimeSpan passed;

        internal long RemainedTicks { get; set; }

        internal RelativeTimeline(DateTime origin)
        {
            this.Origin = origin;
        }

        internal void WaitForTime(TimeSpan span)
        {
            this.passed += span;
        }

        // 基準時刻(Origin)に待った時間の合計を足せば、その時点での現在時刻になる
        internal DateTime UtcNow
        {
            get { return this.Origin + this.passed; }
        }

        /// <summary>
        /// 発火させるべきTickイベントの回数を1増加させます。
        /// </summary>
        internal void IncrementTicksCount()
        {
            this.ticksCount++;
        }

        internal int TicksCount { get { return this.ticksCount; } }
    }

    internal sealed class ChangingNowEventArgs : EventArgs
    {
        internal int TicksCount = 0;
    }

    internal sealed class ChangedNowEventArgs : EventArgs
    {
        internal readonly TimeSpan Delta;
        internal readonly int TicksCount;

        internal ChangedNowEventArgs(TimeSpan delta, int ticksCount)
        {
            this.Delta = delta;
            this.TicksCount = ticksCount;
        }
    }

    internal sealed class RelativeTimelineScope : IDisposable
    {
        readonly Stack<RelativeTimeline> timelines;

        internal RelativeTimelineScope(Stack<RelativeTimeline> timelines, TimeSpan interval, int tickTimes)
        {
            this.timelines = timelines;

            // このクラスはTickを呼ぶたびにインスタンス化されるので、Tickの回数をインクリメントする必要がある。
            timelines.Peek().IncrementTicksCount();
            var newTimeline = new RelativeTimeline(timelines.Peek().Origin + (TimeSpan.FromTicks(interval.Ticks * tickTimes)));
            this.timelines.Push(newTimeline);
        }

        public void Dispose()
        {
            this.timelines.Pop();
        }

        internal int TicksCount
        {
            get { return this.timelines.Peek().TicksCount; }
        }
    }

    /// <summary>
    /// 時間に依存するロジックをテストするために使用するクラスです。
    /// このクラスは、タイマーによる非同期コードを直列化することで、
    /// 実時間を消費する必要なしに時間に依存するロジックに対するテストを実現します。
    /// 通常、テストケースごとにこのクラスのインスタンスを生成し、
    /// アプリケーションで使用するITimelineをそのインスタンスに変更することで、
    /// 時間に依存するロジックをテスト可能にします。
    /// </summary>
    public sealed class Timeline : ITimeline
    {
        internal event EventHandler<ChangingNowEventArgs> ChangingNow;
        internal event EventHandler<ChangedNowEventArgs> ChangedNow;

        readonly Stack<RelativeTimeline> timelines = new Stack<RelativeTimeline>();

        /// <summary>
        /// 基準時刻を指定してインスタンスを生成します。
        /// 基準時刻のKindには、DateTimeKind.Utcが設定されている必要があります。
        /// そのため、DateTime.UtcNowを使うか、ToUniversalTimeメソッドを呼び出すなどして、
        /// UTCに変換したうえで使用してください。
        /// </summary>
        /// <param name="origin">基準時刻</param>
        public Timeline(DateTime origin)
        {
            if (origin.Kind != DateTimeKind.Utc)
                throw new ArgumentException("基準となる時刻にはUTCを指定する必要があります。", "origin");
            timelines.Push(new RelativeTimeline(origin));
        }

        internal RelativeTimelineScope CreateNewTimeline(TimeSpan interval, int tickTimes)
        {
            return new RelativeTimelineScope(this.timelines, interval, tickTimes);
        }

        internal long CurrentRemainedTicks
        {
            get { return this.timelines.Peek().RemainedTicks; }
            set { this.timelines.Peek().RemainedTicks = value; }
        }

        public void WaitForTime(TimeSpan span)
        {
            var changingNowEventArgs = new ChangingNowEventArgs();
            EventHelper.Raise(this.ChangingNow, this, changingNowEventArgs);
            // 現在時刻を指定時間分進めます。
            // その過程で、タイマーと連動(ChangedNowにタイマーのOnChangedNowが登録される)して、
            // 指定周期が満たされた分だけタイマーのTickイベントを発火します。
            timelines.Peek().WaitForTime(span);
            EventHelper.Raise(this.ChangedNow, this, new ChangedNowEventArgs(span, changingNowEventArgs.TicksCount));
        }

        public DateTime UtcNow
        {
            get { return timelines.Peek().UtcNow; }
        }

        public ITimer CreateTimer(TimeSpan interval, InitialTick initialTick = InitialTick.Disabled)
        {
            return new Timer(this, interval, initialTick);
        }

        public IStopwatch CreateStopwatch()
        {
            var start = UtcNow;
            return new Stopwatch(() => UtcNow - start);
        }

        public Action CreateWaiter(params TimeSpan[] timeSpans)
        {
            int i = 0;
            return () =>
            {
                if (timeSpans.Length <= i) throw new InvalidOperationException("予期せぬwait");

                WaitForTime(timeSpans[i++]);
            };
        }

        public Action CreateWaiter(Func<double, TimeSpan> f, params double[] timeSpanValues)
        {
            return this.CreateWaiter(timeSpanValues.Select(f).ToArray());
        }
    }
}
