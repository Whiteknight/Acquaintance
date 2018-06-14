using Acquaintance.Scanning;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.RequestResponse
{
    [TestFixture]
    public class RequestResponse_AutoWireup_Tests
    {
        public class TestClass_Sanity
        {
            [Listener(typeof(string), typeof(int))]
            public int Method(string s)
            {
                return s.Length;
            }
        }

        [Test]
        public void AutoWireup_Sanity()
        {
            var target = new MessageBusBuilder().Build();
            var obj = new TestClass_Sanity();
            target.AutoWireup(obj);
            var response = target.Request<string, int>("", "test");
            response.WaitForResponse();
            response.GetResponse().Should().Be(4);
        }

        public class TestClass_Envelope
        {
            [Listener(typeof(string), typeof(int))]
            public int Method(Envelope<string> s)
            {
                return s.Payload.Length;
            }
        }

        [Test]
        public void AutoWireup_Envelope()
        {
            var target = new MessageBusBuilder().Build();
            var obj = new TestClass_Envelope();
            target.AutoWireup(obj);
            var response = target.Request<string, int>("", "test");
            response.WaitForResponse().Should().BeTrue();
            response.GetResponse().Should().Be(4);
        }

        public class TestClass_Parameterless
        {
            [Listener(typeof(string), typeof(int))]
            public int Method()
            {
                return 5;
            }
        }

        [Test]
        public void AutoWireup_Parameterless()
        {
            var target = new MessageBusBuilder().Build();
            var obj = new TestClass_Parameterless();
            target.AutoWireup(obj);
            var response = target.Request<string, int>("", "test");
            response.WaitForResponse();
            response.GetResponse().Should().Be(5);
        }

        public class TestClass_GenericParameter
        {
            [Listener(typeof(string), typeof(int))]
            public int Method<T>(T s)
            {
                return (s as string).Length;
            }
        }

        [Test]
        public void AutoWireup_GenericParameter()
        {
            var target = new MessageBusBuilder().Build();
            var obj = new TestClass_GenericParameter();
            target.AutoWireup(obj);
            var response = target.Request<string, int>("", "test");
            response.WaitForResponse();
            response.GetResponse().Should().Be(4);
        }

        public class TestClass_GenericReturn
        {
            [Listener(typeof(string), typeof(int))]
            public TResponse Method<TRequest, TResponse>(string s)
            {
                var o = (object)s.Length;
                return (TResponse) o;
            }
        }

        [Test]
        public void AutoWireup_GenericReturn()
        {
            var target = new MessageBusBuilder().Build();
            var obj = new TestClass_GenericReturn();
            target.AutoWireup(obj);
            var response = target.Request<string, int>("", "test");
            response.WaitForResponse();
            response.GetResponse().Should().Be(4);
        }

        public class TestClass_NullResponseType
        {
            [Listener(typeof(string))]
            public int Method(string s)
            {
                return s.Length;
            }
        }

        [Test]
        public void AutoWireup_NullResponseType()
        {
            var target = new MessageBusBuilder().Build();
            var obj = new TestClass_NullResponseType();
            target.AutoWireup(obj);
            var response = target.Request<string, int>("", "test");
            response.WaitForResponse();
            response.GetResponse().Should().Be(4);
        }

        public class TestClass_NullRequestType
        {
            [Listener(null, typeof(int))]
            public int Method(string s)
            {
                return s.Length;
            }
        }

        [Test]
        public void AutoWireup_NullRequestType()
        {
            var target = new MessageBusBuilder().Build();
            var obj = new TestClass_NullRequestType();
            target.AutoWireup(obj);
            var response = target.Request<string, int>("", "test");
            response.WaitForResponse();
            response.GetResponse().Should().Be(4);
        }
    }
}
