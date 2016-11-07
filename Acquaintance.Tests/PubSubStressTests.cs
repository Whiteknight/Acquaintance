using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;
using System.Threading;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class PubSubStressTests
    {
        private class TestPubSubEvent
        {
        }

        [Test]
        public void WorkerThread_Stress()
        {
            const int numEvents = 100000;
            var target = new MessageBus();
            target.StartWorkers(4);
            int count = 0;
            var resetEvent = new ManualResetEvent(false);
            target.Subscribe<TestPubSubEvent>("Test", e =>
            {
                int c = Interlocked.Increment(ref count);
                if (c >= numEvents)
                    resetEvent.Set();
            }, null, new SubscribeOptions { DispatchType = DispatchThreadType.AnyWorkerThread });
            for (int i = 0; i < numEvents; i++)
                target.Publish("Test", new TestPubSubEvent());

            resetEvent.WaitOne(10000).Should().Be(true);
        }

        [Test]
        public void WorkerThread_Stress_Wildcards()
        {
            const int numEvents = 100000;
            var target = new MessageBus(allowWildcards: true);
            target.StartWorkers(4);
            int count = 0;
            var resetEvent = new ManualResetEvent(false);
            target.Subscribe<TestPubSubEvent>("Test.XYZ", e =>
            {
                int c = Interlocked.Increment(ref count);
                if (c >= numEvents)
                    resetEvent.Set();
            }, null, new SubscribeOptions { DispatchType = DispatchThreadType.AnyWorkerThread });
            for (int i = 0; i < numEvents; i++)
                target.Publish("Test.*", new TestPubSubEvent());

            resetEvent.WaitOne(10000).Should().Be(true);
        }
    }
}