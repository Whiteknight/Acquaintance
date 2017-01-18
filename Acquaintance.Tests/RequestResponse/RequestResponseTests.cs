using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Acquaintance.RequestResponse;
using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
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
            target.Listen<TestRequest, TestResponse>(l => l
                .WithChannelName("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_Object()
        {
            var target = new MessageBus();
            target.Listen<TestRequest, TestResponse>(l => l
                .WithChannelName("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = target.Request("Test", typeof(TestRequest), new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(TestResponse));
        }

        [Test]
        public void ListenRequestAndResponse_WorkerThread()
        {
            var target = new MessageBus(threadPool: new MessagingWorkerThreadPool(1));
            try
            {
                target.Listen<TestRequest, TestResponse>(l => l
                    .WithChannelName("Test")
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
        public void RequestAndResponse_Eavesdrop()
        {
            var target = new MessageBus();
            string eavesdropped = null;
            target.Listen<TestRequest, TestResponse>(l => l
                .WithChannelName("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" })
                .Immediate());
            target.Eavesdrop<TestRequest, TestResponse>(s => s
                .WithChannelName("Test")
                .Invoke(conv => eavesdropped = conv.Responses.Select(r => r.Text).FirstOrDefault())
                .Immediate());
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
                target.Listen<GenericRequest<string>, GenericResponse<string>>(l => l
                    .WithChannelName("Test")
                    .Invoke(req => new GenericResponse<string>())
                    .Immediate());
                target.Listen<GenericRequest<int>, GenericResponse<int>>(l => l
                    .WithChannelName("Test")
                    .Invoke(req => new GenericResponse<int>())
                    .Immediate());
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
            target.Listen<TestRequest, TestResponse>(l => l
                .WithChannelName("Test.A")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = target.Request<TestRequest, TestResponse>("Test.*", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_MaxRequests()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithChannelName("Test")
                .Invoke(e => e + 5)
                .Immediate()
                .MaximumRequests(3));
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
            target.Listen<string, int>(l => l
                .WithChannelName("test string")
                .Invoke(r =>
                {
                    request = r;
                    return 5;
                }));
            target.Listen<int, int>(l => l
                .WithChannelName("test int")
                .TransformRequestTo("test string", r => r.ToString() + "A"));
            var response = target.Request<int, int>("test int", 4);

            response.Should().Be(5);
            request.Should().Be("4A");
        }

        [Test]
        public void ListenTransformResponse_Test()
        {
            var target = new MessageBus();
            target.Listen<int, string>(l => l
                .WithChannelName("test string")
                .Invoke(r => "5"));
            target.Listen<int, int>(l => l
                .WithChannelName("test int")
                .TransformResponseFrom<string>("test string", int.Parse));
            var response = target.Request<int, int>("test int", 4);

            response.Should().Be(5);
        }

        [Test]
        public void Listen_ListenerBuilder_CircuitBreaker()
        {
            int requests = 0;
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .OnDefaultChannel()
                .Invoke(i =>
                {
                    requests++;
                    if (i == 1)
                        throw new Exception("test");
                    return i * 10;
                })
                .OnWorkerThread()
                .WithCircuitBreaker(1, 500));

            // First request fails
            var response = target.Request<int, int>(1);
            response.Should().Be(0);
            requests.Should().Be(1);

            // Second request trips the circuit breaker and returns no result
            response = target.Request<int, int>(2);
            response.Should().Be(0);
            requests.Should().Be(1);

            // Third request passes, because of a delay so the circuit breaker can reset itself
            Thread.Sleep(500);
            response = target.Request<int, int>(3);
            response.Should().Be(30);
            requests.Should().Be(2);
        }
    }
}
