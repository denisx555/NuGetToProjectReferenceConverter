namespace NuGetToProjectReferenceConverter.Services.Paths
{
    /// <summary>
    /// Interface for path conversion services.
    /// Интерфейс для сервисов преобразования путей.
    /// </summary>
    public interface IPathService
    {
        /// <summary>
        /// Converts a relative path to an absolute path.
        /// Преобразует относительный путь в абсолютный путь.
        /// </summary>
        /// <param name="baseAbsolutePath">The base absolute path. Основной абсолютный путь.</param>
        /// <param name="relativePath">The relative path to convert. Относительный путь для преобразования.</param>
        /// <returns>The absolute path. Абсолютный путь.</returns>
        string ToAbsolutePath(string baseAbsolutePath, string relativePath);

        /// <summary>
        /// Converts an absolute path to a relative path.
        /// Преобразует абсолютный путь в относительный путь.
        /// </summary>
        /// <param name="baseAbsolutePath">The base absolute path. Основной абсолютный путь.</param>
        /// <param name="targetAbsolutePath">The absolute path to convert. Абсолютный путь для преобразования.</param>
        /// <returns>The relative path. Относительный путь.</returns>
        string ToRelativePath(string baseAbsolutePath, string targetAbsolutePath);
    }
}