using Acquaintance.RequestResponse;
using Acquaintance.ScatterGather;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGatherTests
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
        public void ParticipateScatterGather()
        {
            var target = new MessageBus();
            target.Participate<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = target.Scatter<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });
            response.Should().NotBeNull();
            var responses = response.GatherResponses();
            responses.Should().HaveCount(1);
            responses[0].Response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ParticipateScatterGather_ImmediateCounts()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Invoke(req => req + 5));
            var response = target.Scatter<int, int>(10);
            response.TotalParticipants.Should().Be(1);
            var responses = response.GatherResponses();
            response.CompletedParticipants.Should().Be(1);
            responses.Should().HaveCount(1);
            responses[0].Response.Should().Be(15);
        }

        [Test]
        public void ParticipateScatterGather_WeakReference()
        {
            var target = new MessageBus();
            target.Participate<TestRequestWithResponse, TestResponse>(l => l
                .WithTopic("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }, true));
            var response = target.Scatter<TestRequestWithResponse, TestResponse>("Test", new TestRequestWithResponse { Text = "Request" });
            response.Should().NotBeNull();
            var responses = response.GatherResponses();
            responses.Should().HaveCount(1);
            responses[0].Response.Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ParticipateScatterGather_OnDefaultChannel()
        {
            var target = new MessageBus();
            target.Participate<TestRequestWithResponse, TestResponse>(l => l
                .WithDefaultTopic()
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));

            var response = target.Scatter<TestRequestWithResponse, TestResponse>(new TestRequestWithResponse { Text = "Request" }).GatherResponses(1);
            response.Should().HaveCount(1);
            response[0].Response.Text.Should().Be("RequestResponded");
        }

        private class GenericRequest<T> { }
        private class GenericResponse<T> { }

        [Test]
        public void Participate_Generics()
        {
            var target = new MessageBus();

            Action act = () =>
            {
                target.Participate<GenericRequest<string>, GenericResponse<string>>(l => l
                    .WithTopic("Test")
                    .Invoke(req => new GenericResponse<string>()));
                target.Participate<GenericRequest<int>, GenericResponse<int>>(l => l
                    .WithTopic("Test")
                    .Invoke(req => new GenericResponse<int>()));
            };
            act.ShouldNotThrow();
        }

        [Test]
        public void Participate_SecondListener()
        {
            var target = new MessageBus();
            var listener1 = ImmediateParticipant<TestRequestWithResponse, TestResponse>.Create(req => null);
            var listener2 = ImmediateParticipant<TestRequestWithResponse, TestResponse>.Create(req => null);
            target.Participate("test", listener1);
            Action act = () => target.Participate("test", listener2);
            act.ShouldNotThrow<Exception>();
        }

        [Test]
        public void ParticipateScatterGather_Wildcards()
        {
            var target = new MessageBus(new MessageBusCreateParameters
            {
                AllowWildcards = true
            });
            target.Participate<int, int>(l => l.WithTopic("Test.A").Invoke(req => 1));
            target.Participate<int, int>(l => l.WithTopic("Test.B").Invoke(req => 2));
            target.Participate<int, int>(l => l.WithTopic("Test.C").Invoke(req => 3));
            var response = target.Scatter<int, int>("Test.*", 0).GatherResponses(3).Select(r => r.Response).ToArray();
            response.Should().BeEquivalentTo(1, 2, 3);
        }

        [Test]
        public void ParticipateScatterGather_ThrowAnyExceptions()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Invoke((Func<int, int>)(req => { throw new Exception("expected"); }))
                .OnWorkerThread());

            var response = target.Scatter<int, int>(1).GetNextResponse();
            Action act = () => response.ThrowExceptionIfPresent();
            act.ShouldThrow<Exception>();
        }

        [Test]
        public void ParticipateScatterGather_ThrowAnyExceptions_Immediate()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Invoke((Func<int, int>)(req => { throw new Exception("expected"); }))
                .Immediate());

            var response = target.Scatter<int, int>(1).GetNextResponse();
            Action act = () => response.ThrowExceptionIfPresent();
            act.ShouldThrow<Exception>();
        }

        [Test]
        public void ParticipateScatterGather_ModifyParticipant()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Invoke(req => 1)
                .Immediate()
                .ModifyParticipant(p => new MaxRequestsParticipant<int, int>(p, 2)));

            target.Scatter<int, int>(1).GetNextResponse().Should().NotBeNull();
            target.Scatter<int, int>(2).GetNextResponse().Should().NotBeNull();
            target.Scatter<int, int>(3).GetNextResponse().Should().BeNull();
            target.Scatter<int, int>(4).GetNextResponse().Should().BeNull();
        }

        [Test]
        public void ParticipateScatterGather_TotalParticipants()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithTopic("Test")
                .Invoke(req =>
                {
                    Thread.Sleep(1000);
                    return req + 10;
                }));
            var response = target.Scatter<int, int>("Test", 5);
            response.TotalParticipants.Should().Be(1);
            var values = response.GatherResponses(1).Select(r => r.Response).ToArray();
            response.CompletedParticipants.Should().Be(1);
            values.Length.Should().Be(1);
        }
    }
}
