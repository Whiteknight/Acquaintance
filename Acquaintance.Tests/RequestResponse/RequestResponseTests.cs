using Acquaintance.RequestResponse;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Acquaintance.Routing;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponseTests
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
        public void ListenRequestAndResponse()
        {
            var target = new MessageBus();
            target.Listen<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = target.RequestWait<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_InvokeEnvelope()
        {
            var target = new MessageBus();
            target.Listen<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .InvokeEnvelope(req => new TestResponse { Text = req.Payload.Text + "Responded" }));
            var response = target.RequestWait<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_WeakReference()
        {
            var target = new MessageBus();
            target.Listen<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }, true));

            var response = target.RequestWait<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_EnvelopeWeakReference()
        {
            var target = new MessageBus();
            target.Listen<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .InvokeEnvelope(req => new TestResponse { Text = req.Payload.Text + "Responded" }, true));
            var response = target.RequestWait<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_Unsubscribe()
        {
            var target = new MessageBus();
            var token = target.Listen<int, int>(l => l
                .WithDefaultTopic()
                .Invoke(req => 1));

            target.RequestWait<int, int>(1).Should().Be(1);
            token.Dispose();
            target.RequestWait<int, int>(1).Should().Be(0);
        }

        [Test]
        public void ListenRequestAndResponse_Object()
        {
            var target = new MessageBus();
            target.Listen<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = target.Request("Test", typeof(TestRequestWithResponse), new TestRequestWithResponse { Text = "Request" });
            response.Should().NotBeNull();
            response.Should().BeOfType(typeof(TestResponse));
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
                    .WithTopic("Test")
                    .Invoke(req => new GenericResponse<string>())
                    .Immediate());
                target.Listen<GenericRequest<int>, GenericResponse<int>>(l => l
                    .WithTopic("Test")
                    .Invoke(req => new GenericResponse<int>())
                    .Immediate());
            };
            act.ShouldNotThrow();
        }

        [Test]
        public void Listen_SecondListener()
        {
            var target = new MessageBus();
            var listener1 = ImmediateListener<TestRequestWithResponse, TestResponse>.Create(req => null);
            var listener2 = ImmediateListener<TestRequestWithResponse, TestResponse>.Create(req => null);
            target.Listen("test", listener1);
            Action act = () => target.Listen("test", listener2);
            act.ShouldThrow<Exception>();
        }

        [Test]
        public void ListenRequestAndResponse_Wildcards()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                AllowWildcards = true
            });
            target.Listen<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test.A")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = target.RequestWait<TestRequestWithResponse, TestResponse>("Test.*", new TestRequestWithResponse { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_Wildcards_MultipleListenersBreadthFirst()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                AllowWildcards = true
            });
            target.Listen<int, int>(l => l
                .WithTopic("Test.A")
                .Invoke(req =>req + 10));
            target.Listen<int, int>(l => l
                .WithTopic("Test.A.B")
                .Invoke(req => req + 100));
            var response = target.RequestWait<int, int>("Test.*", 5);
            response.Should().Be(15);
        }

        [Test]
        public void ListenRequestAndResponse_Wildcards_Unsubscribe()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                AllowWildcards = true
            });
            var token = target.Listen<int, int>(l => l
                .WithTopic("Test.A")
                .Invoke(req => req + 10));
            var response = target.RequestWait<int, int>("Test.*", 5);
            response.Should().Be(15);

            token.Dispose();
            response = target.RequestWait<int, int>("Test.*", 5);
            response.Should().Be(0);
        }

        [Test]
        public void ListenRequestAndResponse_ModifyListener()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithTopic("Test")
                .Invoke(e => e + 5)
                .Immediate()
                .ModifyListener(x => new MaxRequestsListener<int, int>(x, 3)));
            var responses = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                var response = target.RequestWait<int, int>("Test", i);
                responses.Add(response);
            }

            responses.Should().BeEquivalentTo(5, 6, 7, 0, 0);
        }

        [Test]
        public void RequestWithNoListeners()
        {
            var target = new MessageBus();
            var result = target.Request<int, int>(5);
            result.Should().NotBeNull();
            result.GetResponse().Should().Be(default(int));
        }

        private class ReturnsNullRouteRule : IRequestRouteRule<int>
        {
            public string GetRoute(string topic, Envelope<int> envelope)
            {
                return null;
            }
        }

        [Test]
        public void RequestRouterReturnsNull()
        {
            var target = new MessageBus();
            target.RequestRouter.AddRule<int, int>("", new ReturnsNullRouteRule());
            var response = target.Request<int, int>("", 5);
        }

        [Test]
        public void ListenRequestAndResponse_Error()
        {
            var target = new MessageBus();
            target.Listen<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .Invoke(req => { throw new Exception("Expected"); }));
            var response = target.Request<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });
            response.WaitForResponse();
            response.IsComplete().Should().BeTrue();
            response.GetErrorInformation().Should().NotBeNull();
        }

        [Test]
        public void ListenRequestAndResponse_ThrowError()
        {
            var target = new MessageBus();
            target.Listen<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .Invoke(req => { throw new Exception("Expected"); }));
            var response = target.Request<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });
            response.WaitForResponse();

            Action act = () => response.ThrowExceptionIfError();
            act.ShouldThrow<Exception>();
        }

        private class ShouldStopListeningListener : IListener<int, int>
        {
            public bool CanHandle(Envelope<int> request)
            {
                return true;
            }

            public void Request(Envelope<int> envelope, Request<int> request)
            {
                request.SetError(new Exception("Should not get here"));
            }

            public bool ShouldStopListening => true;
            public Guid Id { get; set; }
        }

        [Test]
        public void Listen_ShouldStopListening()
        {
            var target = new MessageBus();
            target.Listen("", new ShouldStopListeningListener());
            var response = target.Request<int, int>(5);
            response.IsComplete().Should().BeTrue();
            response.HasResponse().Should().BeFalse();
            response.GetErrorInformation().Should().BeNull();
            response.GetResponse().Should().Be(0);
        }

        [Test]
        public void Request_GetResponseAsync()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithDefaultTopic()
                .Invoke(req => req + 10));
            var response = target.Request<int, int>(5).GetResponseAsync().Result;
            response.Should().Be(15);
        }
    }
}
