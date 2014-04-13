using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeExt
{
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
    /// 一定周期で処理を実行することのできるタイマーを表すインターフェイスです。
    /// </summary>
    public interface ITimer
    {
        /// <summary>
        /// タイマーに設定した刻み毎に呼び出されるイベントです。
        /// </summary>
        event EventHandler Tick;
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
        /// 現在時刻を取得します。
        /// </summary>
        DateTime Now { get; }

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
        /// 間隔を指定してタイマーを生成します。
        /// 生成したタイマーは、すでに起動していることに注意してください。
        /// 最初のTickは、interval経過した後で初めて呼び出されます(タイマーを生成した時点ではTickしない)。
        /// </summary>
        /// <param name="interval">タイマーのTickイベントが呼び出される間隔</param>
        ITimer CreateTimer(TimeSpan interval);
    }
}
