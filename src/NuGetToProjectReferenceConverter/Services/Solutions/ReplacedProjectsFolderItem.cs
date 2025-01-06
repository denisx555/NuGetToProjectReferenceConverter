using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;

namespace NuGetToProjectReferenceConverter.Services.Solutions
{
    /// <summary>
    /// Represents a folder for replaced projects in the solution.
    /// Представляет папку для замененных проектов в решении.
    /// </summary>
    public class ReplacedProjectsFolderItem
    {
        private readonly Project _project;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplacedProjectsFolderItem"/> class.
        /// Инициализирует новый экземпляр класса <see cref="ReplacedProjectsFolderItem"/>.
        /// </summary>
        /// <param name="project">The project representing the folder. Проект, представляющий папку.</param>
        public ReplacedProjectsFolderItem(Project project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
        }

        /// <summary>
        /// Gets the project items in the folder.
        /// Получает элементы проекта в папке.
        /// </summary>
        public IEnumerable<ProjectItem> ProjectItems
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                foreach (ProjectItem item in _project.ProjectItems)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Gets the solution folder.
        /// Получает папку решения.
        /// </summary>
        /// <returns>The solution folder. Папка решения.</returns>
        public SolutionFolder GetSolutionFolder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return (SolutionFolder)_project.Object;
        }

        /// <summary>
        /// Adds a project to the folder from a file.
        /// Добавляет проект в папку из файла.
        /// </summary>
        /// <param name="projectPath">The path to the project file. Путь к файлу проекта.</param>
        public void AddFromFile(string projectPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GetSolutionFolder().AddFromFile(projectPath);
        }
    }
}
