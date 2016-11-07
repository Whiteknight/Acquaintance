using Acquaintance.Utility;
using FluentAssertions;
using NUnit.Framework;

namespace Acquaintance.Tests.Utility
{
    [TestFixture]
    public class StringTrieTests
    {
        [Test]
        public void InsertGet_Test()
        {
            var target = new StringTrie<int>();
            target.GetOrInsert("a", new string[0], () => 1);
            target.GetOrInsert("a", new[] { "b", }, () => 2);
            target.GetOrInsert("a", new[] { "b", "c" }, () => 3);
            target.GetOrInsert("a", new[] { "b", "c" }, () => 4);
            target.GetOrInsert("a", new[] { "b", "d" }, () => 5);

            target.Get("a", new string[] { }).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(1);
            target.Get("a", new[] { "b" }).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(2);
            target.Get("a", new[] { "b", "c" }).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(3);
            target.Get("a", new[] { "b", "d" }).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(5);
        }

        [Test]
        public void InsertGet_Wildcards_Second()
        {
            var target = new StringTrie<int>();
            target.GetOrInsert("a", new[] { "a", "c" }, () => 1);
            target.GetOrInsert("a", new[] { "a", "d" }, () => 2);
            target.GetOrInsert("a", new[] { "b", "c" }, () => 3);
            target.GetOrInsert("a", new[] { "b", "d" }, () => 4);
            target.GetOrInsert("a", new[] { "x", "c" }, () => 9);
            target.GetOrInsert("x", new[] { "x", "d" }, () => 9);

            target.Get("a", new[] { "*", "c" }).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(1, 3, 9);
        }

        [Test]
        public void InsertGet_Wildcards_Third()
        {
            var target = new StringTrie<int>();
            target.GetOrInsert("a", new[] { "a", "c" }, () => 1);
            target.GetOrInsert("a", new[] { "a", "d" }, () => 2);
            target.GetOrInsert("a", new[] { "b", "c" }, () => 3);
            target.GetOrInsert("a", new[] { "b", "d" }, () => 4);
            target.GetOrInsert("a", new[] { "x", "c" }, () => 9);
            target.GetOrInsert("a", new[] { "x", "d" }, () => 9);

            target.Get("a", new[] { "b", "*" }).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(3, 4);
        }

        [Test]
        public void InsertGet_Wildcards_SecondAndThird()
        {
            var target = new StringTrie<int>();
            target.GetOrInsert("a", new[] { "a", "c" }, () => 1);
            target.GetOrInsert("a", new[] { "a", "d" }, () => 2);
            target.GetOrInsert("a", new[] { "b", "c" }, () => 3);
            target.GetOrInsert("a", new[] { "b", "d" }, () => 4);

            target.Get("a", new[] { "*", "*" }).Should().OnlyHaveUniqueItems().And.BeEquivalentTo(1, 2, 3, 4);
        }
    }
}
