using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TimeExt.VirtualImplementations
{
    /// <summary>
    /// テスト用の回数によって時間が変わるwaitを提供するクラスです。
    /// </summary>
    public static class Waiter
    {
        /// <summary>
        /// 呼び出すごとに待つ時間が異なるようなwait関数を作成します。
        /// </summary>
        public static Action CreateWaiter(this ITimeline tl, params TimeSpan[] timeSpans)
        {
            int i = 0;
            return () =>
            {
                if (timeSpans.Length <= i)
                    throw new InvalidOperationException(string.Format("{0}回より多い回数のwaitが発生しました。", timeSpans.Length));

                tl.WaitForTime(timeSpans[i++]);
            };
        }

        /// <summary>
        /// 呼び出すごとに待つ時間が異なるようなwait関数を作成します。
        /// それぞれの時間を数値として渡すと、関数fがそれぞれに適用されます。
        /// 例えば、関数fにTimeSpan.FromSecondsを渡した場合、その後ろの引数は秒数を表します。
        /// </summary>
        public static Action CreateWaiter(this ITimeline tl, Func<double, TimeSpan> f, params double[] timeSpanValues)
        {
            return CreateWaiter(tl, timeSpanValues.Select(f).ToArray());
        }
    }
}
