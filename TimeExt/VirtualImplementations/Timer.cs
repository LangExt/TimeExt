using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Timer : ITimer
    {
        public event EventHandler Tick;
        readonly TimeSpan interval;
        readonly Timeline timeline;

        internal Timer(Timeline timeline, TimeSpan interval)
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

        public void Dispose()
        {
            // for the real world.
        }
    }
}
