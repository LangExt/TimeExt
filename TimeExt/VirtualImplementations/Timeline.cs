using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    internal sealed class RelativeTimeline
    {
        internal readonly DateTime Origin;

        TimeSpan passed;

        internal RelativeTimeline(DateTime origin)
        {
            this.Origin = origin;
        }

        internal void WaitForTime(TimeSpan span)
        {
            this.passed += span;
        }

        // 基準時刻(Origin)に待った時間の合計を足せば、その時点での現在時刻になる
        internal DateTime UtcNow
        {
            get { return this.Origin + this.passed; }
        }

        internal void SetNewUtcNow(DateTime newNow)
        {
            var diff = newNow - this.UtcNow;
            if(0 < diff.Ticks)
                this.passed += diff;
        }
    }

    internal sealed class ChangedNowEventArgs : EventArgs
    {
        internal readonly TimeSpan Delta;

        internal ChangedNowEventArgs(TimeSpan delta)
        {
            this.Delta = delta;
        }
    }

    internal sealed class RelativeTimelineScope : IDisposable
    {
        readonly Stack<RelativeTimeline> timelines;

        public RelativeTimelineScope(Stack<RelativeTimeline> timelines, DateTime now)
        {
            this.timelines = timelines;

            var newTimeline = new RelativeTimeline(now);
            this.timelines.Push(newTimeline);
        }

        public void Dispose()
        {
            this.timelines.Pop();
        }
    }

    internal interface IFireable
    {
        void Fire(DateTime now);
    }

    /// <summary>
    /// Fireのリクエスト情報を保持するクラスです。
    /// Fireのリクエスト情報には、リクエストを要求したTimerと、
    /// Fireが発生すべき時刻が含まれます。
    /// </summary>
    internal sealed class FireRequest
    {
        internal readonly IFireable Fireable;
        internal readonly DateTime TickTime;

        internal FireRequest(IFireable timer, DateTime tickTime)
        {
            this.Fireable = timer;
            this.TickTime = tickTime;
        }

        public override bool Equals(object obj)
        {
            var other = obj as FireRequest;
            if (other == null)
                return false;
            return object.ReferenceEquals(this.Fireable, other.Fireable) && this.TickTime == other.TickTime;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(this.Fireable, this.TickTime).GetHashCode();
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
    public sealed class Timeline : ITimeline
    {
        internal event EventHandler ChangingNow;
        internal event EventHandler<ChangedNowEventArgs> ChangedNow;

        readonly Stack<RelativeTimeline> timelines = new Stack<RelativeTimeline>();

        // 既にTickされたものを再度Tickしないようにするために、historyとして保持しておく
        readonly ISet<FireRequest> history = new HashSet<FireRequest>();

        internal void RequestFire(FireRequest req)
        {
            // Fireがリクエストされても、既にhistoryに同じリクエストがある場合はTickしない
            if (this.history.Contains(req))
                return;

            this.history.Add(req);
            req.Fireable.Fire(req.TickTime);
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
            timelines.Push(new RelativeTimeline(origin));
        }

        internal RelativeTimelineScope CreateNewTimeline(DateTime now)
        {
            return new RelativeTimelineScope(this.timelines, now);
        }

        readonly IDictionary<Tuple<Timer, RelativeTimeline>, long> remainedTicksDict =
            new Dictionary<Tuple<Timer, RelativeTimeline>, long>();

        internal long GetCurrentRemainedTicks(Timer timer)
        {
            var timeline = this.timelines.Peek();
            if (this.remainedTicksDict.ContainsKey(Tuple.Create(timer, timeline)) == false)
                return 0;
            return this.remainedTicksDict[Tuple.Create(timer, timeline)];
        }

        internal void SetCurrentRemainedTicks(Timer timer, long newValue)
        {
            var timeline = this.timelines.Peek();
            this.remainedTicksDict[Tuple.Create(timer, timeline)] = newValue;
        }

        public void WaitForTime(TimeSpan span)
        {
            EventHelper.Raise(this.ChangingNow, this, EventArgs.Empty);
            // 現在時刻を指定時間分進めます。
            // その過程で、タイマーと連動(ChangedNowにタイマーのOnChangedNowが登録される)して、
            // 指定周期が満たされた分だけタイマーのTickイベントを発火します。
            timelines.Peek().WaitForTime(span);
            EventHelper.Raise(this.ChangedNow, this, new ChangedNowEventArgs(span));
        }

        internal void SetContextIfNeed(DateTime dateTime)
        {
            // 現在時刻が進む可能性があるが、既に進められた時刻に追いつこうとしているだけなので、
            // ここでChangingNowやChangedNowを呼び出す必要はない(呼び出してもいいが、なにも起こらない)
            timelines.Peek().SetNewUtcNow(dateTime);
        }

        public DateTime UtcNow
        {
            get { return timelines.Peek().UtcNow; }
        }

        public ITask CreateTask(Action action)
        {
            return new Task(this, this.timelines.Peek(), UtcNow, action);
        }

        public ITimer CreateTimer(TimeSpan interval, InitialTick initialTick = InitialTick.Disabled)
        {
            return new Timer(this, interval, initialTick);
        }

        public IStopwatch CreateStopwatch()
        {
            var start = UtcNow;
            return new Stopwatch(() => UtcNow - start);
        }

        public Action CreateWaiter(params TimeSpan[] timeSpans)
        {
            int i = 0;
            return () =>
            {
                if (timeSpans.Length <= i) throw new InvalidOperationException("予期せぬwait");

                WaitForTime(timeSpans[i++]);
            };
        }

        public Action CreateWaiter(Func<double, TimeSpan> f, params double[] timeSpanValues)
        {
            return this.CreateWaiter(timeSpanValues.Select(f).ToArray());
        }
    }
}
