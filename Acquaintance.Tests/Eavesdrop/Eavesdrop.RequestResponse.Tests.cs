using Acquaintance.RequestResponse;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;

namespace Acquaintance.Tests.Eavesdrop
{
    [TestFixture]
    public class Eavesdrop_RequestResponse_Tests
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
        public void RequestAndResponse_Eavesdrop()
        {
            var target = new MessageBus();
            string eavesdropped = null;
            target.Listen<TestRequest, TestResponse>(l => l
                .WithTopic("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" })
                .Immediate());
            target.Eavesdrop<TestRequest, TestResponse>(s => s
                .WithTopic("Test")
                .Invoke(conv => eavesdropped = conv.Responses.Select(r => r.Text).FirstOrDefault())
                .Immediate());
            var response = target.Request<TestRequest, TestResponse>("Test", new TestRequest { Text = "Request" });
            eavesdropped.Should().Be("RequestResponded");
        }
    }
}
