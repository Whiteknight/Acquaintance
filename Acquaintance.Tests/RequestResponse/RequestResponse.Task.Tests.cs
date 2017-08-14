using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_Task_Tests
    {
        [Test]
        public void RequestAsync_Test()
        {
            var target = new MessageBus();
            target.Listen<int, int>(b => b
                .WithDefaultTopic()
                .Invoke(x => x * 5));
            var task = target.RequestAsync<int, int>(6);
            task.Wait();
            task.Result.Should().Be(30);
        }
    }
}
