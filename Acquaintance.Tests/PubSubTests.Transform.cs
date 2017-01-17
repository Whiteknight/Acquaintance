﻿using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    [TestFixture]
    public partial class PubSubTests
    {
        private class InputEvent
        {
            public string Text { get; set; }

            public InputEvent(string text)
            {
                Text = text;
            }
        }

        private class OutputEvent
        {
            public string Text { get; set; }

            public OutputEvent(string text)
            {
                Text = text;
            }
        }

        [Test]
        public void Subscribe_SubscriptionBuilder_Transform()
        {
            var target = new MessageBus();
            string text = null;
            target.Subscribe<OutputEvent>(builder => builder
                .WithChannelName("Test")
                .Invoke(e => text = e.Text + "Output")
                .Immediate());
            target.Subscribe<InputEvent>(builder => builder
                .WithChannelName("Test")
                .TransformTo(input => new OutputEvent(input.Text + "Translated"), "Test")
                .Immediate()
            );
            target.Publish("Test", new InputEvent("TestPayload"));
            text.Should().Be("TestPayloadTranslatedOutput");
        }
    }
}