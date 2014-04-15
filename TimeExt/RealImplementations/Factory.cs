using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.RealImplementations
{
    public sealed class Factory : IFactory
    {
        public ITimeline CreateTimeline()
        {
            return new Timeline();
        }

        public ITaskJoin CreateTaskJoin()
        {
            return new TaskJoin();
        }
    }
}
