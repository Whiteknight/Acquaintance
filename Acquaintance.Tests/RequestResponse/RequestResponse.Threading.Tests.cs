using Acquaintance.RequestResponse;
using Acquaintance.Threading;
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

        private class TestRequest : IRequest<TestResponse>
        {
            public string Text { get; set; }
        }

        [Test]
        public void ListenRequestAndResponse_WorkerThread()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                ThreadPool = new MessagingWorkerThreadPool(null, 1)
            });
            try
            {
                target.Listen<TestRequest, TestResponse>(l => l
                    .WithTopic("Test")
                    .Invoke(req => new TestResponse { Text = req.Text + "Responded" + Thread.CurrentThread.ManagedThreadId })
                    .OnWorkerThread()
                    .WithTimeout(2000));
                var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });

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
                    .OnDedicatedThread()
                    .WithTimeout(2000));
                var response = target.Request<int, int>(1);

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
                    .OnThreadPool()
                    .WithTimeout(2000));
                var response = target.Request<int, int>(1);

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
            int threadId = target.ThreadPool.StartDedicatedWorker().ThreadId;
            try
            {
                target.Listen<int, int>(l => l
                    .WithDefaultTopic()
                    .Invoke(req => req * 5)
                    .OnThread(threadId)
                    .WithTimeout(2000));
                var response = target.Request<int, int>(1);

                response.Should().Be(5);
            }
            finally
            {
                target.Dispose();
            }
        }
    }
}