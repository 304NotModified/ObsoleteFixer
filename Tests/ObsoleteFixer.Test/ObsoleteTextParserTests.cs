using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ObsoleteFixer.Test
{
    [TestClass]
    public class ObsoleteTextParserTests
    {
        [DataTestMethod]
        [DataRow("", null)]
        [DataRow(null, null)]
        [DataRow("use `a`", null)] // no supported
        [DataRow("replace with `a`", "a")]
        [DataRow("replace with 'a'", null)] //no backticks
        [DataRow("replace with `a'", null)] //no backticks 2
        [DataRow("replace with: `a`", "a")]
        [DataRow("replace with`a`", "a")]
        [DataRow("replace with`a`sometext", "a")]
        public void FindReplaceWithValue_StateUnderTest_ExpectedBehavior(string input, string expected)
        {
            // Arrange

            // Act
            var result = ObsoleteTextParser.FindReplaceWithValue(input);

            // Assert
            Assert.AreEqual(expected, result);
        }
    }
}
