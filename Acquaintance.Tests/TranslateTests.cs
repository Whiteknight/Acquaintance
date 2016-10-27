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
            target.Subscribe<OutputEvent>("Test", e => text = e.Text + "Output");
            target.Transform<InputEvent, OutputEvent>("Test", input => new OutputEvent(input.Text + "Translated"), null, "Test");
            target.Publish("Test", new InputEvent("Test2"));
            text.Should().Be("Test2TranslatedOutput");
        }

    }
}
