using Acquaintance.RequestResponse;
using FluentAssertions;
using NUnit.Framework;
using System.Threading;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_Threading_Tests
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
        public void ListenRequestAndResponse_WorkerThread()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                NumberOfWorkers = 1
            });
            try
            {
                target.Listen<TestRequestWithResponse, TestResponse>(l => l
                    .WithTopic("Test")
                    .Invoke(req => new TestResponse { Text = req.Text + "Responded" + Thread.CurrentThread.ManagedThreadId })
                    .OnWorker());
                var response = target.Request<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });

                response.Should().NotBeNull();
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void ListenRequestAndResponse_OnDedicatedThread()
        {
            var target = new MessageBus();
            try
            {
                target.Listen<int, int>(l => l
                    .WithDefaultTopic()
                    .Invoke(req => req * 5)
                    .OnDedicatedWorker());
                var response = target.RequestWait<int, int>(1);

                response.Should().Be(5);
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void ListenRequestAndResponse_ThreadPool()
        {
            var target = new MessageBus();
            try
            {
                target.Listen<int, int>(l => l
                    .WithDefaultTopic()
                    .Invoke(req => req * 5)
                    .OnThreadPool());
                var response = target.RequestWait<int, int>(1);

                response.Should().Be(5);
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void ListenRequestAndResponse_OnThread()
        {
            var target = new MessageBus();
            int threadId = target.WorkerPool.StartDedicatedWorker().ThreadId;
            try
            {
                target.Listen<int, int>(l => l
                    .WithDefaultTopic()
                    .Invoke(req => req * 5)
                    .OnThread(threadId));
                var response = target.RequestWait<int, int>(1);

                response.Should().Be(5);
            }
            finally
            {
                target.Dispose();
            }
        }
    }
}