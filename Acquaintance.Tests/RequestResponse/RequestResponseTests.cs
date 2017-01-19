using Acquaintance.RequestResponse;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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
        public void ListenRequestAndResponse_WeakReference()
        {
            var target = new MessageBus();
            target.Listen<TestRequest, TestResponse>(l => l
                .WithChannelName("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }, true));

            var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ListenRequestAndResponse_Unsubscribe()
        {
            var target = new MessageBus();
            var token = target.Listen<int, int>(l => l
                .OnDefaultChannel()
                .Invoke(req => 1));

            target.Request<int, int>(1).Should().Be(1);
            token.Dispose();
            target.Request<int, int>(1).Should().Be(0);
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
        public void ListenRequestAndResponse_ModifyListener()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithChannelName("Test")
                .Invoke(e => e + 5)
                .Immediate()
                .ModifyListener(x => new MaxRequestsListener<int, int>(x, 3)));
            var responses = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                var response = target.Request<int, int>("Test", i);
                responses.Add(response);
            }

            responses.Should().BeEquivalentTo(5, 6, 7, 0, 0);
        }
    }
}
