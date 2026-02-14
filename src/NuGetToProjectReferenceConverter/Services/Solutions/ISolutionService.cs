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
        /// Добавляет проект в текущую папку заменённых проектов.
        /// </summary>
        /// <param name="projectPath">The path to the project. Путь к проекту.</param>
        /// <param name="addedList">The list of added projects. Список добавленных проектов.</param>
        void AddProjectToReplacedProjectsFolder(string projectPath, List<string> addedList);

        /// <summary>
        /// Gets the directory of the solution.
        /// Получает каталог решения.
        /// </summary>
        /// <returns>The solution directory. Каталог решения.</returns>
        string GetSolutionDirectory();
    }
}
