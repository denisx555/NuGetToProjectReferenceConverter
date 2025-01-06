using System.IO;
using Xunit;

namespace NuGetToProjectReferenceConverter.Services.Paths.Tests
{
    public class PathServiceTests
    {
        [Theory]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source\\", "C:\\!dbg\\Sezal\\Sezal.Core", "..\\..\\Sezal\\Sezal.Core")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source", "C:\\!dbg\\Sezal\\Sezal.Core\\", "..\\..\\Sezal\\Sezal.Core")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source\\", "C:\\!dbg\\Sezal\\Sezal.Core\\", "..\\..\\Sezal\\Sezal.Core")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source", "C:\\!dbg\\Sezal\\Sezal.Core", "..\\..\\Sezal\\Sezal.Core")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source", "C:\\!dbg\\T2Plus.Spm\\Source", "")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source", "C:\\!dbg\\T2Plus.Spm\\Source\\", "")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source\\", "C:\\!dbg\\T2Plus.Spm\\Source", "")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source", "", "")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source\\", "", "")]
        public void ToRelativePathTest(string mainAbsolutePath, string relativePath, string expectedRelativePath)
        {
            // Arrange
            var pathService = new PathService(false);
            string absolutePath = Path.Combine(mainAbsolutePath, relativePath);

            // Act
            string result = pathService.ToRelativePath(mainAbsolutePath, absolutePath);

            // Assert
            Assert.Equal(expectedRelativePath, result);
        }

        [Theory]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source\\", "SubFolder\\File.txt", "C:\\!dbg\\T2Plus.Spm\\Source\\SubFolder\\File.txt")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source", "SubFolder\\File.txt", "C:\\!dbg\\T2Plus.Spm\\Source\\SubFolder\\File.txt")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source\\", "", "C:\\!dbg\\T2Plus.Spm\\Source\\")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source", "", "C:\\!dbg\\T2Plus.Spm\\Source")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source", "..\\AnotherFolder\\AnotherFile.txt", "C:\\!dbg\\T2Plus.Spm\\AnotherFolder\\AnotherFile.txt")]
        [InlineData("C:\\!dbg\\T2Plus.Spm\\Source\\", "..\\AnotherFolder\\AnotherFile.txt", "C:\\!dbg\\T2Plus.Spm\\AnotherFolder\\AnotherFile.txt")]
        public void ToAbsolutePathTest(string mainAbsolutePath, string relativePath, string expectedAbsolutePath)
        {
            // Arrange
            var pathService = new PathService(false);

            // Act
            string result = pathService.ToAbsolutePath(mainAbsolutePath, relativePath);

            // Assert
            Assert.Equal(expectedAbsolutePath, result);
        }
    }
}