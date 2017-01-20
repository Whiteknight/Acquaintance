using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using System.Threading;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_Threading_Tests
    {
        private class TestResponse
        {
            public string Text { get; set; }
        }

        private class TestRequest : IRequest<TestResponse>
        {
            public string Text { get; set; }
        }

        [Test]
        public void ParticipateScatterGather_WorkerThread()
        {
            var target = new MessageBus(threadPool: new MessagingWorkerThreadPool(1));

            try
            {
                target.Participate<TestRequest, TestResponse>(l => l
                    .WithChannelName("Test")
                    .Invoke(req => new TestResponse { Text = req.Text + "Responded" + Thread.CurrentThread.ManagedThreadId })
                    .OnWorkerThread()
                    .WithTimeout(2000));
                var response = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });

                response.Should().NotBeNull();
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void ParticipateScatterGather_OnDedicatedThread()
        {
            var target = new MessageBus();
            try
            {
                var token = target.Participate<TestRequest, TestResponse>(builder => builder
                    .WithChannelName("Test")
                    .Invoke(e => new TestResponse { Text = Thread.CurrentThread.ManagedThreadId.ToString() })
                    .OnDedicatedThread());
                var results = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Test" });

                results.First().Text.Should().NotBeNullOrEmpty();
                token.Dispose();
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void ParticipateScatterGather_OnThreadPool()
        {
            var target = new MessageBus();
            try
            {
                var token = target.Participate<TestRequest, TestResponse>(builder => builder
                    .WithChannelName("Test")
                    .Invoke(e => new TestResponse { Text = Thread.CurrentThread.ManagedThreadId.ToString() })
                    .OnThreadPool());
                var results = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Test" });

                results.First().Text.Should().NotBeNullOrEmpty();
                token.Dispose();
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void ParticipateScatterGather_OnThread()
        {
            var target = new MessageBus();
            int threadId = target.ThreadPool.StartDedicatedWorker();
            try
            {
                var token = target.Participate<TestRequest, TestResponse>(builder => builder
                    .WithChannelName("Test")
                    .Invoke(e => new TestResponse { Text = Thread.CurrentThread.ManagedThreadId.ToString() })
                    .OnThread(threadId));
                var results = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Test" });

                results.First().Text.Should().NotBeNullOrEmpty();
                token.Dispose();
            }
            finally
            {
                target.Dispose();
            }
        }
    }
}