﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class ExecutionContext
    {
        readonly DateTime origin;

        TimeSpan passed;

        internal ExecutionContext(DateTime origin)
        {
            this.origin = origin;
        }

        internal void WaitForTime(TimeSpan span)
        {
            this.passed += span;
        }

        // 基準時刻(Origin)に待った時間の合計を足せば、その時点での現在時刻になる
        internal DateTime UtcNow
        {
            get { return this.origin + this.passed; }
        }

        internal void SetNewOrigin(DateTime newOrigin)
        {
            var diff = newOrigin - this.UtcNow;
            if(0 < diff.Ticks)
                this.passed += diff;
        }
    }

    internal sealed class ChangingNowEventArgs : EventArgs
    {
        internal readonly TimeSpan Delta;

        internal ChangingNowEventArgs(TimeSpan delta)
        {
            this.Delta = delta;
        }
    }

    internal sealed class ExecutionContextScope : IDisposable
    {
        readonly Stack<ExecutionContext> contextStack;

        public ExecutionContextScope(Stack<ExecutionContext> contextStack, DateTime origin)
        {
            this.contextStack = contextStack;

            var newContext = new ExecutionContext(origin);
            this.contextStack.Push(newContext);
        }

        public void Dispose()
        {
            this.contextStack.Pop();
        }
    }

    /// <summary>
    /// スケジュールされたタスクの情報を保持するクラスです。
    /// スケジュールされたタスクの情報には、スケジュールされたタスクそのものと、
    /// スケジュールされている時刻が含まれます。
    /// </summary>
    internal sealed class ScheduledTask
    {
        internal readonly Task Task;
        internal readonly DateTime Origin;

        internal ScheduledTask(Task task, DateTime origin)
        {
            this.Task = task;
            this.Origin = origin;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ScheduledTask;
            if (other == null)
                return false;
            return this.Task.Equals(other.Task) && this.Origin == other.Origin;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(this.Task, this.Origin).GetHashCode();
        }
    }

    /// <summary>
    /// 時間に依存するロジックをテストするために使用するクラスです。
    /// このクラスは、タイマーによる非同期コードを直列化することで、
    /// 実時間を消費する必要なしに時間に依存するロジックに対するテストを実現します。
    /// 通常、テストケースごとにこのクラスのインスタンスを生成し、
    /// アプリケーションで使用するITimelineをそのインスタンスに変更することで、
    /// 時間に依存するロジックをテスト可能にします。
    /// </summary>
    internal sealed class Timeline : ITimeline
    {
        internal event EventHandler<ChangingNowEventArgs> ChangingNow;

        internal readonly Stack<ExecutionContext> contextStack = new Stack<ExecutionContext>();

        // 既に実行されたものを再度実行しないようにするために、scheduledTasksとして保持しておく
        readonly ISet<ScheduledTask> scheduledTasks = new HashSet<ScheduledTask>();

        internal void Schedule(ScheduledTask scheduled)
        {
            // Scheduleがリクエストされても、既にscheduledTasksに同じスケジュールがある場合は実行しない
            if (this.scheduledTasks.Contains(scheduled))
                return;

            this.scheduledTasks.Add(scheduled);
            using (var scope = CreateNewExecutionContext(scheduled.Origin))
                scheduled.Task.Execute();
        }

        /// <summary>
        /// 基準時刻を指定してインスタンスを生成します。
        /// 基準時刻のKindには、DateTimeKind.Utcが設定されている必要があります。
        /// そのため、DateTime.UtcNowを使うか、ToUniversalTimeメソッドを呼び出すなどして、
        /// UTCに変換したうえで使用してください。
        /// </summary>
        /// <param name="origin">基準時刻</param>
        internal Timeline(DateTime origin)
        {
            if (origin.Kind != DateTimeKind.Utc)
                throw new ArgumentException("基準となる時刻にはUTCを指定する必要があります。", "origin");
            contextStack.Push(new ExecutionContext(origin));
        }

        internal ExecutionContextScope CreateNewExecutionContext(DateTime origin)
        {
            return new ExecutionContextScope(this.contextStack, origin);
        }

        readonly IDictionary<Tuple<Timer, ExecutionContext>, long> remainedTicksDict =
            new Dictionary<Tuple<Timer, ExecutionContext>, long>();

        internal long GetCurrentRemainedTicks(Timer timer)
        {
            var context = this.contextStack.Peek();
            if (this.remainedTicksDict.ContainsKey(Tuple.Create(timer, context)) == false)
                return 0;
            return this.remainedTicksDict[Tuple.Create(timer, context)];
        }

        internal void SetCurrentRemainedTicks(Timer timer, long newValue)
        {
            var context = this.contextStack.Peek();
            this.remainedTicksDict[Tuple.Create(timer, context)] = newValue;
        }

        public void WaitForTime(TimeSpan span)
        {
            EventHelper.Raise(this.ChangingNow, this, new ChangingNowEventArgs(span));
            // 現在時刻を指定時間分進めます。
            // その過程で、タイマーと連動(ChangedNowにタイマーのOnChangedNowが登録される)して、
            // 指定周期が満たされた分だけタイマーのTickイベントを発火します。
            contextStack.Peek().WaitForTime(span);
        }

        internal void SetContextIfNeed(DateTime origin)
        {
            // 現在時刻が進む可能性があるが、既に進められた時刻に追いつこうとしているだけなので、
            // ここでChangingNowやChangedNowを呼び出す必要はない(呼び出してもいいが、なにも起こらない)
            contextStack.Peek().SetNewOrigin(origin);
        }

        public DateTime UtcNow
        {
            get { return contextStack.Peek().UtcNow; }
        }

        public ITask CreateTask(Action action)
        {
            var task = new Task(this, action);
            this.Schedule(new ScheduledTask(task, this.UtcNow));
            return task;
        }

        public ITimer CreateTimer(TimeSpan interval, InitialTick initialTick = InitialTick.Disabled)
        {
            return new Timer(this, interval, initialTick);
        }

        public IStopwatch CreateStopwatch()
        {
            var origin = UtcNow;
            return new Stopwatch(() => UtcNow - origin);
        }

        // Waiterの生成はこのクラスから分離すべきかも
        public Action CreateWaiter(params TimeSpan[] timeSpans)
        {
            int i = 0;
            return () =>
            {
                if (timeSpans.Length <= i) throw new InvalidOperationException("予期せぬwait");

                WaitForTime(timeSpans[i++]);
            };
        }

        // Waiterの生成はこのクラスから分離すべきかも
        public Action CreateWaiter(Func<double, TimeSpan> f, params double[] timeSpanValues)
        {
            return this.CreateWaiter(timeSpanValues.Select(f).ToArray());
        }

	// このメソッドは、テスト以外では使われない。プロダクトコードでは、代わりにITask.Abortを使うこと。
        internal void Abort()
        {
        }
    }
}
