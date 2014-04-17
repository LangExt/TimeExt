using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Task : ITask 
    {
        readonly Timeline timeline;
        readonly Action action;

        internal Task(Timeline timeline, Action action)
        {
            this.timeline = timeline;
            this.action = action;
        }

        DateTime end;

        internal void Execute()
        {
            action();
            this.end = this.timeline.UtcNow;
        }

        public void Dispose()
        {
            // for the real world.
        }

        public void Join()
        {
            timeline.SetContextIfNeed(this.end);
        }
    }
}
