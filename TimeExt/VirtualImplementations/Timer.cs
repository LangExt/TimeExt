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
        }

        private void OnChangingNow(object sender, EventArgs e)
        {
            if (this.initialTick == InitialTick.Enabled && this.timeline.GetCurrentIsCalledInitialTick(this, this.context) == false)
            {
                using (var changedNowCtx = this.timeline.CreateNewExecutionContext(this.timeline.UtcNow))
                {
                    this.timeline.SetCurrentIsCalledInitialTick(this, this.context, true);
                    this.timeline.ExecuteScheduleIfNeed(new ScheduledExecution(this, this.timeline.UtcNow));
                }
            }
        }

        private void OnChangingNow2(object sender, ChangingNow2EventArgs e)
        {
            var oldRemainedTicks = this.timeline.GetCurrentRemainedTicks(this);
            var totalTicksCount = (e.Delta.Ticks + oldRemainedTicks) / this.interval.Ticks;
            var remainedTicks = (e.Delta.Ticks + oldRemainedTicks) % this.interval.Ticks;

            // 最大totalTicsCount回のTickイベントを発火する。
            for (int i = 0; i < totalTicksCount; i++)
            {
                var origin = this.timeline.UtcNow + TimeSpan.FromTicks(this.interval.Ticks * (i + 1)) - TimeSpan.FromTicks(oldRemainedTicks);
                using (var newContext = this.timeline.CreateNewExecutionContext(origin))
                {
                    this.timeline.ExecuteScheduleIfNeed(new ScheduledExecution(this, this.timeline.UtcNow));
                }
            }
            this.timeline.SetCurrentRemainedTicks(this, remainedTicks);
        }

        public void Dispose()
        {
            // for the real world.
        }

        public void Execute()
        {
            EventHelper.Raise(this.tickHandler, this, EventArgs.Empty);
        }


        public void Start()
        {
            timeline.ChangingNow += this.OnChangingNow;
            timeline.ChangingNow2 += this.OnChangingNow2;
        }
    }
}
