﻿using System.Threading;
using Acquaintance.PubSub;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.PubSub
{
    [TestFixture]
    public class PubSub_AutoSubscribe_Tests
    {
        public class TestClass1
        {
            private readonly ManualResetEvent _wait;

            public TestClass1(ManualResetEvent wait)
            {
                _wait = wait;
            }

            public string Value { get; private set; }

            [Subscription(typeof(string))]
            public void StringSubscriber(string s)
            {
                Value = s;
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_Test()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass1(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("test");
                wait.WaitOne(2000).Should().Be(true);
                obj.Value.Should().Be("test");
            }
        }

        public class TestClass2
        {
            private readonly ManualResetEvent _wait;

            public TestClass2(ManualResetEvent wait)
            {
                _wait = wait;
            }

            public string Value { get; private set; }

            [Subscription(typeof(string), new[] { "B" })]
            public void StringSubscriber(string s)
            {
                Value = s;
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_Topics()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass2(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("A", "test1");
                target.Publish("B", "test2");
                target.Publish("C", "test3");
                wait.WaitOne(2000).Should().Be(true);
                obj.Value.Should().Be("test2");
            }
        }

        public class TestClass3
        {
            private readonly ManualResetEvent _wait;

            public TestClass3(ManualResetEvent wait)
            {
                _wait = wait;
            }

            public string Value { get; private set; }

            [Subscription(typeof(string))]
            public void StringSubscriber(Envelope<string> s)
            {
                Value = s.Payload;
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_Envelope()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass3(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("test");
                wait.WaitOne(2000).Should().Be(true);
                obj.Value.Should().Be("test");
            }
        }

        public class TestClass4
        {
            private readonly ManualResetEvent _wait;

            public TestClass4(ManualResetEvent wait)
            {
                _wait = wait;
            }

            [Subscription(typeof(string))]
            public void ParameterlessSubscriber()
            {
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_Parameterless()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass4(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("test");
                wait.WaitOne(2000).Should().Be(true);
            }
        }

        public class TestClass5
        {
            private readonly ManualResetEvent _wait;

            public TestClass5(ManualResetEvent wait)
            {
                _wait = wait;
            }

            public string Value { get; private set; }

            [Subscription(typeof(string))]
            public void GenericSubscriber<T>(T payload)
            {
                Value = payload.ToString();
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_GenericParameter()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass5(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("test");
                wait.WaitOne(2000).Should().Be(true);
                obj.Value.Should().Be("test");
            }
        }

        public class TestClass6
        {
            private readonly ManualResetEvent _wait;

            public TestClass6(ManualResetEvent wait)
            {
                _wait = wait;
            }

            [Subscription(typeof(string))]
            public void GenericSubscriber<T>()
            {
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_GenericParameterless()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass6(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("test");
                wait.WaitOne(2000).Should().Be(true);
            }
        }

        public class TestClass7<T>
        {
            private readonly ManualResetEvent _wait;

            public TestClass7(ManualResetEvent wait)
            {
                _wait = wait;
            }

            public string Value { get; private set; }

            [Subscription(typeof(string))]
            public void GenericSubscriber(T payload)
            {
                Value = payload.ToString();
                _wait.Set();
            }
        }

        [Test]
        public void Autosubscribe_GenericClass()
        {
            using (var wait = new ManualResetEvent(false))
            {
                var target = new MessageBus();
                var obj = new TestClass7<string>(wait);
                var token = target.AutoSubscribe(obj);

                target.Publish("test");
                wait.WaitOne(2000).Should().Be(true);
                obj.Value.Should().Be("test");
            }
        }
    }
}
