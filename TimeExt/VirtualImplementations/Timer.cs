﻿using System;
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

        bool isCalledWaitForTime = false;
        Task tickerTask;

        internal Timer(Timeline timeline, TimeSpan interval, InitialTick initialTick)
        {
            this.timeline = timeline;
            this.interval = interval;
            this.initialTick = initialTick;

            timeline.ChangingNow += this.OnChangingNow;
            tickerTask = ((Task)this.timeline.CreateTask(() => { EventHelper.Raise(this.tickHandler, this, EventArgs.Empty); }, false));
        }

        private void OnChangingNow(object sender, ChangedNowEventArgs e)
        {

            if (this.initialTick == InitialTick.Enabled && this.isCalledWaitForTime == false)
            {
                this.isCalledWaitForTime = true;
                timeline.Schedule(new ScheduledExecution(tickerTask, timeline.UtcNow));
            }

            var oldRemainedTicks = this.timeline.GetCurrentRemainedTicks(this);
            var totalTicksCount = (e.Delta.Ticks + oldRemainedTicks) / this.interval.Ticks;
            var remainedTicks = (e.Delta.Ticks + oldRemainedTicks) % this.interval.Ticks;
            this.timeline.SetCurrentRemainedTicks(this, remainedTicks);

            // 最大totalTicsCount回のTickイベントを発火する。
            for (int i = 0; i < totalTicksCount; i++)
            {
                var now = this.timeline.UtcNow + (TimeSpan.FromTicks(this.interval.Ticks * (i + 1 /* InitialTickd期間分 */)));
                timeline.Schedule(new ScheduledExecution(tickerTask, now));

            }
        }

        // タイムラインの現在時刻が変更された場合に呼び出されるメソッド。
        // この中で必要に応じてTickイベントを発火する。
        private void OnChangedNow(object sender, ChangedNowEventArgs e)
        {
            var oldRemainedTicks = this.timeline.GetCurrentRemainedTicks(this);
            var totalTicksCount = (e.Delta.Ticks + oldRemainedTicks) / this.interval.Ticks;
            var remainedTicks = (e.Delta.Ticks + oldRemainedTicks) % this.interval.Ticks;
            this.timeline.SetCurrentRemainedTicks(this, remainedTicks);
            // 最大totalTicsCount回のTickイベントを発火する。
            for (int i = 0; i < totalTicksCount; i++)
            {
                var now = this.timeline.UtcNow - e.Delta + (TimeSpan.FromTicks(this.interval.Ticks * (i + 1) - oldRemainedTicks));
                this.timeline.CreateTask(() => { EventHelper.Raise(this.tickHandler, this, EventArgs.Empty); }, now, false);
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
