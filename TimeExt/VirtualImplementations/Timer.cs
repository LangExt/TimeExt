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
            timeline.ChangedNow += this.OnChangedNow;
        }

        private void OnChangingNow(object sender, ChangingNowEventArgs e)
        {
            if (this.initialTick == InitialTick.Enabled && this.isCalledWaitForTime == false)
            {
                using (var scope = this.timeline.CreateNewTimeline(TimeSpan.Zero, 1))
                {
                    this.isCalledWaitForTime = true;
                    EventHelper.Raise(this.tickHandler, this, EventArgs.Empty);
                    e.TicksCount = scope.TicksCount;
                }
            }
        }

        // タイムラインの現在時刻が変更された場合に呼び出されるメソッド。
        // この中で必要に応じてTickイベントを発火する。
        private void OnChangedNow(object sender, ChangedNowEventArgs e)
        {
            var totalTicksCount = (e.Delta.Ticks + this.timeline.CurrentRemainedTicks) / this.interval.Ticks;
            //this.timeline.CurrentRemainedTicks = (e.Delta.Ticks + this.timeline.CurrentRemainedTicks) % this.interval.Ticks;
            // 最大totalTicsCount回のTickイベントを発火する。
            for (int i = 0; i < totalTicksCount - e.ChangingNowTicksCount; i++)
            {
                var now = this.timeline.UtcNow - e.Delta + (TimeSpan.FromTicks(this.interval.Ticks * (i + 1)));
                this.timeline.RequestTick(new TickRequest(this, now));
            }
        }

        private void DoChangedNow(ChangedNowEventArgs e)
        {
            var totalTicksCount = (e.Delta.Ticks + this.timeline.CurrentRemainedTicks) / this.interval.Ticks;
            this.timeline.CurrentRemainedTicks = (e.Delta.Ticks + this.timeline.CurrentRemainedTicks) % this.interval.Ticks;
            // 最大totalTicsCount回のTickイベントを発火する。
            for (int i = 0; i < totalTicksCount - e.ChangingNowTicksCount; i++)
            {
                using (var scope = this.timeline.CreateNewTimeline(interval, i + 1))
                {
                    EventHelper.Raise(this.tickHandler, this, EventArgs.Empty);
                    i += scope.TicksCount;      // 子のタイムラインでTickイベントを発火していたら、その分は発火しないようにする。
                                                // これをしないと、イベントを重複して発火させてしまう。
                }
            }
        }

        public void Dispose()
        {
            // for the real world.
        }

        internal void FireTick(DateTime now)
        {
            using (var scope = this.timeline.CreateNewTimeline(now))
            {
                EventHelper.Raise(this.tickHandler, this, EventArgs.Empty);
            }
        }
    }
}
