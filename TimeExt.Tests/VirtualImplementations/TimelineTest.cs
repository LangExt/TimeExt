using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimeExt.VirtualImplementations;

namespace TimeExt.Tests.VirtualImplementations
{
    [TestFixture]
    public class TimelineTest
    {
        DateTime origin;
        IFactory factory;

        [SetUp]
        public void SetUp()
        {
            this.origin = DateTime.Parse("2014/01/01").ToUniversalTime();
            this.factory = new Factory(origin);
        }

        [Test]
        public void 複数のタイマーが扱える()
        {
            var tl = new Timeline(origin);

            var timerA = tl.CreateTimer(TimeSpan.FromSeconds(5));
            var waitA = tl.CreateWaiter(TimeSpan.FromSeconds, 2, 2, 2, 2, 2);
            var countA = 0;
            timerA.Tick += (sender, arg) => { var now = tl.UtcNow; countA++; waitA(); };

            var timerB = tl.CreateTimer(TimeSpan.FromSeconds(8));
            var waitB = tl.CreateWaiter(TimeSpan.FromSeconds, 3, 3, 3);
            var countB = 0;
            timerB.Tick += (sender, arg) => { var now = tl.UtcNow; countB++; waitB(); };

            tl.WaitForTime(TimeSpan.FromSeconds(29));

            Assert.That(countA, Is.EqualTo(5));
            Assert.That(countB, Is.EqualTo(3));
            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(29)));
        }

        [Test]
        public void 複数のタイマーが干渉しない()
        {
            var tl = new Timeline(origin);

            var timerA = tl.CreateTimer(TimeSpan.FromSeconds(5));
            var waitA = tl.CreateWaiter(TimeSpan.FromSeconds, 10, 2, 2, 2, 2);
            var historyA = new List<DateTime>();
            timerA.Tick += (sender, arg) => { historyA.Add(tl.UtcNow); waitA(); };

            var timerB = tl.CreateTimer(TimeSpan.FromSeconds(5));
            var waitB = tl.CreateWaiter(TimeSpan.FromSeconds, 2, 2, 2, 2, 2);
            var historyB = new List<DateTime>();
            timerB.Tick += (sender, arg) => { historyB.Add(tl.UtcNow); waitB(); };

            tl.WaitForTime(TimeSpan.FromSeconds(29));

            var exceptedHistory = Enumerable.Repeat(origin, 5).Select((d, i) => d + TimeSpan.FromSeconds(5 * (i + 1))).ToArray();
            Assert.That(historyA.ToArray(), Is.EqualTo(exceptedHistory));
            Assert.That(historyB.ToArray(), Is.EqualTo(exceptedHistory));
            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(29)));
        }

        [Test]
        public void 初回チックがある複数のタイマーが干渉しない()
        {
            var tl = new Timeline(origin);

            var timerA = tl.CreateTimer(TimeSpan.FromSeconds(5), InitialTick.Enabled);
            var waitA = tl.CreateWaiter(TimeSpan.FromSeconds, 10, 2, 2, 2, 2, 2);
            var historyA = new List<DateTime>();
            timerA.Tick += (sender, arg) => { historyA.Add(tl.UtcNow); waitA(); };

            var timerB = tl.CreateTimer(TimeSpan.FromSeconds(5), InitialTick.Enabled);
            var waitB = tl.CreateWaiter(TimeSpan.FromSeconds, 2, 2, 2, 2, 2, 2);
            var historyB = new List<DateTime>();
            timerB.Tick += (sender, arg) => { historyB.Add(tl.UtcNow); waitB(); };

            tl.WaitForTime(TimeSpan.FromSeconds(29));

            var exceptedHistory = Enumerable.Repeat(origin, 6).Select((d, i) => d + TimeSpan.FromSeconds(5 * (i))).ToArray();
            Assert.That(historyA.ToArray(), Is.EqualTo(exceptedHistory));
            Assert.That(historyB.ToArray(), Is.EqualTo(exceptedHistory));
            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(29)));
        }

        [Test]
        public void 初回チックがある一定間隔の待ちタイマーが干渉しない()
        {
            var tl = new Timeline(origin);

            var timerA = tl.CreateTimer(TimeSpan.FromSeconds(5), InitialTick.Enabled);
            var waitA = tl.CreateWaiter(TimeSpan.FromSeconds, 2, 2, 2, 2, 2, 2);
            var historyA = new List<DateTime>();
            timerA.Tick += (sender, arg) => { historyA.Add(tl.UtcNow); waitA(); };

            var timerB = tl.CreateTimer(TimeSpan.FromSeconds(5), InitialTick.Enabled);
            var waitB = tl.CreateWaiter(TimeSpan.FromSeconds, 2, 2, 2, 2, 2, 2);
            var historyB = new List<DateTime>();
            timerB.Tick += (sender, arg) => { historyB.Add(tl.UtcNow); waitB(); };

            tl.WaitForTime(TimeSpan.FromSeconds(29));

            var exceptedHistory = Enumerable.Repeat(origin, 6).Select((d, i) => d + TimeSpan.FromSeconds(5 * (i))).ToArray();
            Assert.That(historyA.ToArray(), Is.EqualTo(exceptedHistory));
            Assert.That(historyB.ToArray(), Is.EqualTo(exceptedHistory));
            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(29)));
        }

        [Test]
        public void タスクを扱える()
        {
            var tl = new Timeline(origin);

            var count = 0;
            var task = tl.CreateTask(() => { count++; });

            tl.WaitForTime(TimeSpan.FromSeconds(3));

            Assert.That(count, Is.EqualTo(1));
            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(3)));
        }

        [Test]
        public void 複数のタスクを扱える()
        {
            var tl = new Timeline(origin);

            var countA = 0;
            var taskA = tl.CreateTask(() => { countA++; });

            var countB = 0;
            var taskB = tl.CreateTask(() => { countB++; });

            tl.WaitForTime(TimeSpan.FromSeconds(3));

            Assert.That(countA, Is.EqualTo(1));
            Assert.That(countB, Is.EqualTo(1));
            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(3)));
        }

        [Test]
        public void タスクの中で例外を投げてもJoinを呼び出すまでは無視される()
        {
            var tl = new Timeline(origin);

            var task = tl.CreateTask(() => { throw new Exception("oops!"); });

            Assert.That(() => task.Join(), Throws.Exception.TypeOf<AggregateException>().And.InnerException.TypeOf<Exception>());
        }

        [Test]
        public void 複数のタスクの中で例外を投げてもJoinするまでは無視される()
        {
            var tl = new Timeline(origin);

            var taskA = tl.CreateTask(() => { throw new Exception("oops!"); });
            var taskB = tl.CreateTask(() => { throw new InvalidOperationException("oops!"); });

            try
            {
                factory.CreateTaskJoin().JoinAll(new[] { taskA, taskB });
            }
            catch (AggregateException e)
            {
                e = e.Flatten();
                Assert.That(e.InnerExceptions.Count, Is.EqualTo(2));
                Assert.That(e.InnerExceptions[0], Is.TypeOf<Exception>());
                Assert.That(e.InnerExceptions[1], Is.TypeOf<InvalidOperationException>());
            }
        }

        [Test]
        public void タスクの中でタスクが扱える()
        {
            var tl = new Timeline(origin);

            var count = 0;
            tl.CreateTask(() =>
            {
                tl.CreateTask(() => { count++; });
            });
            tl.WaitForTime(TimeSpan.Zero);

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void 内部で待つタスクとタイマーを同時に扱える()
        {
            var tl = new Timeline(origin);

            var countA = 0;
            var task = tl.CreateTask(() => { countA++; tl.WaitForTime(TimeSpan.FromSeconds(6)); }); // ここではまだtimerが生成されてないのでtickは呼ばれない。

            var countB = 0;
            var timer = tl.CreateTimer(TimeSpan.FromSeconds(3), InitialTick.Enabled);
            timer.Tick += delegate { countB++; };

            tl.WaitForTime(TimeSpan.FromSeconds(1));

            Assert.That(countA, Is.EqualTo(1));
            Assert.That(countB, Is.EqualTo(1));
            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void タスクと内部で待つタイマーを同時に扱える()
        {
            var tl = new Timeline(origin);

            var countA = 0;
            var task = tl.CreateTask(() => { countA++; });

            var countB = 0;
            var timer = tl.CreateTimer(TimeSpan.FromSeconds(3), InitialTick.Enabled);
            timer.Tick += delegate { countB++; tl.WaitForTime(TimeSpan.FromSeconds(1)); };

            tl.WaitForTime(TimeSpan.FromSeconds(10));

            Assert.That(countA, Is.EqualTo(1));
            Assert.That(countB, Is.EqualTo(4));
            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(10)));
        }

        [Test]
        public void タスクの終了を待ち受けれる()
        {
            var tl = new Timeline(origin);

            var task = tl.CreateTask(() => { tl.WaitForTime(TimeSpan.FromSeconds(10)); });
            tl.WaitForTime(TimeSpan.FromSeconds(1));

            task.Join();

            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(10)));
        }

        [Test]
        public void 複数のタスクの終了を待ち受けれる()
        {
            var tl = new Timeline(origin);

            var taskA = tl.CreateTask(() => { tl.WaitForTime(TimeSpan.FromSeconds(10)); });
            var taskB = tl.CreateTask(() => { tl.WaitForTime(TimeSpan.FromSeconds(20)); });
            var taskC = tl.CreateTask(() => { tl.WaitForTime(TimeSpan.FromSeconds(5)); });
            tl.WaitForTime(TimeSpan.FromSeconds(1));

            factory.CreateTaskJoin().JoinAll(new[] { taskA, taskB, taskC });

            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(20)));
        }

        [Test]
        public void TimelineにUTCではないDateTimeを渡すと例外が投げられる()
        {
            Assert.That(() => new Timeline(DateTime.Now), Throws.Exception.TypeOf<ArgumentException>());
        }

        [TestCase("2014/01/01")]
        [TestCase("2014/01/02")]
        public void Timelineは現在時刻を取得できる(string now)
        {
            ITimeline tl = new Timeline(DateTime.Parse(now).ToUniversalTime());
            Assert.That(tl.UtcNow, Is.EqualTo(DateTime.Parse(now).ToUniversalTime()));
        }


        [Test]
        public void Timelineは時間を進めると現在時刻がその分進んでいる()
        {
            var tl = new Timeline(this.origin);

            tl.WaitForTime(TimeSpan.FromSeconds(5));
            Assert.That(tl.UtcNow, Is.EqualTo(this.origin + TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void ブロック間の時間を計測して進めた分だけの時間が取得できる()
        {
            var tl = new Timeline(this.origin);
            var sw = tl.CreateStopwatch();

            tl.WaitForTime(TimeSpan.FromSeconds(5));
            Assert.That(sw.Elapsed, Is.EqualTo(TimeSpan.FromSeconds(5)));
        }

        [Test]
        public void タスクをアボートするとそれ以降の待ち処理は実行されない()
        {
            var tl = new Timeline(origin);
            var task = tl.CreateTask(() =>
            {
                tl.Abort();
                tl.WaitForTime(TimeSpan.FromSeconds(60));
            });

            task.Join();

            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(0)));
        }

        [Test]
        public void タスクをアボートしてもそれ以前の待ち処理は実行される()
        {
            var tl = new Timeline(origin);
            var task = tl.CreateTask(() =>
            {
                tl.WaitForTime(TimeSpan.FromSeconds(60));
                tl.Abort();
            });

            task.Join();

            Assert.That(tl.UtcNow, Is.EqualTo(origin + TimeSpan.FromSeconds(60)));
        }

        public class タイマー起動時にTickしないタイマーのテスト
        {
            readonly DateTime origin = DateTime.Parse("2014/01/01").ToUniversalTime();

            [Test]
            public void 指定した間隔分の時間を進めた時に初回のTickイベントが発火される()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3));
                var isFired = false;
                timer.Tick += (sender, args) => { isFired = true; };
                tl.WaitForTime(TimeSpan.FromSeconds(3));

                Assert.That(isFired, Is.True);
            }

            [Test]
            public void 指定した間隔の3回分時間のかかる処理を実行したとき4回実行される()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3));
                var wait = tl.CreateWaiter(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromTicks(1));
                var count = 0;
                var history = new List<DateTime>();
                timer.Tick += (sender, args) => { var now = tl.UtcNow; history.Add(now); ++count; wait(); };
                tl.WaitForTime(TimeSpan.FromSeconds(3));

                Assert.That(count, Is.EqualTo(4));
                Assert.That(tl.UtcNow, Is.EqualTo(this.origin + TimeSpan.FromSeconds(3)));
            }

            [Test]
            public void 指定した間隔で時間のかかる処理を実行してもメインでその分待っていれば余計に実行されない()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3));
                var wait = tl.CreateWaiter(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
                var count = 0;
                timer.Tick += (sender, args) => { var start = tl.UtcNow; ++count; wait(); var end = tl.UtcNow; Console.WriteLine(); };
                tl.WaitForTime(TimeSpan.FromSeconds(14));

                // 余計に実行されない確認
                Assert.That(count, Is.EqualTo(14 / 3 /*4*/));
                // 余計に待たない確認
                Assert.That(tl.UtcNow, Is.EqualTo(this.origin + TimeSpan.FromSeconds(14)));
            }

            [Test]
            public void 指定した間隔2回分の時間が進めた時に2回Tickイベントが発火される()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3));
                var count = 0;
                timer.Tick += (sender, args) => { count++; };
                tl.WaitForTime(TimeSpan.FromSeconds(6));

                Assert.That(count, Is.EqualTo(2));
            }

            [Test]
            public void 指定した間隔を2回に分けて進めた時に初回のTickイベントが発火される()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3));
                var isFired = false;
                timer.Tick += (sender, args) => { isFired = true; };
                tl.WaitForTime(TimeSpan.FromSeconds(1));
                tl.WaitForTime(TimeSpan.FromSeconds(2));

                Assert.That(isFired, Is.True);
                Assert.That(tl.UtcNow, Is.EqualTo(this.origin + TimeSpan.FromSeconds(3)));
            }

            [TestCase(1, 8)]
            [TestCase(4, 5)]
            public void 指定した間隔3回分を2回に分けて進めたときに3回Tickイベントが発火される(int firstSpan, int secondSpan)
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3));
                var count = 0;
                timer.Tick += (sender, args) => { count++; };
                tl.WaitForTime(TimeSpan.FromSeconds(firstSpan));
                tl.WaitForTime(TimeSpan.FromSeconds(secondSpan));

                Assert.That(count, Is.EqualTo(3));
            }

            [TestCase(0, 1, 1)]
            [TestCase(3, 1, 1)]
            [TestCase(5, 1, 1)]
            [TestCase(6, 1, 1)]
            [TestCase(0, 3, 1)]
            [TestCase(3, 3, 1)]
            [TestCase(5, 3, 1)]
            [TestCase(6, 3, 1)]
            [TestCase(0, 10, 1)]
            [TestCase(3, 10, 1)]
            [TestCase(5, 10, 1)]
            [TestCase(6, 10, 1)]
            [TestCase(0, 1, 2)]
            [TestCase(3, 1, 2)]
            [TestCase(5, 1, 2)]
            [TestCase(6, 1, 2)]
            [TestCase(0, 3, 2)]
            [TestCase(3, 3, 2)]
            [TestCase(5, 3, 2)]
            [TestCase(6, 3, 2)]
            [TestCase(0, 10, 2)]
            [TestCase(3, 10, 2)]
            [TestCase(5, 10, 2)]
            [TestCase(6, 10, 2)]
            [TestCase(0, 1, 10)]
            [TestCase(3, 1, 10)]
            [TestCase(5, 1, 10)]
            [TestCase(6, 1, 10)]
            [TestCase(0, 3, 10)]
            [TestCase(3, 3, 10)]
            [TestCase(5, 3, 10)]
            [TestCase(6, 3, 10)]
            [TestCase(0, 10, 10)]
            [TestCase(3, 10, 10)]
            [TestCase(5, 10, 10)]
            [TestCase(6, 10, 10)]
            public void n秒タイムラインを進めたのちに指定した間隔expected回分をx回分に分けて進めたときにexpected回Tickイベントが発火される(int n, int x, int expected)
            {
                var specificInterval = 3.0;

                var tl = new Timeline(this.origin);
                tl.WaitForTime(TimeSpan.FromSeconds(n));
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(specificInterval));
                var count = 0;
                timer.Tick += (sender, args) => { count++; };
                var shouldPassingTime = specificInterval * expected;
                var onePassingTime = shouldPassingTime / x;
                for (int i = 0; i < x; i++)
                    tl.WaitForTime(TimeSpan.FromSeconds(onePassingTime));

                Assert.That(count, Is.EqualTo(expected));
            }
        }

        public class タイマー起動時にTickするタイマーのテスト
        {
            readonly DateTime origin = DateTime.Parse("2014/01/01").ToUniversalTime();

            [Test]
            public void 指定した間隔分の時間を進めなくても初回のTickイベントが発火される()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3), InitialTick.Enabled);
                var isFired = false;
                timer.Tick += (sender, args) => { isFired = true; };
                tl.WaitForTime(TimeSpan.FromSeconds(1));

                Assert.That(isFired, Is.True);
            }

            [Test]
            public void 指定した間隔の3回分時間のかかる処理を実行したとき4回実行される()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3), InitialTick.Enabled);
                var wait = tl.CreateWaiter(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromTicks(1));
                var count = 0;
                timer.Tick += (sender, args) => { ++count; wait(); };
                tl.WaitForTime(TimeSpan.FromSeconds(3));

                Assert.That(count, Is.EqualTo(4));
                Assert.That(tl.UtcNow, Is.EqualTo(this.origin + TimeSpan.FromSeconds(3)));
            }

            [Test]
            public void 指定した間隔で時間のかかる処理を実行してもメインでその分待っていれば余計に実行されない()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3), InitialTick.Enabled);
                var wait = tl.CreateWaiter(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero);
                var count = 0;
                timer.Tick += (sender, args) => { var start = tl.UtcNow; ++count; wait(); var end = tl.UtcNow; Console.WriteLine(); };
                tl.WaitForTime(TimeSpan.FromSeconds(14));

                // 余計に実行されない確認
                Assert.That(count, Is.EqualTo(14 / 3 + 1 /*5*/));
                // 余計に待たない確認
                Assert.That(tl.UtcNow, Is.EqualTo(this.origin + TimeSpan.FromSeconds(14)));
            }

            [Test]
            public void 指定した間隔2回分の時間が進めた時に3回Tickイベントが発火される()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3), InitialTick.Enabled);
                var count = 0;
                timer.Tick += (sender, args) => { count++; };
                tl.WaitForTime(TimeSpan.FromSeconds(6));

                Assert.That(count, Is.EqualTo(3));
            }

            [Test]
            public void 指定した間隔を2回に分けて進めた時に2回目のTickイベントが発火される()
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3), InitialTick.Enabled);
                var count = 0;
                timer.Tick += (sender, args) => { count++; };
                tl.WaitForTime(TimeSpan.FromSeconds(1));
                tl.WaitForTime(TimeSpan.FromSeconds(2));

                Assert.That(count, Is.EqualTo(2));
                Assert.That(tl.UtcNow, Is.EqualTo(this.origin + TimeSpan.FromSeconds(3)));
            }

            [TestCase(1, 8)]
            [TestCase(4, 5)]
            public void 指定した間隔3回分を2回に分けて進めたときに4回Tickイベントが発火される(int firstSpan, int secondSpan)
            {
                var tl = new Timeline(this.origin);
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(3), InitialTick.Enabled);
                var count = 0;
                timer.Tick += (sender, args) => { count++; };
                tl.WaitForTime(TimeSpan.FromSeconds(firstSpan));
                tl.WaitForTime(TimeSpan.FromSeconds(secondSpan));

                Assert.That(count, Is.EqualTo(4));
            }

            [TestCase(0, 1, 2)]
            [TestCase(3, 1, 2)]
            [TestCase(5, 1, 2)]
            [TestCase(6, 1, 2)]
            [TestCase(0, 3, 2)]
            [TestCase(3, 3, 2)]
            [TestCase(5, 3, 2)]
            [TestCase(6, 3, 2)]
            [TestCase(0, 10, 2)]
            [TestCase(3, 10, 2)]
            [TestCase(5, 10, 2)]
            [TestCase(6, 10, 2)]
            [TestCase(0, 1, 3)]
            [TestCase(3, 1, 3)]
            [TestCase(5, 1, 3)]
            [TestCase(6, 1, 3)]
            [TestCase(0, 3, 3)]
            [TestCase(3, 3, 3)]
            [TestCase(5, 3, 3)]
            [TestCase(6, 3, 3)]
            [TestCase(0, 10, 3)]
            [TestCase(3, 10, 3)]
            [TestCase(5, 10, 3)]
            [TestCase(6, 10, 3)]
            [TestCase(0, 1, 11)]
            [TestCase(3, 1, 11)]
            [TestCase(5, 1, 11)]
            [TestCase(6, 1, 11)]
            [TestCase(0, 3, 11)]
            [TestCase(3, 3, 11)]
            [TestCase(5, 3, 11)]
            [TestCase(6, 3, 11)]
            [TestCase(0, 10, 11)]
            [TestCase(3, 10, 11)]
            [TestCase(5, 10, 11)]
            [TestCase(6, 10, 11)]
            public void n秒タイムラインを進めたのちに指定した間隔expected引く1回分をx回分に分けて進めたときにexpected回Tickイベントが発火される(int n, int x, int expected)
            {
                var specificInterval = 3.0;

                var tl = new Timeline(this.origin);
                tl.WaitForTime(TimeSpan.FromSeconds(n));
                var timer = tl.CreateTimer(TimeSpan.FromSeconds(specificInterval), InitialTick.Enabled);
                var count = 0;
                timer.Tick += (sender, args) => { count++; };
                var shouldPassingTime = specificInterval * (expected - 1);
                var onePassingTime = shouldPassingTime / x;
                for (int i = 0; i < x; i++)
                    tl.WaitForTime(TimeSpan.FromSeconds(onePassingTime));

                Assert.That(count, Is.EqualTo(expected));
            }
        }
    }
}
