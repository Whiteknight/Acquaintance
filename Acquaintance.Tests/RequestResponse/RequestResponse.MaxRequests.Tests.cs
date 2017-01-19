using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_MaxRequests_Tests
    {
        [Test]
        public void ListenRequestAndResponse_MaxRequests()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithChannelName("Test")
                .Invoke(e => e + 5)
                .Immediate()
                .MaximumRequests(3));
            var responses = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                var response = target.Request<int, int>("Test", i);
                responses.Add(response);
            }

            responses.Should().BeEquivalentTo(5, 6, 7, 0, 0);
        }
    }
}
