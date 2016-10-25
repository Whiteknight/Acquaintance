using System.Threading;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class MessageBusTests
    {
        private class TestPubSubEvent
        {
            public string Text { get; set; }

            public TestPubSubEvent(string text)
            {
                Text = text;
            }
        }

        [Test]
        public void SubscribeAndPublish()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text);
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");
        }

        [Test]
        public void SubscribeAndPublish_Filtered()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text, e => e.Text == "Test2");
            target.Publish("Test", new TestPubSubEvent("Test1"));
            text.Should().BeNull();
            target.Publish("Test", new TestPubSubEvent("Test2"));
            text.Should().Be("Test2");

        }

        [Test]
        public void SubscribeAndPublish_WorkerThread()
        {
            var target = new MessageBus();
            target.StartWorkers(1);
            try
            {
                string text = null;
                var testThread = Thread.CurrentThread.ManagedThreadId;
                target.Subscribe<TestPubSubEvent>("Test", e => text = e.Text + Thread.CurrentThread.ManagedThreadId, new SubscribeOptions
                {
                    DispatchType = DispatchThreadType.AnyWorkerThread,
                });
                target.Publish("Test", new TestPubSubEvent("Test"));
                // TODO: Find a better way to test without hard-coded timeout.
                Thread.Sleep(2000);

                text.Should().NotBeNull();
                text.Should().NotBe("Test");
                text.Should().NotBe("Test" + testThread);
            }
            finally
            {
                target.Dispose();
            }
        }

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
