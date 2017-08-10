using Acquaintance.RequestResponse;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_Patterns_Tests
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
        public void ParticipateScatterGather_MapReduce()
        {
            var target = new MessageBus();

            target.Participate<TestRequest, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "A" }));
            target.Participate<TestRequest, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "B" }));
            target.Participate<TestRequest, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "C" }));
            target.Participate<TestRequest, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "D" }));
            target.Participate<TestRequest, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "E" }));

            var response = target.Scatter<TestRequest, TestResponse>(new TestRequest { Text = "x" });

            var reduced = string.Join("", response.Responses.Select(r => r.Responses[0].Text).OrderBy(s => s));
            reduced.Should().Be("xAxBxCxDxE");
        }
    }
}
