using System;
using System.IO;

namespace NuGetToProjectReferenceConverter.Services.Paths
{
    /// <summary>
    /// Provides methods to convert between absolute and relative paths.
    /// Предоставляет методы для преобразования между абсолютными и относительными путями.
    /// </summary>
    public class PathService : IPathService
    {
        private readonly bool _checkPathExists;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathService"/> class.
        /// Инициализирует новый экземпляр класса <see cref="PathService"/>.
        /// </summary>
        /// <param name="checkPathExists">If set to <c>true</c>, checks if the path exists. Если установлено значение <c>true</c>, проверяет, существует ли путь.</param>
        public PathService(bool checkPathExists = true)
        {
            _checkPathExists = checkPathExists;
        }

        /// <summary>
        /// Converts an absolute path to a relative path.
        /// Преобразует абсолютный путь в относительный путь.
        /// </summary>
        /// <param name="baseAbsolutePath">The base absolute path. Основной абсолютный путь.</param>
        /// <param name="targetAbsolutePath">The absolute path to convert. Абсолютный путь для преобразования.</param>
        /// <returns>The relative path. Относительный путь.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the base absolute path does not exist and <paramref name="checkPathExists"/> is <c>true</c>. Выбрасывается, когда основной абсолютный путь не существует и <paramref name="checkPathExists"/> равно <c>true</c>.</exception>
        public string ToRelativePath(string baseAbsolutePath, string targetAbsolutePath)
        {
            if (_checkPathExists && !Directory.Exists(baseAbsolutePath))
            {
                throw new DirectoryNotFoundException($"The directory '{baseAbsolutePath}' does not exist.");
            }

            if (string.IsNullOrEmpty(targetAbsolutePath))
            {
                return targetAbsolutePath;
            }

            var baseUri = new Uri(baseAbsolutePath.EndsWith(Path.DirectorySeparatorChar.ToString()) 
                ? baseAbsolutePath 
                : baseAbsolutePath + Path.DirectorySeparatorChar);

            var targetUri = new Uri(targetAbsolutePath.EndsWith(Path.DirectorySeparatorChar.ToString()) 
                ? targetAbsolutePath 
                : targetAbsolutePath + Path.DirectorySeparatorChar);

            if (baseUri == targetUri)
            {
                return string.Empty;
            }

            var relativeUri = baseUri.MakeRelativeUri(targetUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Converts a relative path to an absolute path.
        /// Преобразует относительный путь в абсолютный путь.
        /// </summary>
        /// <param name="baseAbsolutePath">The base absolute path. Основной абсолютный путь.</param>
        /// <param name="relativePath">The relative path to convert. Относительный путь для преобразования.</param>
        /// <returns>The absolute path. Абсолютный путь.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the base absolute path does not exist and <paramref name="checkPathExists"/> is <c>true</c>. Выбрасывается, когда основной абсолютный путь не существует и <paramref name="checkPathExists"/> равно <c>true</c>.</exception>
        public string ToAbsolutePath(string baseAbsolutePath, string relativePath)
        {
            if (_checkPathExists && !Directory.Exists(baseAbsolutePath))
            {
                throw new DirectoryNotFoundException($"The directory '{baseAbsolutePath}' does not exist.");
            }

            if (string.IsNullOrEmpty(relativePath))
            {                
                return baseAbsolutePath;
            }

            var absolutePath = Path.GetFullPath(Path.Combine(baseAbsolutePath, relativePath));
            return absolutePath;
        }
    }
}