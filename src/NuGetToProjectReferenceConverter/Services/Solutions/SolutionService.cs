using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell;
using NuGetToProjectReferenceConverter.Services.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGetToProjectReferenceConverter.Services.Solutions
{
    /// <summary>
    /// Provides methods to manage the solution.
    /// Предоставляет методы для управления решением.
    /// </summary>
    public class SolutionService : ISolutionService
    {
        private const string ReplacedProjectsFolderName = "!ReplacedProjects";

        private readonly IServiceProvider _serviceProvider;
        private readonly IPathService _pathService;
        private ReplacedProjectsFolderItem _replacedProjectsFolder = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionService"/> class.
        /// Инициализирует новый экземпляр класса <see cref="SolutionService"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider. Поставщик услуг.</param>
        /// <param name="pathService">The path service. Сервис путей.</param>
        public SolutionService(IServiceProvider serviceProvider, IPathService pathService)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
        }

        /// <summary>
        /// Gets all projects in the solution.
        /// Получает все проекты в решении.
        /// </summary>
        /// <returns>A collection of projects. Коллекция проектов.</returns>
        public IEnumerable<EnvDTE.Project> GetAllProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
            var solution = dte.Solution;

            foreach (EnvDTE.Project project in solution.Projects)
            {
                foreach (EnvDTE.Project projectResult in GetSubProjects(project))
                {
                    yield return projectResult;
                }
            }
        }

        /// <summary>
        /// Gets sub-projects from a project folder.
        /// Получает подпроекты из папки проекта.
        /// </summary>
        /// <param name="project">The project to get sub-projects from. Проект для получения подпроектов.</param>
        /// <returns>An enumerable of sub-projects. Перечисление подпроектов.</returns>
        private IEnumerable<EnvDTE.Project> GetSubProjects(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                foreach (EnvDTE.ProjectItem item in project.ProjectItems)
                {
                    if (item.SubProject != null)
                    {
                        foreach (EnvDTE.Project subProject in GetSubProjects(item.SubProject))
                        {
                            yield return subProject;
                        }
                    }
                }
            }
            else
            {
                yield return project;
            }
        }

        /// <summary>
        /// Gets or creates the replaced projects folder in the solution.
        /// Получает или создаёт папку заменённых проектов в решении.
        /// </summary>
        /// <returns>The replaced projects folder item. Элемент папки заменённых проектов.</returns>
        private ReplacedProjectsFolderItem GetReplacedProjectsFolder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_replacedProjectsFolder != null)
            {
                return _replacedProjectsFolder;
            }

            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
            var solution = (Solution2)dte.Solution;

            // Find or create the folder for replaced projects
            EnvDTE.Project replacedProjectsFolder = null;
            foreach (EnvDTE.Project project in solution.Projects)
            {
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && project.Name == ReplacedProjectsFolderName)
                {
                    replacedProjectsFolder = project;
                    break;
                }
            }

            if (replacedProjectsFolder == null)
            {
                replacedProjectsFolder = solution.AddSolutionFolder(ReplacedProjectsFolderName);
            }

            _replacedProjectsFolder = new ReplacedProjectsFolderItem(replacedProjectsFolder);
            return _replacedProjectsFolder;
        }

        /// <summary>
        /// Adds a project to the current replaced projects folder.
        /// Добавляет проект в текущую папку заменённых проектов.
        /// </summary>
        /// <param name="projectPath">The path to the project. Путь к проекту.</param>
        /// <param name="addedList">The list of added projects. Список добавленных проектов.</param>
        public void AddProjectToReplacedProjectsFolder(string projectPath, List<string> addedList)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (addedList is null)
            {
                throw new ArgumentNullException(nameof(addedList));
            }

            if (!File.Exists(projectPath))
            {
                throw new FileNotFoundException($"Файл проекта не существует: {projectPath}");
            }

            var projectName = Path.GetFileNameWithoutExtension(projectPath);
            var replacedProjectsFolder = GetReplacedProjectsFolder();

            // Check if the project already exists in the folder
            foreach (EnvDTE.ProjectItem item in replacedProjectsFolder.ProjectItems)
            {
                if (item.Name == projectName)
                {
                    return;
                }
            }

            replacedProjectsFolder.AddFromFile(projectPath);
            addedList.Add(projectPath);

            using (var projectCollection = new ProjectCollection())
            {
                var subProject = projectCollection.LoadProject(projectPath);
                var items = subProject.GetItems("ProjectReference").ToArray();

                foreach (var item in items)
                {
                    // Путь уже должен быть абсолютным
                    string subProjectAbsolutePath = item.EvaluatedInclude;

                    if (!File.Exists(subProjectAbsolutePath))
                    {
                        continue;
                    }

                    AddProjectToReplacedProjectsFolder(subProjectAbsolutePath, addedList);
                }
            }
        }

        /// <summary>
        /// Gets the directory of the solution.
        /// Получает каталог решения.
        /// </summary>
        /// <returns>The solution directory. Каталог решения.</returns>
        public string GetSolutionDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
            var solutionDir = Path.GetDirectoryName(dte.Solution.FullName);
            return solutionDir;
        }
    }
}
