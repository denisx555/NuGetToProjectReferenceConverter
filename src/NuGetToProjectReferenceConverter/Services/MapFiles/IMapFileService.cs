namespace NuGetToProjectReferenceConverter.Services.MapFile
{
    /// <summary>
    /// Interface for map file services.
    /// Интерфейс для сервисов работы с файлом карты.
    /// </summary>
    public interface IMapFileService
    {
        /// <summary>
        /// Adds or updates a mapping entry.
        /// Добавляет или обновляет запись в карте.
        /// </summary>
        /// <param name="packageId">The package ID. Идентификатор пакета.</param>
        /// <param name="projectPath">The project path. Путь к проекту.</param>
        void AddOrUpdate(string packageId, string projectPath);

        /// <summary>
        /// Gets a project path by package ID.
        /// Получает путь к проекту по идентификатору пакета.
        /// </summary>
        /// <param name="packageId">The package ID. Идентификатор пакета.</param>
        /// <param name="projectPath">The project path. Путь к проекту.</param>
        /// <returns>True if the package ID exists, otherwise false. Возвращает true, если идентификатор пакета существует, иначе false.</returns>
        bool Get(string packageId, out string projectPath);

        /// <summary>
        /// Loads the map file or creates it if it does not exist.
        /// Загружает файл карты или создает его, если он не существует.
        /// </summary>
        void LoadOrCreateIfNotExists();

        /// <summary>
        /// Saves the map file.
        /// Сохраняет файл карты.
        /// </summary>
        void Save();
    }
}