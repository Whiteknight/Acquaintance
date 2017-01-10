using Acquaintance.Nets;
using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class NetTests
    {
        [Test]
        public void PipelineSimple()
        {
            string output = null;
            var builder = new NetBuilder();
            builder.AddNode<string>("capitalize")
                .ReadInput()
                .Transform(s => s.ToUpperInvariant());
            builder.AddNode<string>("exclaim")
                .ReadFrom("capitalize")
                .Transform(s => s + "!!!");
            builder.AddNode<string>("save string")
                .ReadFrom("exclaim")
                .Handle(s =>
                {
                    output = s;
                });

            var target = builder.BuildNet();
            target.Inject("test");
            Thread.Sleep(1000);
            output.Should().Be("TEST!!!");
        }

        [Test]
        public void Pipeline_OnDedicatedThreads()
        {
            int receivedMessages = 0;
            var outputs = new HashSet<string>();
            var builder = new NetBuilder();
            builder.AddNode<string>("add thread id")
                .ReadInput()
                .Transform(s => s + Thread.CurrentThread.ManagedThreadId)
                .OnDedicatedThreads(4);

            builder.AddNode<string>("output")
                .ReadFrom("add thread id")
                .Handle(s =>
                {
                    receivedMessages++;
                    if (!outputs.Contains(s))
                        outputs.Add(s);
                })
                .OnDedicatedThread();

            var target = builder.BuildNet();

            target.Inject("thread:");
            target.Inject("thread:");
            target.Inject("thread:");
            target.Inject("thread:");
            target.Inject("thread:");
            target.Inject("thread:");
            target.Inject("thread:");
            target.Inject("thread:");
            target.Inject("thread:");
            target.Inject("thread:");

            Thread.Sleep(1000);

            receivedMessages.Should().Be(10);
            outputs.Count.Should().BeGreaterThan(1);
        }
    }
}
