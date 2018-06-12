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
            var target = new Envelope<string>("A", 1, "Test", "Payload");
            target.GetMetadata("Anything").Should().BeNull();
        }

        [Test]
        public void SetMetadata_Test()
        {
            var target = new Envelope<string>("A", 1, "Test", "Payload");
            target.SetMetadata("Anything", "Value");
            target.GetMetadata("Anything").Should().Be("Value");
        }

        [Test]
        public void HasMetadataFromFactory()
        {
            var target = new EnvelopeFactory("A", new LocalIncrementIdGenerator()).Create("test", 5, new Dictionary<string, string>
            {
                { "value1", "result1" }
            });

            target.Should().BeOfType<Envelope<int>>();
            target.GetMetadata("value1").Should().Be("result1");
        }

        [Test]
        public void RedirectsWithMetadata()
        {
            var target = new Envelope<int>("A", 1, "test", 5);
            target.SetMetadata("key", "value");

            var result = target.RedirectToTopic("other");
            result.GetMetadata("key").Should().Be("value");
        }

        [Test]
        public void NoDuplicateIds_LocalIncrement()
        {
            const int numEnvelopes = 1000;
            var target = new EnvelopeFactory("test", new LocalIncrementIdGenerator());
            var seenIds = new HashSet<long>();
            for (int i = 0; i < numEnvelopes; i++)
            {
                var envelope = target.Create("", i);
                seenIds.Contains(envelope.Id).Should().BeFalse();
                seenIds.Add(envelope.Id);
            }
        }

        [Test]
        public void NoDuplicateIds_NetworkedIncrement()
        {
            const int numEnvelopes = 1000;
            var target = new EnvelopeFactory("test", new NetworkedIncrementIdGenerator(5));
            var seenIds = new HashSet<long>();
            for (int i = 0; i < numEnvelopes; i++)
            {
                var envelope = target.Create("", i);
                seenIds.Contains(envelope.Id).Should().BeFalse();
                seenIds.Add(envelope.Id);
            }
        }

        [Test]
        public void GetEnvelopeHistory_Tests()
        {
            var envelope = new Envelope<int>("A", 1, "Test", 5);

            var entry = Envelope.CreateHistoryEntry("A", 1);
            envelope.AppendMetadata(Envelope.MetadataHistory, entry);
            entry = Envelope.CreateHistoryEntry("B", 2);
            envelope.AppendMetadata(Envelope.MetadataHistory, entry);
            entry = Envelope.CreateHistoryEntry("C", 3);
            envelope.AppendMetadata(Envelope.MetadataHistory, entry);

            var history = envelope.GetHistory();
            history.Should().NotBeNull();
            history.OriginBusId.Should().Be("A");
            history.Hops.Count.Should().Be(3);
            history.Hops[0].BusId.Should().Be("A");
            history.Hops[0].EnvelopeId.Should().Be(1);
            history.Hops[1].BusId.Should().Be("B");
            history.Hops[1].EnvelopeId.Should().Be(2);
            history.Hops[2].BusId.Should().Be("C");
            history.Hops[2].EnvelopeId.Should().Be(3);
        }
    }
}
