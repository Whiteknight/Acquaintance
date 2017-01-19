using Acquaintance.Nets;
using Acquaintance.PubSub;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Acquaintance.Tests.Nets
{
    [TestFixture]
    public class NetTests
    {
        [Test]
        public void Transform_Pipeline_Simple()
        {
            string output = null;
            var builder = new NetBuilder();
            var resetEvent = new ManualResetEvent(false);
            builder.AddNode<string>("capitalize")
                .ReadInput()
                .Transform(s => s.ToUpperInvariant());
            builder.AddNode<string>("exclaim")
                .ReadOutputFrom("capitalize")
                .Transform(s => s + "!!!");
            builder.AddNode<string>("save string")
                .ReadOutputFrom("exclaim")
                .Handle(s =>
                {
                    output = s;
                    resetEvent.Set();
                });

            var target = builder.BuildNet();
            target.Inject("test");
            resetEvent.WaitOne(1000).Should().BeTrue();
            output.Should().Be("TEST!!!");
        }

        [Test]
        public void Transform_Pipeline_OnCondition()
        {
            string output = string.Empty;
            var builder = new NetBuilder();
            var resetEvent = new ManualResetEvent(false);
            builder.AddNode<string>("capitalize")
                .ReadInput()
                .OnCondition(s => s == "test")
                .Transform(s => s.ToUpperInvariant());
            builder.AddNode<string>("exclaim")
                .ReadOutputFrom("capitalize")
                .Transform(s => s + "!!!");
            builder.AddNode<string>("save string")
                .ReadOutputFrom("exclaim")
                .Handle(s =>
                {
                    output += s;
                    resetEvent.Set();
                });

            var target = builder.BuildNet();
            target.Inject("other");
            target.Inject("test");
            resetEvent.WaitOne(1000).Should().BeTrue();
            output.Should().Be("TEST!!!");
        }

        [Test]
        public void TransformMany_Pipeline_Simple()
        {
            var results = new List<int>();
            var builder = new NetBuilder();
            builder.AddNode<int>("increment")
                .ReadInput()
                .TransformMany(i => new[] { i + 1, i + 4 });
            builder.AddNode<int>("multiply")
                .ReadOutputFrom("increment")
                .TransformMany(i => new[] { i * 2, i * 3 });
            builder.AddNode<int>("save")
                .ReadOutputFrom("multiply")
                .Handle(s =>
                {
                    results.Add(s);
                })
                .OnDedicatedThread();

            var target = builder.BuildNet();
            target.Inject(1);
            Thread.Sleep(1000);
            results.Should().BeEquivalentTo(4, 6, 10, 15);
        }

        [Test]
        public void Handle_Pipeline_Simple()
        {
            string output = "";
            var builder = new NetBuilder();
            var resetEvent = new ManualResetEvent(false);
            builder.AddNode<string>("capitalize")
                .ReadInput()
                .Handle(s => output += s.ToUpperInvariant());
            builder.AddNode<string>("exclaim")
                .ReadOutputFrom("capitalize")
                .Handle(s =>
                {
                    output += s + "!!!";
                    resetEvent.Set();
                });

            var target = builder.BuildNet();
            target.Inject("test");
            resetEvent.WaitOne(1000).Should().BeTrue();
            output.Should().Be("TESTtest!!!");
        }

        [Test]
        public void Handle_Errors_Simple()
        {
            string output = "";
            var builder = new NetBuilder();
            var resetEvent = new ManualResetEvent(false);
            builder.AddNode<string>("throws")
                .ReadInput()
                .Handle(s =>
                {
                    throw new Exception("throws");
                });
            builder.AddErrorNode<string>("save")
                .ReadOutputFrom("throws")
                .Handle(s =>
                {
                    output = "caught error " + s.Error.Message;
                    resetEvent.Set();
                });


            var target = builder.BuildNet();
            target.Inject("test");
            resetEvent.WaitOne(1000).Should().BeTrue();
            output.Should().Be("caught error throws");
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
                .ReadOutputFrom("add thread id")
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

        private class TestHandler : ISubscriptionHandler<int>
        {
            public void Handle(int payload)
            {
                Value = payload;
            }

            public int Value { get; private set; }
        }

        [Test]
        public void Handle_Stateful()
        {
            var handler = new TestHandler();
            var builder = new NetBuilder();
            var resetEvent = new ManualResetEvent(false);
            builder.AddNode<int>("handler")
                .ReadInput()
                .Handle(handler);
            builder.AddNode<int>("set")
                .ReadOutputFrom("handler")
                .Handle(i => resetEvent.Set());

            var target = builder.BuildNet();
            target.Inject(1);
            resetEvent.WaitOne(1000).Should().BeTrue();
            handler.Value.Should().Be(1);
        }
    }
}
