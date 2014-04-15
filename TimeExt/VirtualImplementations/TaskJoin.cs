using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    public sealed class TaskJoin : ITaskJoin
    {
        public void JoinAll(IEnumerable<ITask> tasks)
        {
            throw new NotImplementedException();
        }
    }
}
