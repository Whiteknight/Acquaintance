using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Acquaintance.ScatterGather;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.ScatterGather
{
    [TestFixture]
    public class ScatterGather_Stress_Tests
    {
        [Test]
        public void ScatterMany_Test()
        {
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const int iterations = 100;

            var scatters = new List<IScatter<string>>();
            var target = new MessageBus(new MessageBusCreateParameters {
                MaximumQueuedMessages = letters.Length * iterations
            });
            
            foreach (char letter in letters)
            {
                target.Participate<int, string>(b => b
                    .WithDefaultTopic()
                    .Invoke(i => i.ToString() + letter)
                    .OnWorker()
                    .Named(letter.ToString()));
            }

            for (int i = 0; i < iterations; i++)
            {
                var scatter = target.Scatter<int, string>(i);
                scatters.Add(scatter);
            }

            for (int i = 0; i < iterations; i++)
            {
                scatters[i].TotalParticipants.Should().Be(letters.Length);
                var responses = scatters[i].GatherResponses(letters.Length).OrderBy(r => r.Value).ToArray();
                scatters[i].CompletedParticipants.Should().Be(letters.Length);
                responses.Length.Should().Be(letters.Length);
                for (int j = 0; j < letters.Length; j++)
                {
                    responses[j].IsEmpty.Should().BeFalse();
                    responses[j].IsSuccess.Should().BeTrue();
                    responses[j].Name.Should().Be(letters[j].ToString());
                    responses[j].Value.Should().Be(i.ToString() + letters[j]);
                }
            }
        }
    }
}
