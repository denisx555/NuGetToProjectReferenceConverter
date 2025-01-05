using System;
using System.IO;

namespace NuGetToProjectReferenceConverter.Services.DbgPath
{
    public interface IDbgPathService
    {
        string ToAbsolutePath(string value);
        string ToRelativePath(string value);
    }

    public class DbgPathService : IDbgPathService
    {
        private readonly string _mainAbsolutePath;

        public DbgPathService(string mainAbsolutePath)
        {
            if (string.IsNullOrWhiteSpace(mainAbsolutePath))
            {
                throw new ArgumentException($"\"{nameof(mainAbsolutePath)}\" не может быть пустым или содержать только пробел.", nameof(mainAbsolutePath));
            }

            _mainAbsolutePath = mainAbsolutePath;
        }

        public string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return absolutePath;
            }

            var solutionUri = new Uri(_mainAbsolutePath);
            var pathUri = new Uri(absolutePath);

            var relativeUri = solutionUri.MakeRelativeUri(pathUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        public string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return relativePath;
            }

            var absolutePath = Path.GetFullPath(Path.Combine(_mainAbsolutePath, relativePath));
            return absolutePath;
        }
    }
}
