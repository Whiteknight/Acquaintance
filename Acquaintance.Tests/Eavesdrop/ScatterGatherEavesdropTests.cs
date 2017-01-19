using Acquaintance.RequestResponse;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace Acquaintance.Tests.Eavesdrop
{
    [TestFixture]
    public class ScatterGatherEavesdropTests
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
    }
}
