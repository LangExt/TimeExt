﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeExt.VirtualImplementations;

namespace TimeExt.Tests.VirtualImplementations
{
    [TestFixture]
    public class TimelineTest
    {
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

        readonly DateTime origin = DateTime.UtcNow;

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
            timer.Tick += (sender, args) => { ++count; wait(); };
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
}