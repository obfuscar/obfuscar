using Obfuscar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ObfuscarTests
{
    public class FilterTests
    {
        [Theory]
        [InlineData("+[XXX]", true)]
        [InlineData("-[XXX]", true)]
        [InlineData("+[XXX] -[YYY]", true)]
        [InlineData("[XXX]", false)]
        public void IsFilter(string value, bool expected)
        {
            Assert.Equal(expected, Filter.TryGetFilter(Environment.CurrentDirectory, value) != null);
        }

        [Fact]
        public void FullPaths()
        {
            // Arrange
            var sut = Filter.TryGetFilter(Environment.CurrentDirectory, $"+[{Path.Combine(Environment.CurrentDirectory, "*.*")}]");

            // Act
            var files = sut.ToList();

            // Assert
            Assert.Equal(
                Directory.EnumerateFiles(Environment.CurrentDirectory, "*.*").Select(f => Path.GetFullPath(f)),
                files);
        }

        [Fact]
        public void RelativePaths()
        {
            // Arrange
            var backAndForthRelativePath = Path.Combine("..", Path.GetFileName(Environment.CurrentDirectory));
            var sut = Filter.TryGetFilter(".", $"+[{Path.Combine(backAndForthRelativePath, "*.*")}]");

            // Act
            var files = sut.ToList();

            // Assert
            Assert.Equal(
                Directory.EnumerateFiles(Environment.CurrentDirectory, "*.*").Select(f => Path.GetFullPath(f)),
                files);
        }

        [Fact]
        public void Exclusion()
        {
            // Arrange
            var expected = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.*").Select(f => Path.GetFullPath(f)).ToList();
            var backAndForthRelativePath = Path.Combine("..", Path.GetFileName(Environment.CurrentDirectory));
            var oneFile = expected[0];
            expected.RemoveAt(0);
            var sut = Filter.TryGetFilter(".", $"+[{Path.Combine(backAndForthRelativePath, "*.*")}] -[{oneFile}]");

            // Act
            var files = sut.ToList();

            // Assert
            Assert.Equal(expected, files);
        }
    }
}
