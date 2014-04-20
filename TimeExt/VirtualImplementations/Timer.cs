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
                using (var changedNowCtx = this.timeline.CreateNewExecutionContext(this.timeline.UtcNow))
                {
                    this.isCalledWaitForTime = true;
                    this.timeline.ExecuteScheduleIfNeed(new ScheduledExecution(this, this.timeline.UtcNow));
                }

                this.timeline.ClearRemainedTicks();
            }
        }

        // タイムラインの現在時刻が変更された場合に呼び出されるメソッド。
        // この中で必要に応じてTickイベントを発火する。
        private void OnChangedNow(object sender, ChangedNowEventArgs e)
        {
            var oldRemainedTicks = this.timeline.GetCurrentRemainedTicks(this); // remain
            var totalTicksCount = (e.Delta.Ticks + oldRemainedTicks) / this.interval.Ticks;
            var remainedTicks = (e.Delta.Ticks + oldRemainedTicks) % this.interval.Ticks;
            if (this.timeline.UtcNow != this.context.UtcNow && remainedTicks == 0) ++totalTicksCount;

            using (var changedNowCtx = this.timeline.CreateNewExecutionContext(this.timeline.UtcNow))
            {
                // 最大totalTicsCount回のTickイベントを発火する。
                for (int i = 0; i < totalTicksCount; i++)
                {
                    var origin = this.context.UtcNow + TimeSpan.FromTicks(this.interval.Ticks * (i + 1));
                    using (var newContext = this.timeline.CreateNewExecutionContext(origin))
                    {
                        var result = this.timeline.ExecuteScheduleIfNeed(new ScheduledExecution(this, this.timeline.UtcNow));
                        if (result == false) // すでに同スケジュールが実行されていてすでに残り時間は保存されてる
                            remainedTicks -= this.interval.Ticks;
                    }
                }
            }
            this.timeline.ClearRemainedTicks();
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
    }
}
