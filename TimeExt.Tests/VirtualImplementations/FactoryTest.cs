using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TimeExt.VirtualImplementations;

namespace TimeExt.Tests.VirtualImplementations
{
    [TestFixture]
    public class FactoryTest
    {
        Factory factory;

        [SetUp]
        public void SetUp()
        {
            this.factory = new Factory(DateTime.UtcNow);
        }

        [Test]
        public void Timelineが生成できる()
        {
            Assert.That(factory.CreateTimeline(), Is.TypeOf<Timeline>());
        }

        [Test]
        public void TaskJoinが生成できる()
        {
            Assert.That(factory.CreateTaskJoin(), Is.TypeOf<TaskJoin>());
        }

    }
}
