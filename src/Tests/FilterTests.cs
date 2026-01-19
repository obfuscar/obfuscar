using Obfuscar;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ObfuscarTests
{
    public class FilterTests
    {
        [Fact]
        public void FullPaths()
        {
            // Arrange
            var sut = new Filter(
                Environment.CurrentDirectory,
                new[] { Path.Combine(Environment.CurrentDirectory, "*.*") },
                new string[0]);

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
            var sut = new Filter(
                Environment.CurrentDirectory,
                new[] { Path.Combine(backAndForthRelativePath, "*.*") },
                new string[0]);

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
            var sut = new Filter(
                Environment.CurrentDirectory,
                new[] { Path.Combine(backAndForthRelativePath, "*.*") },
                new[] { oneFile });

            // Act
            var files = sut.ToList();

            // Assert
            Assert.Equal(expected, files);
        }
    }
}
