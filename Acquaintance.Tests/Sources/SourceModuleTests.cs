using Acquaintance.Sources;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading;

namespace Acquaintance.Tests.Sources
{
    [TestFixture]
    public class SourceModuleTests
    {
        private class TestEventSource1 : IEventSource
        {
            private readonly ManualResetEvent _resetEvent;

            public TestEventSource1(ManualResetEvent resetEvent)
            {
                _resetEvent = resetEvent;
            }

            public void CheckForEvents(IEventSourceContext context, CancellationToken cancellationToken)
            {
                context.Publish<int>(1);
                context.Publish<int>(2);
                context.Publish<int>(4);
                context.Complete();
                _resetEvent.Set();
            }
        }

        [Test]
        public void RunEventSource_Test1()
        {
            int sum = 0;
            IDisposable token = null;
            var messageBus = new MessageBus();
            messageBus.InitializeEventSources();
            messageBus.Subscribe<int>(b => b.WithDefaultTopic().Invoke(i => sum += i));
            var resetEvent = new ManualResetEvent(false);
            var target = new TestEventSource1(resetEvent);

            try
            {
                token = messageBus.RunEventSource(target);
                resetEvent.WaitOne(5000).Should().BeTrue();
                sum.Should().Be(7);
            }
            finally
            {
                token?.Dispose();
                resetEvent.Dispose();
                messageBus.Dispose();
            }
        }

        private class TestEventSource2 : IEventSource
        {
            private readonly ManualResetEvent _resetEvent;
            private int _iteration;

            public TestEventSource2(ManualResetEvent resetEvent)
            {
                _resetEvent = resetEvent;
                _iteration = 0;
            }

            public void CheckForEvents(IEventSourceContext context, CancellationToken cancellationToken)
            {
                _iteration++;
                if (_iteration == 1)
                    context.Publish<int>(1);
                else if (_iteration == 2)
                    context.Publish<int>(2);
                else if (_iteration == 3)
                    context.Publish<int>(4);
                else
                {
                    context.Complete();
                    _resetEvent.Set();
                }
            }
        }

        [Test]
        public void RunEventSource_Test2()
        {
            int sum = 0;
            IDisposable token = null;
            var messageBus = new MessageBus();
            messageBus.InitializeEventSources();
            messageBus.Subscribe<int>(b => b.WithDefaultTopic().Invoke(i => sum += i));
            var resetEvent = new ManualResetEvent(false);
            var target = new TestEventSource2(resetEvent);

            try
            {
                token = messageBus.RunEventSource(target);
                resetEvent.WaitOne(5000).Should().BeTrue();
                sum.Should().Be(7);
            }
            finally
            {
                token?.Dispose();
                resetEvent.Dispose();
                messageBus.Dispose();
            }
        }

        [Test]
        public void RunEventSource_ThreadReport()
        {
            IDisposable token = null;
            var messageBus = new MessageBus();
            messageBus.InitializeEventSources();
            var resetEvent = new ManualResetEvent(false);
            var target = new TestEventSource2(resetEvent);

            try
            {
                token = messageBus.RunEventSource(target);
                var report = messageBus.ThreadPool.GetThreadReport();
                report.RegisteredThreads.Count.Should().Be(1);
                var str = report.ToString();
            }
            finally
            {
                token?.Dispose();
                resetEvent.Dispose();
                messageBus.Dispose();
            }
        }

        [Test]
        public void RunEventSource__Delegate()
        {
            IDisposable token = null;
            var messageBus = new MessageBus();
            messageBus.InitializeEventSources();
            var resetEvent = new ManualResetEvent(false);
            var target = new TestEventSource2(resetEvent);

            try
            {
                token = messageBus.RunEventSource(target);
                var report = messageBus.ThreadPool.GetThreadReport();
                report.RegisteredThreads.Count.Should().Be(1);
                var str = report.ToString();
            }
            finally
            {
                token?.Dispose();
                resetEvent.Dispose();
                messageBus.Dispose();
            }
        }
    }
}
