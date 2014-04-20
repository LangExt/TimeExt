using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class Task : ITask, IExecution
    {
        readonly Timeline timeline;
        readonly ExecutionContext currentContext;
        readonly DateTime origin;
        readonly Action action;

        internal Task(Timeline timeline, ExecutionContext currentContext, DateTime origin, Action action)
        {
            this.timeline = timeline;
            this.currentContext = currentContext;
            this.origin = origin;
            this.action = action;

            timeline.ChangingNow += OnChangingNow;
        }

        void OnChangingNow(object sender, EventArgs e)
        {
            if (origin <= currentContext.UtcNow)
                this.timeline.ExecuteScheduleIfNeed(new ScheduledExecution(this, origin));
        }

        DateTime end;
        List<Exception> exceptions = new List<Exception>();

        public void Execute()
        {
            using (var newContext = this.timeline.CreateNewExecutionContext(origin))
            {
                try
                {
                    action();
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // ThreadAbortExceptionは無視する
                    throw;
                }
                catch (Exception e)
                {
                    this.exceptions.Add(e);
                }
                finally
                {
                    this.end = this.timeline.UtcNow; // 異常終了したときこれでいいのか・・・？
                }
            }
        }

        public void Dispose()
        {
            // for the real world.
        }

        public void Join()
        {
            timeline.SetContextIfNeed(this.end);
            if (this.exceptions.Count != 0)
                throw new AggregateException(this.exceptions);
        }

        public void Abort()
        {
            timeline.Abort();
        }
    }
}
