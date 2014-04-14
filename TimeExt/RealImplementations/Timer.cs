﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.RealImplementations
{
    public sealed class Timer : ITimer
    {
        public event EventHandler Tick;

        readonly System.Threading.Timer timer;

        internal Timer(TimeSpan interval) 
        {
            this.timer =
                new System.Threading.Timer(_ =>
                    EventHelper.Raise(Tick, this, EventArgs.Empty), null, interval, interval);
        }

        public void Dispose()
        {
            this.timer.Dispose();
        }
    }
}