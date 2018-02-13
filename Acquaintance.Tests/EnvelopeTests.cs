using System;
using System.Collections.Generic;
using Acquaintance.Utility;
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
            var target = new Envelope<string>(Guid.Empty, 1, "Test", "Payload");
            target.GetMetadata("Anything").Should().BeNull();
        }

        [Test]
        public void SetMetadata_Test()
        {
            var target = new Envelope<string>(Guid.Empty, 1, "Test", "Payload");
            target.SetMetadata("Anything", "Value");
            target.GetMetadata("Anything").Should().Be("Value");
        }

        [Test]
        public void HasMetadataFromFactory()
        {
            var target = new EnvelopeFactory(Guid.Empty).Create("test", 5, new Dictionary<string, string>
            {
                { "value1", "result1" }
            });

            target.Should().BeOfType<Envelope<int>>();
            target.GetMetadata("value1").Should().Be("result1");
        }

        [Test]
        public void RedirectsWithMetadata()
        {
            var target = new Envelope<int>(Guid.Empty, 1, "test", 5);
            target.SetMetadata("key", "value");

            var result = target.RedirectToTopic("other");
            result.GetMetadata("key").Should().Be("value");
        }
    }
}
