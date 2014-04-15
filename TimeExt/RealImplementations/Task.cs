using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetTasks = System.Threading.Tasks;

namespace TimeExt.RealImplementations
{
    public sealed class Task : ITask
    {
        readonly DotNetTasks.Task task;

        internal Task(Action action)
        {
            this.task = DotNetTasks.Task.Factory.StartNew(action);
        }

        public void Join()
        {
            this.task.Wait();
        }

        public void Dispose()
        {
            this.task.Dispose();
        }
    }
}
