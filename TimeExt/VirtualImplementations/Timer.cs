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
            timeline.CreateTask(() =>
            {
                var totalTicksCount = (e.Delta.Ticks + oldRemainedTicks) / this.interval.Ticks 
                    + (this.initialTick == InitialTick.Enabled && !isCalledWaitForTime ? 1 : 0);

                // 最大totalTicsCount回のTickイベントを発火する。
                for (int i = 0; i < totalTicksCount; i++)
                {   
                    if (i == 0 && this.initialTick == InitialTick.Enabled && this.isCalledWaitForTime == false)
                    {
                        this.isCalledWaitForTime = true;
                        EventHelper.Raise(this.tickHandler, this, EventArgs.Empty);
                        continue;
                    }
                    else if(this.isCalledWaitForTime)
                    { 
			timeline.contextStack.Peek().WaitForTime(TimeSpan.FromTicks(this.interval.Ticks * (i + 1)));
                        EventHelper.Raise(this.tickHandler, this, EventArgs.Empty);
                    }
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
