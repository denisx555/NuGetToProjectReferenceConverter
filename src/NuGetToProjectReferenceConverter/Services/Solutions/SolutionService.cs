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
        /// Добавляет проект в текущую папку замененных проектов.
        /// </summary>
        /// <param name="projectPath">The path to the project. Путь к проекту.</param>
        public void AddProjectToReplacedProjectsFolder(string projectPath, List<string> addedList)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (addedList is null)
            {
                throw new ArgumentNullException(nameof(addedList));
            }

            var replacedProjectsFolder = GetReplacedProjectsFolder();
            var projectName = Path.GetFileNameWithoutExtension(projectPath);

            // Check if the project already exists in the folder
            foreach (EnvDTE.ProjectItem item in replacedProjectsFolder.ProjectItems)
            {
                if (item.Name == projectName)
                {
                    // Project already exists in the folder
                    return;
                }
            }

            replacedProjectsFolder.AddFromFile(projectPath);
            addedList.Add(projectPath);

            using (var projectCollection = new ProjectCollection())
            {
                var subProject = projectCollection.LoadProject(projectPath);
                var items = subProject.GetItems("ProjectReference").ToArray();
                var subProjectPath = Path.GetDirectoryName(subProject.FullPath);

                foreach (var item in items)
                {
                    var subProjectAbsolutePath = _pathService.ToAbsolutePath(subProjectPath, item.EvaluatedInclude);
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
            return Path.GetDirectoryName(dte.Solution.FullName);
        }
    }
}
