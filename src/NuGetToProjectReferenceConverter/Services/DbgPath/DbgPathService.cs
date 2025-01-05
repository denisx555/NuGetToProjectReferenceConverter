using System;
using System.IO;

namespace NuGetToProjectReferenceConverter.Services.DbgPath
{
    /// <summary>
    /// Provides methods to convert between absolute and relative paths.
    /// Предоставляет методы для преобразования между абсолютными и относительными путями.
    /// </summary>
    public class DbgPathService : IDbgPathService
    {
        private readonly string _mainAbsolutePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="DbgPathService"/> class.
        /// Инициализирует новый экземпляр класса <see cref="DbgPathService"/>.
        /// </summary>
        /// <param name="mainAbsolutePath">The main absolute path. Основной абсолютный путь.</param>
        /// <param name="checkPathExists">If set to <c>true</c>, checks if the path exists. Если установлено значение <c>true</c>, проверяет, существует ли путь.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="mainAbsolutePath"/> is null or empty. Выбрасывается, когда <paramref name="mainAbsolutePath"/> равно null или пусто.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist and <paramref name="checkPathExists"/> is <c>true</c>. Выбрасывается, когда каталог не существует и <paramref name="checkPathExists"/> равно <c>true</c>.</exception>
        public DbgPathService(string mainAbsolutePath, bool checkPathExists = true)
        {
            if (string.IsNullOrEmpty(mainAbsolutePath))
            {
                throw new ArgumentNullException(nameof(mainAbsolutePath), "mainAbsolutePath cannot be null or empty.");
            }

            if (checkPathExists && !Directory.Exists(mainAbsolutePath))
            {
                throw new DirectoryNotFoundException($"The directory '{mainAbsolutePath}' does not exist.");
            }

            _mainAbsolutePath = mainAbsolutePath;
        }

        /// <summary>
        /// Converts an absolute path to a relative path.
        /// Преобразует абсолютный путь в относительный путь.
        /// </summary>
        /// <param name="absolutePath">The absolute path to convert. Абсолютный путь для преобразования.</param>
        /// <returns>The relative path. Относительный путь.</returns>
        public string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return absolutePath;
            }

            var mainPathUri = new Uri(_mainAbsolutePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? _mainAbsolutePath : _mainAbsolutePath + Path.DirectorySeparatorChar);
            var absolutePathUri = new Uri(absolutePath.EndsWith(Path.DirectorySeparatorChar.ToString()) ? absolutePath : absolutePath + Path.DirectorySeparatorChar);

            if (mainPathUri == absolutePathUri)
            {
                return string.Empty;
            }

            var relativeUri = mainPathUri.MakeRelativeUri(absolutePathUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Converts a relative path to an absolute path.
        /// Преобразует относительный путь в абсолютный путь.
        /// </summary>
        /// <param name="relativePath">The relative path to convert. Относительный путь для преобразования.</param>
        /// <returns>The absolute path. Абсолютный путь.</returns>
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
