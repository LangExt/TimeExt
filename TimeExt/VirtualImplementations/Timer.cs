using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Timer : ITimer, IExecution
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
        readonly ExecutionContext context;

        bool isCalledWaitForTime = false;

        internal Timer(Timeline timeline, ExecutionContext context, TimeSpan interval, InitialTick initialTick)
        {
            this.timeline = timeline;
            this.interval = interval;
            this.initialTick = initialTick;
            this.context = context;

            timeline.ChangingNow += this.OnChangingNow;
            timeline.ChangedNow += this.OnChangedNow;
        }

        private void OnChangingNow(object sender, EventArgs e)
        {
            if (this.initialTick == InitialTick.Enabled && this.isCalledWaitForTime == false)
            {
                this.isCalledWaitForTime = true;
                this.timeline.Schedule(new ScheduledExecution(this, this.timeline.UtcNow));
            }
        }

        // タイムラインの現在時刻が変更された場合に呼び出されるメソッド。
        // この中で必要に応じてTickイベントを発火する。
        private void OnChangedNow(object sender, ChangedNowEventArgs e)
        {
            var oldRemainedTicks = 0;// this.timeline.GetCurrentRemainedTicks(this, context);

            var totalTicksCount = (e.Delta.Ticks + oldRemainedTicks) / this.interval.Ticks;
            var remainedTicks = (e.Delta.Ticks + oldRemainedTicks) % this.interval.Ticks;
            this.timeline.SetCurrentRemainedTicks(this, context, remainedTicks);
            // 最大totalTicsCount回のTickイベントを発火する。
            for (int i = 0; i < totalTicksCount; i++)
            {
                var now = this.context.UtcNow + (TimeSpan.FromTicks(this.interval.Ticks * (i + 1) - oldRemainedTicks));
                this.timeline.Schedule(new ScheduledExecution(this, now));
            }
        }

        public void Dispose()
        {
            // for the real world.
        }

        public void Execute()
        {
            EventHelper.Raise(this.tickHandler, this, EventArgs.Empty);
        }
    }
}
