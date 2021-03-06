﻿using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_CircuitBreaker_Tests
    {
        [Test]
        public void Request_CircuitBreaker()
        {
            int requests = 0;
            var target = new MessageBus();
            target.Listen<int, int>(l => l
                .WithDefaultTopic()
                .Invoke(i =>
                {
                    requests++;
                    if (i == 1)
                        throw new Exception("test");
                    return i * 10;
                })
                .OnWorker()
                .WithCircuitBreaker(1, 500));

            // First request fails. This trips the circuit breaker
            var response = target.Request<int, int>(1);
            response.WaitForResponse();
            response.GetResponse().Should().Be(0);
            requests.Should().Be(1);

            // Second request trips the circuit breaker and returns no result
            response = target.Request<int, int>(2);
            response.WaitForResponse();
            response.GetResponse().Should().Be(0);
            requests.Should().Be(1);

            // Third request passes. The circuit breaker has time to reset itself
            Thread.Sleep(1000);
            response = target.Request<int, int>(3);
            response.WaitForResponse();
            response.GetResponse().Should().Be(30);
            requests.Should().Be(2);
        }
    }
}