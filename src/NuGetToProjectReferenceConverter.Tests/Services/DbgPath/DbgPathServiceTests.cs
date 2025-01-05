using System.IO;
using Xunit;

namespace NuGetToProjectReferenceConverter.Services.DbgPath.Tests
{
    public class DbgPathServiceTests
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
            var _dbgPathService = new DbgPathService(mainAbsolutePath, false);
            string absolutePath = Path.Combine(mainAbsolutePath, relativePath);

            // Act
            string result = _dbgPathService.ToRelativePath(absolutePath);

            // Assert
            Assert.Equal(expectedRelativePath, result);
        }
    }
}