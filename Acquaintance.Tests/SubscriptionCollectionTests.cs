using System;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class SubscriptionCollectionTests
    {
        [Test]
        public void SubscriptionCollection_CreateReport_Test()
        {
            var target = new SubscriptionCollection(new MessageBus());
            target.Subscribe<int>(b => b
                .WithTopic("test1")
                .Invoke(x => { }));
            target.Listen<int, string>(b => b
                .WithTopic("test2")
                .Invoke(x => "ok"));
            target.Participate<long, DateTime>(b => b
                .WithTopic("test3")
                .Invoke(x => DateTime.UtcNow));

            var report = target.ReportContents();
            report.Length.Should().Be(3);
        }
    }
}
