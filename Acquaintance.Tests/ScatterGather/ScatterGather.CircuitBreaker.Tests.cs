using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_CircuitBreaker_Tests
    {
        [Test]
        public void Scatter_CircuitBreaker()
        {
            int requests = 0;
            var target = new MessageBus();
            target.Participate<int, int>(l => l
                .WithDefaultTopic()
                .Invoke(i =>
                {
                    requests++;
                    if (i == 1)
                        throw new Exception("test");
                    return i * 10;
                })
                .Immediate()
                .WithCircuitBreaker(1, 500));

            // First request fails. This trips the circuit breaker but returns error information
            var scatter = target.Scatter<int, int>(1);
            var response = scatter.GatherResponses(1).First();
            response.IsEmpty.Should().BeFalse();
            response.ErrorInformation.Should().NotBeNull();
            response.Value.Should().Be(0);
            requests.Should().Be(1);

            // Second request trips the circuit breaker and returns no result
            scatter = target.Scatter<int, int>(2);
            response = scatter.GatherResponses(1).First();
            response.IsEmpty.Should().BeTrue();
            requests.Should().Be(1);

            // Third request passes. The circuit breaker has time to reset itself
            Thread.Sleep(1000);
            scatter = target.Scatter<int, int>(3);
            response = scatter.GatherResponses(1).First();
            response.IsEmpty.Should().BeFalse();
            response.Value.Should().Be(30);
            requests.Should().Be(2);
        }
    }
}