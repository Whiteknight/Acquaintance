using System;
using Acquaintance.Nets;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.Nets
{
    [TestFixture]
    public class Net_Validation_Tetsts
    {
        [Test]
        public void Validate_Empty()
        {
            var target = new NetBuilder();
            var net = target.BuildNet();
            Action act = () => net.Validate();
            act.ShouldThrow<NetValidationException>();
        }

        [Test]
        public void Validate_Ok()
        {
            var target = new NetBuilder();
            target.AddNode<string>("node", b => b
                .ReadInput()
                .Handle(s => { }));
            var net = target.BuildNet();
            Action act = () => net.Validate();
            act.ShouldNotThrow<NetValidationException>();
        }

        [Test]
        public void Validate_BadInput()
        {
            var target = new NetBuilder();
            target.AddNode<string>("Node", b => b
                .ReadOutputFrom("DOES NOT EXIST")
                .Handle(s => { }));
            var net = target.BuildNet();
            Action act = () => net.Validate();
            act.ShouldThrow<NetValidationException>();
        }
    }
}
