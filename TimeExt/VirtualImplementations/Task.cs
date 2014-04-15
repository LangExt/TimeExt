using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Task : ITask, IFireable
    {
        readonly Timeline rootTimeline;
        readonly RelativeTimeline timeline;
        readonly DateTime utcNow;
        readonly Action action;

        internal Task(Timeline rootTimeline, RelativeTimeline timeline, DateTime UtcNow, Action action)
        {
            this.rootTimeline = rootTimeline;
            this.timeline = timeline;
            this.utcNow = UtcNow;
            this.action = action;

            rootTimeline.ChangingNow += OnChangingNow;
        }

        void OnChangingNow(object sender, EventArgs e)
        {
            if (utcNow <= timeline.UtcNow)
                this.rootTimeline.RequestFire(new FireRequest(this, utcNow));
        }

        public void Fire(DateTime now)
        {
            using (var scope = rootTimeline.CreateNewTimeline(now))
                action();
        }

        public void Dispose()
        {
            // for the real world.
        }
    }
}
