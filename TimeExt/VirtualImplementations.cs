using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TimeExt
{
    internal sealed class VirtualStopwatch : IStopwatch
    {
        readonly Func<TimeSpan> elapsed;

        internal VirtualStopwatch(Func<TimeSpan> elapsed)
        {
            this.elapsed = elapsed;
        }

        public TimeSpan Elapsed
        {
            get { return elapsed(); }
        }
    }

    internal sealed class VirtualTimer : ITimer
    {
        public event EventHandler Tick;
        readonly TimeSpan interval;
        readonly VirtualTimeline timeline;

        internal VirtualTimer(VirtualTimeline timeline, TimeSpan interval)
        {
            this.timeline = timeline;
            this.interval = interval;

            timeline.ChangedNow += this.OnChangedNow;
        }

        // タイムラインの現在時刻が変更された場合に呼び出されるメソッド。
        // この中で必要に応じてTickイベントを発火する。
        private void OnChangedNow(object sender, ChangedNowEventArgs e)
        {
            var totalTicksCount = (e.Delta.Ticks + this.timeline.CurrentRemainedTicks) / this.interval.Ticks;
            this.timeline.CurrentRemainedTicks = (e.Delta.Ticks + this.timeline.CurrentRemainedTicks) % this.interval.Ticks;
            // 最大totalTicsCount回のTickイベントを発火する。
            for (int i = 0; i < totalTicksCount; i++)
            {
                using (var scope = this.timeline.CreateNewTimeline(interval, i + 1))
                {
                    EventHelper.Raise(this.Tick, this, EventArgs.Empty);
                    i += scope.TicksCount;      // 子のタイムラインでTickイベントを発火していたら、その分は発火しないようにする。
                                                // これをしないと、イベントを重複して発火させてしまう。
                }
            }
        }
    }

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
        internal DateTime Now
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

    internal sealed class ChangedNowEventArgs : EventArgs
    {
        internal readonly TimeSpan Delta;

        internal ChangedNowEventArgs(TimeSpan delta)
        {
            this.Delta = delta;
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
    public sealed class VirtualTimeline : ITimeline
    {
        internal event EventHandler<ChangedNowEventArgs> ChangedNow;

        readonly Stack<RelativeTimeline> timelines = new Stack<RelativeTimeline>();

        /// <summary>
        /// 基準時刻を指定してインスタンスを生成します。
        /// </summary>
        /// <param name="origin">基準時刻</param>
        public VirtualTimeline(DateTime origin)
        {
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
            // 現在時刻を指定時間分進めます。
            // その過程で、タイマーと連動(ChangedNowにタイマーのOnChangedNowが登録される)して、
            // 指定周期が満たされた分だけタイマーのTickイベントを発火します。
            timelines.Peek().WaitForTime(span);
            EventHelper.Raise(this.ChangedNow, this, new ChangedNowEventArgs(span));
        }

        public DateTime Now
        {
            get { return timelines.Peek().Now; }
        }

        public ITimer CreateTimer(TimeSpan interval)
        {
            return new VirtualTimer(this, interval);
        }

        public IStopwatch CreateStopwatch()
        {
            var start = Now;
            return new VirtualStopwatch(() => Now - start);
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
