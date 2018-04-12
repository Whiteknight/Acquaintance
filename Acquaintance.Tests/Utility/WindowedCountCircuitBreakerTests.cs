using System;
using System.Collections.Generic;
using System.Text;
using Acquaintance.Utility;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.Utility
{
    [TestFixture]
    public class WindowedCountCircuitBreakerTests
    {
        [Test]
        public void NotTripped_Test()
        {
            var target = new WindowedCountingCircuitBreaker(1000, 2, 4);
            target.CanProceed().Should().BeTrue();
            target.RecordResult(true);
            target.RecordResult(true);
            target.RecordResult(true);
            target.RecordResult(true);
            target.RecordResult(true);
            target.CanProceed().Should().BeTrue();
        }

        [Test]
        public void TrippedAndReset_Test()
        {
            var target = new WindowedCountingCircuitBreaker(1000, 2, 4);
            target.CanProceed().Should().BeTrue();
            target.RecordResult(false);
            target.RecordResult(false);
            target.CanProceed().Should().BeFalse();
            target.RecordResult(true);
            target.RecordResult(true);
            target.RecordResult(true);
            target.CanProceed().Should().BeTrue();
        }
    }
}
