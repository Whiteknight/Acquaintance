using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_Proxy_Tests
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
        public void ListenRequestAndResponse()
        {
            var target = new MessageBus();
            var requestChannel = target.GetRequestChannel<TestRequest, TestResponse>();
            requestChannel.Listen(l => l
                .WithTopic("Test")
                .Invoke(req => new TestResponse { Text = req.Text + "Responded" }));

            var response = requestChannel.Request("Test", new TestRequest { Text = "Request" });
            response.Should().NotBeNull();
            response.Text.Should().Be("RequestResponded");
        }
    }
}
