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
            builder.AddNode<string>("capitalize", b => b
                .ReadInput()
                .Transform(s => s.ToUpperInvariant()));
            builder.AddNode<string>("exclaim", b => b
                .ReadOutputFrom("capitalize")
                .Transform(s => s + "!!!"));
            builder.AddNode<string>("save string", b => b
                .ReadOutputFrom("exclaim")
                .Handle(s =>
                {
                    output = s;
                    resetEvent.Set();
                }));

            var target = builder.BuildNet();
            target.Inject("test");
            resetEvent.WaitOne(1000).Should().BeTrue();
            output.Should().Be("TEST!!!");
        }

        [Test]
        public void Transform_Pipeline_SimpleNodeRef()
        {
            string output = null;
            var builder = new NetBuilder();
            var resetEvent = new ManualResetEvent(false);
            var n1 = builder.AddNode<string>("capitalize", b => b
                .ReadInput()
                .Transform(s => s.ToUpperInvariant()));
            var n2 = builder.AddNode<string>("exclaim", b => b
                .ReadOutputFrom(n1)
                .Transform(s => s + "!!!"));
            builder.AddNode<string>("save string", b => b
                .ReadOutputFrom(n2)
                .Handle(s =>
                {
                    output = s;
                    resetEvent.Set();
                }));

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
            builder.AddNode<string>("capitalize", b => b
                .ReadInput()
                .Transform(s => s.ToUpperInvariant())
                .OnCondition(s => s == "test"));
            builder.AddNode<string>("exclaim", b => b
                .ReadOutputFrom("capitalize")
                .Transform(s => s + "!!!"));
            builder.AddNode<string>("save string", b => b
                .ReadOutputFrom("exclaim")
                .Handle(s =>
                {
                    output += s;
                    resetEvent.Set();
                }));

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
            builder.AddNode<int>("increment", b => b
                .ReadInput()
                .TransformMany(i => new[] { i + 1, i + 4 }));
            builder.AddNode<int>("multiply", b => b
                .ReadOutputFrom("increment")
                .TransformMany(i => new[] { i * 2, i * 3 }));
            builder.AddNode<int>("save", b => b
                .ReadOutputFrom("multiply")
                .Handle(s =>
                {
                    results.Add(s);
                })
                .OnDedicatedThread());

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
            builder.AddNode<string>("capitalize", b => b
                .ReadInput()
                .Handle(s => output += s.ToUpperInvariant()));
            builder.AddNode<string>("exclaim", b => b
                .ReadOutputFrom("capitalize")
                .Handle(s =>
                {
                    output += s + "!!!";
                    resetEvent.Set();
                }));

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
            builder.AddNode<string>("throws", b => b
                .ReadInput()
                .Handle(s => throw new Exception("throws")));
            builder.AddErrorNode<string>("save", b => b
                .ReadOutputFrom("throws")
                .Handle(s =>
                {
                    output = "caught error " + s.Error.Message;
                    resetEvent.Set();
                }));

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
            builder.AddNode<string>("add thread id", b => b
                .ReadInput()
                .Transform(s => s + Thread.CurrentThread.ManagedThreadId)
                .OnDedicatedThreads(4));

            builder.AddNode<string>("output", b => b
                .ReadOutputFrom("add thread id")
                .Handle(s =>
                {
                    receivedMessages++;
                    if (!outputs.Contains(s))
                        outputs.Add(s);
                })
                .OnDedicatedThread());

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
            public void Handle(Envelope<int> message)
            {
                Value = message.Payload;
            }

            public int Value { get; private set; }
        }

        [Test]
        public void Handle_Stateful()
        {
            var handler = new TestHandler();
            var builder = new NetBuilder();
            var resetEvent = new ManualResetEvent(false);
            builder.AddNode<int>("handler", b => b
                .ReadInput()
                .Handle(handler));
            builder.AddNode<int>("set", b => b
                .ReadOutputFrom("handler")
                .Handle(i => resetEvent.Set()));

            var target = builder.BuildNet();
            target.Inject(1);
            resetEvent.WaitOne(1000).Should().BeTrue();
            handler.Value.Should().Be(1);
        }
    }

    
}
