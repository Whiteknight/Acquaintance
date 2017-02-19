using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_Wrap_Tests
    {
        [Test]
        public void Requestresponse_WrapFunction()
        {
            var target = new MessageBus();

            var func = target.WrapFunction<string, int>(e => e.Length, b => b.Immediate()).Function;
            func("Test2").Should().Be(5);
        }
    }
}
