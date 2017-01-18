using System.Threading;
using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.PubSub
{
    [TestFixture]
    public partial class PubSubTests
    {
        private class TestPubSubStressEvent
        {
        }

        [Test]
        public void WorkerThread_Stress()
        {
            const int numEvents = 100000;
            var target = new MessageBus(threadPool: new MessagingWorkerThreadPool(4));
            int count = 0;
            var resetEvent = new ManualResetEvent(false);
            target.Subscribe<TestPubSubStressEvent>(s => s
                .WithChannelName("Test")
                .Invoke(e =>
                {
                    int c = Interlocked.Increment(ref count);
                    if (c >= numEvents)
                        resetEvent.Set();
                })
                .OnWorkerThread());
            for (int i = 0; i < numEvents; i++)
                target.Publish("Test", new TestPubSubStressEvent());

            resetEvent.WaitOne(10000).Should().Be(true);
        }

        [Test]
        public void WorkerThread_Stress_Wildcards()
        {
            const int numEvents = 100000;
            var target = new MessageBus(threadPool: new MessagingWorkerThreadPool(4), dispatcherFactory: new TrieDispatchStrategyFactory());
            int count = 0;
            var resetEvent = new ManualResetEvent(false);
            target.Subscribe<TestPubSubStressEvent>(s => s
                .WithChannelName("Test.XYZ")
                .Invoke(e =>
                {
                    int c = Interlocked.Increment(ref count);
                    if (c >= numEvents)
                        resetEvent.Set();
                })
                .OnWorkerThread());
            for (int i = 0; i < numEvents; i++)
                target.Publish("Test.*", new TestPubSubStressEvent());

            resetEvent.WaitOne(10000).Should().Be(true);
        }
    }
}