using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DotNetTasks = System.Threading.Tasks;

namespace TimeExt.RealImplementations
{
    /// <summary>
    /// RealImplementations.TaskをAbortした際の情報を保持するクラスです。
    /// </summary>
    public sealed class AbortInfo
    {
        /// <summary>
        /// AbortしたITaskのIdを取得します。
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// AbortしたITaskを実行していたThreadのManagedThreadIdを取得します。
        /// </summary>
        public int ThreadId { get; internal set; }

        /// <summary>
        /// AbortしたITaskを実行していたSystem.Threading.Tasks.TaskのIdを取得します。
        /// </summary>
        public int? TaskId { get; internal set; }

        /// <summary>
        /// AbortしたITaskが開始された日時を取得します。
        /// </summary>
        public DateTime Started { get; internal set; }

        /// <summary>
        /// ITaskをAbortした日時を取得します。
        /// </summary>
        public DateTime Aborted { get; internal set; }

        /// <summary>
        /// AbortしたITask実装クラスのコンストラクタを呼び出したStackTraceを取得します。
        /// IsEnableStackTraceがfalseの場合、"disabled"が返されます。
        /// </summary>
        public string StackTrace { get; internal set; }

        /// <summary>
        /// StackTraceの取得が有効かどうかを表す値を取得・設定します。
        /// </summary>
        public static bool IsEnableStackTrace { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Id={0}, ThreadId={1}, TaskId={2}, Started={3}, Aborted={4}, StackTrace={5}",
                this.Id, this.ThreadId, this.TaskId, this.Started, this.Aborted, this.StackTrace);
        }
    }

    [DebuggerDisplay("Task Id = {Id}")]
    internal sealed class Task : ITask
    {
        readonly Guid id = Guid.NewGuid();
        public Guid Id { get { return this.id; } }

        internal readonly DotNetTasks.Task InternalTask;
        readonly System.Threading.CancellationTokenSource cancelToken = new System.Threading.CancellationTokenSource();

        internal Task(Action action)
        {
            var stackTrace = AbortInfo.IsEnableStackTrace ? Environment.StackTrace : "disabled";
            var start = DateTime.UtcNow;
            this.InternalTask = DotNetTasks.Task.Factory.StartNew(() =>
            {
                var currentThread = System.Threading.Thread.CurrentThread;
                var abortInfo = new AbortInfo
                {
                    Id = this.id,
                    ThreadId = currentThread.ManagedThreadId,
                    TaskId = DotNetTasks.Task.CurrentId,
                    StackTrace = stackTrace,
                    Started = start
                };
                using (cancelToken.Token.Register(() => { abortInfo.Aborted = DateTime.UtcNow; currentThread.Abort(abortInfo); }))
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
