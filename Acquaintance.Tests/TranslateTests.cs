using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests
{
    [TestFixture]
    public class TranslateTests
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
        public void Translate()
        {
            var target = new MessageBus();
            string text = null;
            var options = new SubscribeOptions { DispatchType = Threading.DispatchThreadType.Immediate };
            target.Subscribe<OutputEvent>("Test", e => text = e.Text + "Output", options);
            target.SubscribeTransform<InputEvent, OutputEvent>("Test", input => new OutputEvent(input.Text + "Translated"), null, "Test", options);
            target.Publish("Test", new InputEvent("TestPayload"));
            text.Should().Be("TestPayloadTranslatedOutput");
        }

    }
}
