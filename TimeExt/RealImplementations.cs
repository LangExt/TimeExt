using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace TimeExt
{
    public sealed class RealStopwatch : IStopwatch
    {
        readonly Stopwatch sw = Stopwatch.StartNew();

        internal RealStopwatch() { }

        public TimeSpan Elapsed
        {
            get { return sw.Elapsed; }
        }
    }

    public sealed class RealTimer : ITimer
    {
        public event EventHandler Tick;

        readonly Timer timer;

        internal RealTimer(TimeSpan interval) 
        {
            this.timer = new Timer(_ => EventHelper.Raise(Tick, this, EventArgs.Empty), null, interval, interval);
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }

    public sealed class RealTimeline : ITimeline
    {
        public DateTime Now
        {
            get { return DateTime.UtcNow; }
        }

        public void WaitForTime(TimeSpan timeSpan)
        {
            Thread.Sleep(timeSpan);
        }

        public IStopwatch CreateStopwatch()
        {
            return new RealStopwatch();
        }

        public ITimer CreateTimer(TimeSpan interval)
        {
            return new RealTimer(interval);
        }
    }
}
