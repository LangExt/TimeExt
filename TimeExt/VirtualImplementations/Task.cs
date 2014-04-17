using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Task : ITask 
    {
        readonly Timeline timeline;
        readonly ExecutionContext currentContext;
        readonly DateTime origin;
        readonly Action action;

        internal Task(Timeline timeline, ExecutionContext currentContext, DateTime origin, Action action)
        {
            this.timeline = timeline;
            this.currentContext = currentContext;
            this.origin = origin;
            this.action = action;
        }

        void OnChangingNow(object sender, EventArgs e)
        {
            if (currentContext.UtcNow <= origin)
                this.timeline.Schedule(new ScheduledTask(this, origin));
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
