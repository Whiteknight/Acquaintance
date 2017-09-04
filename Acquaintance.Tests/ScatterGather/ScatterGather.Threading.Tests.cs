using Acquaintance.RequestResponse;
using FluentAssertions;
using NUnit.Framework;
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

        private class TestRequestWithResponse : IRequestWithResponse<TestResponse>
        {
            public string Text { get; set; }
        }

        [Test]
        public void ParticipateScatterGather_WorkerThread()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                NumberOfWorkers = 1
            });

            try
            {
                target.Participate<TestRequestWithResponse, TestResponse>(l => l
                    .WithTopic("Test")
                    .Invoke(req => new TestResponse { Text = req.Text + "Responded" + Thread.CurrentThread.ManagedThreadId })
                    .OnWorker());
                var response = target.Scatter<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });

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
                var token = target.Participate<TestRequestWithResponse, TestResponse>(builder => builder
                    .WithTopic("Test")
                    .Invoke(e => new TestResponse { Text = Thread.CurrentThread.ManagedThreadId.ToString() })
                    .OnDedicatedWorker());
                var results = target.Scatter<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Test" });

                results.GetNextResponse().Value.Text.Should().NotBeNullOrEmpty();
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
                var token = target.Participate<TestRequestWithResponse, TestResponse>(builder => builder
                    .WithTopic("Test")
                    .Invoke(e => new TestResponse { Text = Thread.CurrentThread.ManagedThreadId.ToString() })
                    .OnThreadPool());
                var results = target.Scatter<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Test" });

                results.GetNextResponse().Value.Text.Should().NotBeNullOrEmpty();
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
            int threadId = target.WorkerPool.StartDedicatedWorker().ThreadId;
            try
            {
                var token = target.Participate<TestRequestWithResponse, TestResponse>(builder => builder
                    .WithTopic("Test")
                    .Invoke(e => new TestResponse { Text = Thread.CurrentThread.ManagedThreadId.ToString() })
                    .OnThread(threadId));
                var results = target.Scatter<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Test" });

                results.GetNextResponse().Value.Text.Should().NotBeNullOrEmpty();
                token.Dispose();
            }
            finally
            {
                target.Dispose();
            }
        }
    }
}