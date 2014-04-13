using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeExt
{
    public interface IStopwatch
    {
        TimeSpan Elapsed { get; }
    }

    public interface ITimer : IDisposable
    {
        event EventHandler Tick;
    }

    public interface ITimeline
    {
        DateTime Now { get; }

        void WaitForTime(TimeSpan timeSpan);

        IStopwatch CreateStopwatch();

        ITimer CreateTimer(TimeSpan interval);
    }
}
