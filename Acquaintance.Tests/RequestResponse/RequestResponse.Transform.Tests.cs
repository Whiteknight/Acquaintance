using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_Transform_Tests
    {
        [Test]
        public void ListenTransformRequest_Test()
        {
            var target = new MessageBus();
            string request = null;
            target.Listen<string, int>(l => l
                .WithTopic("test string")
                .Invoke(r =>
                {
                    request = r;
                    return 5;
                }));
            target.Listen<int, int>(l => l
                .WithTopic("test int")
                .TransformRequestTo("test string", r => r.ToString() + "A"));
            var response = target.RequestWait<int, int>("test int", 4);

            response.Should().Be(5);
            request.Should().Be("4A");
        }

        [Test]
        public void ListenTransformResponse_Test()
        {
            var target = new MessageBus();
            target.Listen<int, string>(l => l
                .WithTopic("test string")
                .Invoke(r => "5"));
            target.Listen<int, int>(l => l
                .WithTopic("test int")
                .TransformResponseFrom<string>("test string", int.Parse));
            var response = target.RequestWait<int, int>("test int", 4);

            response.Should().Be(5);
        }
    }
}