﻿using System;
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

        public ITimer CreateTimer(TimeSpan interval)
        {
            return new Timer(interval);
        }
    }
}