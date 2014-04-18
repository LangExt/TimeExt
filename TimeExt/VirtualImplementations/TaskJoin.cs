using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class TaskJoin : ITaskJoin
    {
        public void JoinAll(IEnumerable<ITask> tasks)
        {
            var exceptions = new List<Exception>();
            foreach (var task in tasks)
            {
                try
                {
                    task.Join();
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // ThreadAbortExceptionは無視
                    throw;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count != 0)
                throw new AggregateException(exceptions);
        }
    }
}
