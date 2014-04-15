using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    public sealed class Factory
    {
        public ITimeline CreateTimeline(DateTime origin)
        {
            return new Timeline(origin);
        }

        public ITaskJoin CreateTaskJoin()
        {
            return new TaskJoin();
        }
    }
}
