using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetTasks = System.Threading.Tasks;

namespace TimeExt.RealImplementations
{
    internal sealed class Task : ITask
    {
        internal readonly DotNetTasks.Task InternalTask;

        internal Task(Action action)
        {
            this.InternalTask = DotNetTasks.Task.Factory.StartNew(action);
        }

        public void Join()
        {
            this.InternalTask.Wait();
        }

        public void Dispose()
        {
            this.InternalTask.Dispose();
        }
    }
}
