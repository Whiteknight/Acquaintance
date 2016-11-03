using Acquaintance.Threading;
using FluentAssertions;
using NUnit.Framework;
using System;
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
            target.Participate<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" });
            var response = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Responses.Should().HaveCount(1);
            response.Responses[0].Text.Should().Be("RequestResponded");
        }

        [Test]
        public void ParticipateScatterGather_WorkerThread()
        {
            var target = new MessageBus();
            target.StartWorkers(1);
            try
            {
                target.Participate<TestRequest, TestResponse>("Test", req => new TestResponse { Text = req.Text + "Responded" + Thread.CurrentThread.ManagedThreadId }, null, new ListenOptions
                {
                    DispatchType = DispatchThreadType.AnyWorkerThread,
                    WaitTimeoutMs = 2000
                });
                var response = target.Scatter<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });

                response.Should().NotBeNull();
            }
            finally
            {
                target.Dispose();
            }
        }

        private class GenericRequest<T> { }
        private class GenericResponse<T> { }

        [Test]
        public void Participate_Generics()
        {
            var target = new MessageBus();

            Action act = () =>
            {
                target.Participate<GenericRequest<string>, GenericResponse<string>>("Test", req => new GenericResponse<string>());
                target.Participate<GenericRequest<int>, GenericResponse<int>>("Test", req => new GenericResponse<int>());
            };
            act.ShouldNotThrow();
        }

    }

}
