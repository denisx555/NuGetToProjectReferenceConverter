using System;

namespace NuGetToProjectReferenceConverter.Services.Indexing
{
    /// <summary>
    /// Сервис для индексирования проектов в файловой системе
    /// </summary>
    public interface IProjectIndexService
    {
        /// <summary>
        /// Строит индекс всех проектов в указанной директории
        /// </summary>
        /// <param name="rootDirectory">Корневая директория для индексирования</param>
        void BuildIndex(string rootDirectory);
        
        /// <summary>
        /// Находит путь к проекту по имени
        /// </summary>
        /// <param name="projectName">Имя проекта (без расширения)</param>
        /// <returns>Полный путь к файлу проекта или null, если не найден</returns>
        string FindProject(string projectName);
        
        /// <summary>
        /// Проверяет, был ли построен индекс
        /// </summary>
        bool IsIndexBuilt { get; }
        
        /// <summary>
        /// Очищает индекс
        /// </summary>
        void ClearIndex();
        
        /// <summary>
        /// Получает статистику индекса
        /// </summary>
        ProjectIndexStats GetStats();
    }
    
    /// <summary>
    /// Статистика индекса проектов
    /// </summary>
    public class ProjectIndexStats
    {
        /// <summary>
        /// Общее количество проектов в индексе
        /// </summary>
        public int TotalProjects { get; set; }
        
        /// <summary>
        /// Количество проиндексированных директорий
        /// </summary>
        public int IndexedDirectories { get; set; }
        
        /// <summary>
        /// Время построения индекса
        /// </summary>
        public TimeSpan BuildTime { get; set; }
        
        /// <summary>
        /// Корневая директория индексирования
        /// </summary>
        public string RootDirectory { get; set; }
    }
}