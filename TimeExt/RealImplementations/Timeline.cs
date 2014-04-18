using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TimeExt.RealImplementations
{
    public sealed class Timeline : ITimeline
    {
        public DateTime UtcNow
        {
            get { return DateTime.UtcNow; }
        }

        public void WaitForTime(TimeSpan timeSpan)
        {
            Thread.Sleep(timeSpan);
        }

        public IStopwatch CreateStopwatch()
        {
            return new Stopwatch();
        }

        public ITask CreateTask(Action action)
        {
            return new Task(action);
        }

        public ITimer CreateTimer(TimeSpan interval, InitialTick initialTick = InitialTick.Disabled)
        {
            return new Timer(interval, initialTick);
        }
    }
}
