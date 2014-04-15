using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    public sealed class Factory : IFactory
    {
        readonly DateTime origin;

        public Factory(DateTime origin)
        {
            this.origin = origin;
        }

        public ITimeline CreateTimeline()
        {
            return new Timeline(origin);
        }

        public ITaskJoin CreateTaskJoin()
        {
            return new TaskJoin();
        }
    }
}
