using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Stopwatch : IStopwatch
    {
        readonly Func<TimeSpan> elapsed;

        internal Stopwatch(Func<TimeSpan> elapsed)
        {
            this.elapsed = elapsed;
        }

        public TimeSpan Elapsed
        {
            get { return elapsed(); }
        }
    }
}
