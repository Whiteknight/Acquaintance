using System.Threading;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class RequestResponseTests
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
        public void SubscribeRequestAndResponse()
        {
            var target = new MessageBus();
            target.Subscribe<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" });
            var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Responses.Should().HaveCount(1);
            response.Responses[0].Text.Should().Be("RequestResponded");
        }

        //[Test]
        //public void SubscribeRequestAndResponseObject()
        //{
        //    var target = new MessageBus();
        //    target.Subscribe<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" });
        //    var response = target.Request("Test", typeof(TestRequest), new TestRequest { Text = "Request" });
        //    response.Should().NotBeNull();
        //    response.Responses.Should().HaveCount(1);
        //    response.Responses[0].Should().BeOfType(typeof(TestResponse));
        //}

        [Test]
        public void SubscribeRequestAndResponse_WorkerThread()
        {
            var target = new MessageBus();
            target.StartWorkers(1);
            try
            {
                target.Subscribe<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" + Thread.CurrentThread.ManagedThreadId }, new SubscribeOptions
                {
                    DispatchType = DispatchThreadType.AnyWorkerThread,
                    WaitTimeoutMs = 2000
                });
                var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });

                response.Should().NotBeNull();
                response.Responses.Should().HaveCount(1);
            }
            finally
            {
                target.Dispose();
            }
        }
    }

}
