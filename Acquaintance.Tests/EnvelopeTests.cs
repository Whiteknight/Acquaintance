using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class EnvelopeTests
    {
        [Test]
        public void GetMetadata_Empty()
        {
            var target = new Envelope<string>(1, "Test", "Payload");
            target.GetMetadata("Anything").Should().BeNull();
        }

        [Test]
        public void SetMetadata_Test()
        {
            var target = new Envelope<string>(1, "Test", "Payload");
            target.SetMetadata("Anything", "Value");
            target.GetMetadata("Anything").Should().Be("Value");
        }
    }
}
