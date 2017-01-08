using Acquaintance.Nets;
using FluentAssertions;
using NUnit.Framework;
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
                .Transform<string>(s =>
                {
                    return s.ToUpperInvariant();
                });
            builder.AddNode<string>("exclaim")
                .ReadFrom("capitalize")
                .Transform<string>(s =>
                {
                    return s + "!!!";
                });
            builder.AddNode<string>("save string")
                .ReadFrom("exclaim")
                .Handle(s =>
                {
                    output = s;
                });

            var target = builder.BuildNet();
            target.Inject<string>("test");
            Thread.Sleep(1000);
            output.Should().Be("TEST!!!");
        }
    }
}
