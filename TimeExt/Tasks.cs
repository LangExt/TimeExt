using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt
{
    public static class Tasks
    {
        public static void JoinAll(params ITask[] tasks)
        {
            // 簡易実装
            foreach (var task in tasks)
                task.Join();
        }
    }
}
