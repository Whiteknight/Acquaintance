using Acquaintance.ScatterGather;
using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class ScatterGatherTests
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
        public void ParticipateScatterGather()
        {
            var target = new MessageBus();
            target.Participate<TestRequest, TestResponse>(l => l
                .WithChannelName("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Responses.Should().HaveCount(1);
            response.Responses[0].Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ParticipateScatterGather_WorkerThread()
        {
            var target = new MessageBus(threadPool: new MessagingWorkerThreadPool(1));

            try
            {
                target.Participate<TestRequest, TestResponse>(l => l
                    .WithChannelName("Test")
                    .Invoke(req => new TestResponse { Text = req.Text + "Responded" + Thread.CurrentThread.ManagedThreadId })
                    .WithTimeout(2000)
                    .OnWorkerThread());
                var response = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });

                response.Should().NotBeNull();
            }
            finally
            {
                target.Dispose();
            }
        }

        [Test]
        public void ParticipateScatterGather_MapReduce()
        {
            var target = new MessageBus();

            target.Participate<TestRequest, TestResponse>(l => l.Invoke(r => new TestResponse { Text = r.Text + "A" }));
            target.Participate<TestRequest, TestResponse>(l => l.Invoke(r => new TestResponse { Text = r.Text + "B" }));
            target.Participate<TestRequest, TestResponse>(l => l.Invoke(r => new TestResponse { Text = r.Text + "C" }));
            target.Participate<TestRequest, TestResponse>(l => l.Invoke(r => new TestResponse { Text = r.Text + "D" }));
            target.Participate<TestRequest, TestResponse>(l => l.Invoke(r => new TestResponse { Text = r.Text + "E" }));

            var response = target.Scatter<TestRequest, TestResponse>(new TestRequest { Text = "x" });

            var reduced = string.Join("", response.Responses.Select(r => r.Text).OrderBy(s => s));
            reduced.Should().Be("xAxBxCxDxE");
        }

        [Test]
        public void ParticipateScatterGather_Eavesdrop()
        {
            var target = new MessageBus();
            string eavesdropped = null;

            target.Participate<TestRequest, TestResponse>(l => l
                .WithChannelName("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            target.Eavesdrop<TestRequest, TestResponse>(s => s
                .WithChannelName("Test")
                .Invoke(conv => eavesdropped = conv.Responses.Select(r => r.Text).FirstOrDefault())
                .Immediate());
            var response = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            eavesdropped.Should().Be("RequestResponded");
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
                .WithChannelName("Test")
                .Invoke(req => new GenericResponse<string>()));
                target.Participate<GenericRequest<int>, GenericResponse<int>>(l => l
                .WithChannelName("Test")
                .Invoke(req => new GenericResponse<int>()));
            };
            act.ShouldNotThrow();
        }

        [Test]
        public void Participate_SecondListener()
        {
            var target = new MessageBus();
            var listener1 = ImmediateParticipant<TestRequest, TestResponse>.Create(req => null);
            var listener2 = ImmediateParticipant<TestRequest, TestResponse>.Create(req => null);
            target.Participate("test", listener1);
            Action act = () => target.Participate("test", listener2);
            act.ShouldNotThrow<Exception>();
        }

        [Test]
        public void ParticipateScatterGather_Wildcards()
        {
            var target = new MessageBus(dispatcherFactory: new TrieDispatchStrategyFactory());
            target.Participate<int, int>(l => l.WithChannelName("Test.A").Invoke(req => 1));
            target.Participate<int, int>(l => l.WithChannelName("Test.B").Invoke(req => 2));
            target.Participate<int, int>(l => l.WithChannelName("Test.C").Invoke(req => 3));
            var response = target.Scatter<int, int>("Test.*", 0).Responses;
            response.Should().BeEquivalentTo(1, 2, 3);
        }

        [Test]
        public void Participate_ParticipantBuilder_TransformRequestTo()
        {
            var target = new MessageBus();
            target.Participate<string, int>(l => l.Invoke(s => s.Length));
            target.Participate<string, int>(l => l.Invoke(s => s.Length * 2));

            target.Participate<int, int>(l => l.TransformRequestTo<string>(null, i => i.ToString()));

            var results = target.Scatter<int, int>(100);
            results.Should().Contain(3);
            results.Should().Contain(6);
        }

        [Test]
        public void Participate_ParticipantBuilder_TransformResponseFrom()
        {
            var target = new MessageBus();
            target.Participate<int, string>(l => l.Invoke(s => s.ToString()));
            target.Participate<int, string>(l => l.Invoke(s => s.ToString() + "A"));

            target.Participate<int, int>(l => l.TransformResponseFrom<string>(null, i => i.Length));

            var results = target.Scatter<int, int>(100);
            results.Should().Contain(3);
            results.Should().Contain(4);
        }
    }
}
