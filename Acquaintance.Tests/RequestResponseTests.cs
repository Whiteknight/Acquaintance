using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        public void ListenRequestAndResponse()
        {
            var target = new MessageBus();
            target.Listen<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" });
            var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_Object()
        {
            var target = new MessageBus();
            target.Listen<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" });
            var response = target.Request("Test", typeof(TestRequest), new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(TestResponse));
        }

        [Test]
        public void ListenRequestAndResponse_WorkerThread()
        {
            var target = new MessageBus();
            target.StartWorkers(1);
            try
            {
                target.Listen<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" + Thread.CurrentThread.ManagedThreadId }, null, new ListenOptions
                {
                    DispatchType = DispatchThreadType.AnyWorkerThread,
                    WaitTimeoutMs = 2000
                });
                var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });

                response.Should().NotBeNull();
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void RequestAndResponse_Eavesdrop()
        {
            var target = new MessageBus();
            string eavesdropped = null;
            target.Listen<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" });
            target.Eavesdrop<TestRequest, TestResponse>("Test", conv => eavesdropped = conv.Responses.Select(r => r.Text).FirstOrDefault(), null);
            var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            eavesdropped.Should().Be("RequestResponded");
        }

        private class GenericRequest<T> { }
        private class GenericResponse<T> { }

        [Test]
        public void ListenRequestAndResponse_Generics()
        {
            var target = new MessageBus();

            Action act = () =>
            {
                target.Listen<GenericRequest<string>, GenericResponse<string>>("Test", req => new GenericResponse<string>());
                target.Listen<GenericRequest<int>, GenericResponse<int>>("Test", req => new GenericResponse<int>());
            };
            act.ShouldNotThrow();
        }

        [Test]
        public void Listen_SecondListener()
        {
            var target = new MessageBus();
            var listener1 = ImmediateListener<TestRequest, TestResponse>.Create(req => null);
            var listener2 = ImmediateListener<TestRequest, TestResponse>.Create(req => null);
            target.Listen("test", listener1);
            Action act = () => target.Listen("test", listener2);
            act.ShouldThrow<Exception>();
        }

        [Test]
        public void ListenRequestAndResponse_Wildcards()
        {
            var target = new MessageBus(dispatcherFactory: new TrieDispatchStrategyFactory());
            target.Listen<TestRequest, TestResponse>("Test.A", req => new TestResponse { Text = req.Text + "Responded" });
            var response = target.Request<TestRequest, TestResponse>("Test.*", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_MaxRequests()
        {
            var target = new MessageBus();
            target.Listen<int, int>("Test", e => e + 5, options: new ListenOptions
            {
                MaxRequests = 3
            });
            var responses = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                var response = target.Request<int, int>("Test", i);
                responses.Add(response);
            }

            responses.Should().BeEquivalentTo(5, 6, 7, 0, 0);
        }

        [Test]
        public void ListenTransformRequest_Test()
        {
            var target = new MessageBus();
            string request = null;
            target.Listen<string, int>("test string", r =>
            {
                request = r;
                return 5;
            });
            target.ListenTransformRequest<int, string, int>("test int", r => r.ToString() + "A", null, "test string");
            var response = target.Request<int, int>("test int", 4);

            response.Should().Be(5);
            request.Should().Be("4A");
        }

        [Test]
        public void ListenTransformResponse_Test()
        {
            var target = new MessageBus();
            target.Listen<int, string>("test string", r => "5");
            target.ListenTransformResponse<int, string, int>("test int", int.Parse, null, "test string");
            var response = target.Request<int, int>("test int", 4);

            response.Should().Be(5);
        }
    }
}
