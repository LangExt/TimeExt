﻿using System;
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
            timeline.CreateTask(() =>
            {
                var shouldInitialTick = this.initialTick == InitialTick.Enabled && !isCalledWaitForTime;
                var totalTicksCount = (e.Delta.Ticks + oldRemainedTicks) / this.interval.Ticks
                    + (shouldInitialTick ? 1 : 0);

                // 最大totalTicsCount回のTickイベントを発火する。
                for (int i = 0; i < totalTicksCount; i++)
                {
                    if (shouldInitialTick)
                    {
                        this.isCalledWaitForTime = true;
                        timeline.CreateTask(() => EventHelper.Raise(this.tickHandler, this, EventArgs.Empty));
                        var firstTickWait = TimeSpan.FromTicks(this.interval.Ticks); // initialTick分は進めておく
                        timeline.contextStack.Peek().WaitForTime(firstTickWait);
                        continue;
                    }

                    timeline.CreateTask(() => EventHelper.Raise(this.tickHandler, this, EventArgs.Empty));
                    var tickWait = TimeSpan.FromTicks(this.interval.Ticks * (i + 1));
                    timeline.contextStack.Peek().WaitForTime(tickWait);
                }
            });
            var remainedTicks = (e.Delta.Ticks + oldRemainedTicks) % this.interval.Ticks;
            this.timeline.SetCurrentRemainedTicks(this, remainedTicks);
        }

        public void Dispose()
        {
            // for the real world.
        }
    }
}
