using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DotNetTasks = System.Threading.Tasks;

namespace TimeExt.RealImplementations
{
    [DebuggerDisplay("Task Id = {Id}")]
    internal sealed class Task : ITask
    {
        readonly Guid id = Guid.NewGuid();
        public Guid Id { get { return this.id; } }

        internal readonly DotNetTasks.Task InternalTask;
        readonly System.Threading.CancellationTokenSource cancelToken = new System.Threading.CancellationTokenSource();

        internal Task(Action action)
        {
            this.InternalTask = DotNetTasks.Task.Factory.StartNew(() =>
            {
                var currentThread = System.Threading.Thread.CurrentThread;
                using (cancelToken.Token.Register(currentThread.Abort))
                {
                    action();
                }
            }, cancelToken.Token);
        }

        public void Join()
        {
            this.InternalTask.Wait();
        }

        public void Dispose()
        {
            this.InternalTask.Dispose();
        }


        public void Abort()
        {
            cancelToken.Cancel();
            this.InternalTask.Wait();
        }
    }
}
