using System.Collections.Generic;

namespace NuGetToProjectReferenceConverter.Services.Solutions
{
    /// <summary>
    /// Interface for solution management services.
    /// Интерфейс для сервисов управления решениями.
    /// </summary>
    public interface ISolutionService
    {
        /// <summary>
        /// Gets all projects in the solution.
        /// Получает все проекты в решении.
        /// </summary>
        /// <returns>A collection of projects. Коллекция проектов.</returns>
        IEnumerable<EnvDTE.Project> GetAllProjects();

        /// <summary>
        /// Adds a project to the current replaced projects folder.
        /// Добавляет проект в текущую папку замененных проектов.
        /// </summary>
        /// <param name="projectPath">The path to the project. Путь к проекту.</param>
        void AddProjectToReplacedProjectsFolder(string projectPath, List<string> addedList);

        /// <summary>
        /// Initializes the project index before starting work.
        /// Инициализирует индекс проектов перед началом работы.
        /// </summary>
        /// <param name="rootDirectory">The root directory for indexing. Корневая директория для индексирования.</param>
        void InitializeProjectIndex(string rootDirectory);

        /// <summary>
        /// Gets the directory of the solution.
        /// Получает каталог решения.
        /// </summary>
        /// <returns>The solution directory. Каталог решения.</returns>
        string GetSolutionDirectory();
    }
}
