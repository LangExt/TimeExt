using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeExt
{
    /// <summary>
    /// TimeExtが提供する一連のインターフェイスのインスタンスを作成するためのメソッド群を提供します。
    /// </summary>
    public interface IFactory
    {
        /// <summary>
        /// ITimeline インターフェイスを実装しているクラスの新しいインスタンスを返します。
        /// </summary>
        ITimeline CreateTimeline();

        /// <summary>
        /// ITaskJoin インターフェイスを実装しているクラスの新しいインスタンスを返します。
        /// </summary>
        ITaskJoin CreateTaskJoin();
    }
    
    /// <summary>
    /// ストップウォッチを表すインターフェイスです。
    /// このストップウォッチは明示的な開始操作はないので、
    /// インスタンスを生成すると自動的に開始される点に注意してください。
    /// </summary>
    public interface IStopwatch
    {
        /// <summary>
        /// ストップウォッチ開始時からの経過時間を取得します。
        /// </summary>
        TimeSpan Elapsed { get; }
    }

    /// <summary>
    /// タスクを表すインターフェイスです。
    /// このタスクには明示的な開始操作はなく、
    /// インスタンスを生成したと同時に自動的に開始される点に注意してください。
    /// タスクを明示的に終了したい場合は、Disposeメソッドを呼び出してください。
    /// </summary>
    public interface ITask : IDisposable
    {
        Guid Id { get; }

        void Join();

        void Abort();
    }

    public interface ITaskJoin
    {
        void JoinAll(IEnumerable<ITask> tasks);
    }

    /// <summary>
    /// 一定周期で処理を実行することのできるタイマーを表すインターフェイスです。
    /// </summary>
    public interface ITimer : IDisposable
    {
        /// <summary>
        /// タイマーに設定した刻み毎に呼び出されるイベントです。
        /// </summary>
        event EventHandler Tick;

        void Start();
    }

    /// <summary>
    /// タイマー起動時にTickイベントを発火するかどうかを表す列挙型です。
    /// </summary>
    public enum InitialTick
    {
        /// <summary>
        /// タイマー起動時にTickイベントを発火しないことを表します。
        /// </summary>
        Disabled,

        /// <summary>
        /// タイマー起動時にTickイベントを発火することを表します。
        /// </summary>
        Enabled
    }

    /// <summary>
    /// 時間の流れを表すインターフェイスです。
    /// DateTime.NowやThread.Sleepを呼び出す代わりにこのインターフェイスのメソッドを呼び出すことで、
    /// 時間に依存したロジックのテストを可能にします。
    /// 通常、アプリケーションで一つこのインターフェイスのインスタンスを生成し、
    /// そのインスタンスに対して時間に関する操作を行うようにします。
    /// </summary>
    public interface ITimeline
    {
        /// <summary>
        /// コンピュータの現在時刻をUTCで表したオブジェクトを取得します。
        /// </summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// 指定した時間だけ待ちます。
        /// このメソッドを呼び出した後は、Nowの値がtimeSpan分だけ進んでいます。
        /// </summary>
        /// <param name="timeSpan">待つ時間</param>
        void WaitForTime(TimeSpan timeSpan);

        /// <summary>
        /// ストップウォッチを生成します。
        /// 生成されたストップウォッチは、すでに開始している点に注意してください。
        /// </summary>
        IStopwatch CreateStopwatch();

        /// <summary>
        /// タスクを生成します。
        /// 生成されたタスクは、すでに開始している点に注意してください。
        /// </summary>
        /// <returns></returns>
        ITask CreateTask(Action action);

        /// <summary>
        /// 間隔を指定してタイマーを生成します。
        /// 生成したタイマーは、すでに起動していることに注意してください。
        /// initialTickにEnabledを指定しない場合、最初のTickはinterval経過した後で初めて発火します(タイマーを生成した時点ではTickしない)。
        /// </summary>
        /// <param name="interval">タイマーのTickイベントを発火する間隔</param>
        /// <param name="initialTick">生成直後にTickイベントを発火するかどうか</param>
        ITimer CreateTimer(TimeSpan interval, InitialTick initialTick = InitialTick.Disabled);
    }
}
