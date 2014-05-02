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
                var forIsExit = new object();
                var isExit = false;

                var currentThread = System.Threading.Thread.CurrentThread;
                cancelToken.Token.Register(() =>
                {
                    lock (forIsExit)
                    {
                        // 実行が終了していたらAbortを呼ばない(呼ぶと、既に別の仕事をしている可能性のあるcurrentThreadをAbortしてしまう)
                        if (isExit)
                            return;
                        currentThread.Abort();
                    }
                });
                try
                {
                    action();
                }
                finally
                {
                    lock (forIsExit)
                    {
                        isExit = true;
                    }
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
