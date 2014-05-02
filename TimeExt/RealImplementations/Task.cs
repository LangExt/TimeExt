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
        }
    }
}
