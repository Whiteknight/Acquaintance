﻿using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class ScatterRouterTests
    {
        [Test]
        public void ScatterRouter_Publish()
        {
            var target = new MessageBus();
            target.Participate<int, int>(l => l.WithChannelName("Evens").InvokeFunction(e => e * 10));
            target.Participate<int, int>(l => l.WithChannelName("Odds").InvokeFunction(e => e * 100));

            target.Participate<int, int>(l => l
                .RouteForward(e => e % 2 == 0, "Evens")
                .RouteForward(e => e % 2 == 1, "Odds"));

            target.Scatter<int, int>(1).Should().Contain(100);
            target.Scatter<int, int>(2).Should().Contain(20);
            target.Scatter<int, int>(3).Should().Contain(300);
            target.Scatter<int, int>(4).Should().Contain(40);
            target.Scatter<int, int>(5).Should().Contain(500);
        }
    }
}