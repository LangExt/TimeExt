using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Timer : ITimer
    {
        EventHandler tickHandler;
        public event EventHandler Tick
        {
            add
            {
                if (this.isCalledWaitForTime)
                    throw new InvalidOperationException("Tickへの登録はWaitForTimeメソッドの呼び出し以前に行う必要があります。");

                this.tickHandler += value;
            }
            remove { this.tickHandler -= value; }
        }

        readonly TimeSpan interval;
        readonly Timeline timeline;
        readonly InitialTick initialTick;

        bool isCalledWaitForTime = false;

        internal Timer(Timeline timeline, TimeSpan interval, InitialTick initialTick)
        {
            this.timeline = timeline;
            this.interval = interval;
            this.initialTick = initialTick;

            timeline.ChangingNow += this.OnChangingNow;
        }

        private void OnChangingNow(object sender, ChangingNowEventArgs e)
        {
            var oldRemainedTicks = this.timeline.GetCurrentRemainedTicks(this);
            this.e[Tuple.Create(timeline.UtcNow, this)] = e;
            this.oldRemainedTicks[Tuple.Create(timeline.UtcNow, this)] = oldRemainedTicks;
            timeline.CreateTask(DoTicks);
            var remainedTicks = (e.Delta.Ticks + oldRemainedTicks) % this.interval.Ticks;
            this.timeline.SetCurrentRemainedTicks(this, remainedTicks);
        }

        Dictionary<Tuple<DateTime, Timer>, ChangingNowEventArgs> e = new Dictionary<Tuple<DateTime, Timer>, ChangingNowEventArgs>();
        Dictionary<Tuple<DateTime, Timer>, long> oldRemainedTicks = new Dictionary<Tuple<DateTime, Timer>, long>();

        internal void DoTicks()
        {
            var e = this.e[Tuple.Create(timeline.UtcNow, this)];
            var oldRemainedTicks = this.oldRemainedTicks[Tuple.Create(timeline.UtcNow, this)];


            if (this.initialTick == InitialTick.Enabled && !isCalledWaitForTime)
            {
                this.isCalledWaitForTime = true; // 別のコンテキストから再度initialTickが呼ばれないようにするフラグを立てる
                timeline.CreateTask(RaiseTick);
            }

            var totalTicksCount = (e.Delta.Ticks + oldRemainedTicks) / this.interval.Ticks;

            // 最大totalTicsCount回のTickイベントを発火する。
            for (int i = 0; i < totalTicksCount; i++)
            {
                var tickWait = TimeSpan.FromTicks(this.interval.Ticks);
                timeline.contextStack.Peek().WaitForTime(tickWait);
                timeline.CreateTask(RaiseTick);
            }
        }

        void RaiseTick()
        {
            EventHelper.Raise(this.tickHandler, this, EventArgs.Empty);
        }

        public void Dispose()
        {
            // for the real world.
        }
    }
}
