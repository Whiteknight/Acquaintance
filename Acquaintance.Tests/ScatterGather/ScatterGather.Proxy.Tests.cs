using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_Proxy_Tests
    {
        private class TestResponse
        {
            public string Text { get; set; }
        }

        private class TestRequest 
        {
            public string Text { get; set; }
        }

        [Test]
        public void ParticipateScatterGather()
        {
            var target = new MessageBus();
            var channel = target.GetScatterChannel<TestRequest, TestResponse>();
            channel.Participate(l => l
                .WithTopic("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));
            var response = channel.Scatter("Test", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            var responses = response.GatherResponses(1);
            responses[0].Response.Text.Should().Be("RequestResponded");
        }
    }
}
