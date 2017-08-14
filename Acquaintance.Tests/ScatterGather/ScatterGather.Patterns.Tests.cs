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

        private class TestRequestWithResponse : IRequestWithResponse<TestResponse>
        {
            public string Text { get; set; }
        }

        [Test]
        public void ParticipateScatterGather_MapReduce()
        {
            var target = new MessageBus();

            target.Participate<TestRequestWithResponse, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "A" }));
            target.Participate<TestRequestWithResponse, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "B" }));
            target.Participate<TestRequestWithResponse, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "C" }));
            target.Participate<TestRequestWithResponse, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "D" }));
            target.Participate<TestRequestWithResponse, TestResponse>(l => l.WithDefaultTopic().Invoke(r => new TestResponse { Text = r.Text + "E" }));

            var response = target.Scatter<TestRequestWithResponse, TestResponse>(new TestRequestWithResponse { Text = "x" })
                .GatherResponses(5)
                .Select(r => r.Response.Text)
                .OrderBy(s=> s);

            var reduced = string.Join("", response);
            reduced.Should().Be("xAxBxCxDxE");
        }
    }
}
