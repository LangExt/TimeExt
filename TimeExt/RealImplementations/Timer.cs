using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.RealImplementations
{
    internal sealed class Timer : ITimer
    {
        public event EventHandler Tick;

        System.Threading.Timer timer;
        readonly TimeSpan interval;
        readonly InitialTick initialTick;

        internal Timer(TimeSpan interval, InitialTick initialTick) 
        {
            this.interval = interval;
            this.initialTick = initialTick;
        }

        public void Start()
        {
            if (this.timer != null)
                throw new InvalidOperationException("このタイマーはすでに開始しています。");

            var dueTime = initialTick == InitialTick.Enabled ? TimeSpan.Zero : interval;
            this.timer =
                new System.Threading.Timer(_ =>
                    EventHelper.Raise(Tick, this, EventArgs.Empty), null, dueTime, interval);
        }

        public void Dispose()
        {
            if (this.timer != null)
                this.timer.Dispose();
        }
    }
}
