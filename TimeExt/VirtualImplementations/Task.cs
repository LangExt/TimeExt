using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Task : ITask, IExecution
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

            timeline.ChangingNow += OnChangingNow;
        }

        void OnChangingNow(object sender, EventArgs e)
        {
            if (origin <= currentContext.UtcNow)
                this.timeline.Schedule(new ScheduledExecution(this, origin));
        }

        DateTime end;

        public void Execute()
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

        public void Abort()
        {
        }
    }
}
