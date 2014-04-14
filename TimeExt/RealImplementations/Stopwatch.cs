using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.RealImplementations
{
    public sealed class Stopwatch : IStopwatch
    {
        readonly System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

        internal Stopwatch() { }

        public TimeSpan Elapsed
        {
            get { return this.sw.Elapsed; }
        }
    }
}
