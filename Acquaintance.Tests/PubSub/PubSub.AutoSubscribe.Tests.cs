using System.Threading;
using Acquaintance.PubSub;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.PubSub
{
    [TestFixture]
    public class PubSub_AutoSubscribe_Tests
    {
        public class TestClass1
        {
            private readonly ManualResetEvent _wait;

            public TestClass1(ManualResetEvent wait)
            {
                _wait = wait;
            }

            public string Value { get; private set; }

            [Subscription(typeof(string))]
            public void StringSubscriber(string s)
            {
                Value = s;
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_Test()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass1(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("test");
                wait.WaitOne(2000).Should().Be(true);
                obj.Value.Should().Be("test");
            }
        }

        public class TestClass2
        {
            private readonly ManualResetEvent _wait;

            public TestClass2(ManualResetEvent wait)
            {
                _wait = wait;
            }

            public string Value { get; private set; }

            [Subscription(typeof(string), new[] { "B" })]
            public void StringSubscriber(string s)
            {
                Value = s;
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_Topics()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass2(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("A", "test1");
                target.Publish("B", "test2");
                target.Publish("C", "test3");
                wait.WaitOne(2000).Should().Be(true);
                obj.Value.Should().Be("test2");
            }
        }
    }
}
