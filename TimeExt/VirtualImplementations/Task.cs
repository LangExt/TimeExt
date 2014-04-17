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
        
        public override bool Equals(object obj)
        {
            var other = obj as Task;
            if (other == null)
                return false;
            return this.timeline == other.timeline && action == other.action;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(this.timeline, this.action).GetHashCode();
        }


        public void Abort()
        {
            timeline.Abort();
        }
    }
}
