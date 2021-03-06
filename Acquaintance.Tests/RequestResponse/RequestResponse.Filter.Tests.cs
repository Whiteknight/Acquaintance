﻿using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_Filter_Tests
    {
        [Test]
        public void RequestRouter_Publish()
        {
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithDefaultTopic()
                .Invoke(e => e * 10)
                .Immediate()
                .WithFilter(e => e % 2 == 0));

            target.RequestWait<int, int>(1).Should().Be(0);
            target.RequestWait<int, int>(2).Should().Be(20);
            target.RequestWait<int, int>(3).Should().Be(0);
            target.RequestWait<int, int>(4).Should().Be(40);
            target.RequestWait<int, int>(5).Should().Be(0);
        }
    }
}
