using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetTasks = System.Threading.Tasks;

namespace TimeExt.RealImplementations
{
    public sealed class TaskJoin : ITaskJoin
    {
        internal TaskJoin() { }

        public void JoinAll(IEnumerable<ITask> tasks)
        {
            var internalTasks = tasks.Cast<Task>().Select(t => t.InternalTask);
            DotNetTasks.Task.WaitAll(internalTasks.ToArray());
        }
    }
}
