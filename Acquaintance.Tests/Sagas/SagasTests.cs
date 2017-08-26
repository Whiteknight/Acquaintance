using System.Collections.Generic;
using System.Threading;
using Acquaintance.Sagas;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.Sagas
{
    [TestFixture]
    public class SagasTests
    {
        private class MyState
        {
            public MyState(string start)
            {
                Start = start;
                Continues = new List<int>();
            }

            public string Start { get; }
            public List<int> Continues { get; }
        }

        [Test]
        public void Saga_Test()
        {
            var target = new MessageBus();
            var reset = new ManualResetEvent(false);
            MyState state = null;
            target.InitializeSagas(1);
            target.CreateSaga<MyState, int>(b => b
                .StartWith<string>(null, x => 1, x => new MyState(x), null)
                .ContinueWith<int>(null, x => 1, (c, x) =>
                {
                    c.State.Continues.Add(x);
                    if (c.State.Continues.Count >= 2)
                        c.Complete();
                })
                .WhenCompleted((m, c) => {
                    m.Publish(c);
                }));
            target.Subscribe<MyState>(b => b
                .WithDefaultTopic()
                .Invoke(s =>
                {
                    state = s;
                    reset.Set();
                }));

            target.Publish("Test");
            target.Publish(1);
            target.Publish(2);

            reset.WaitOne(1000).Should().BeTrue();
            state.Should().NotBeNull();
            state.Start.Should().Be("Test");
            state.Continues.Should().BeEquivalentTo(new[] { 1, 2 });
        }
    }
}
