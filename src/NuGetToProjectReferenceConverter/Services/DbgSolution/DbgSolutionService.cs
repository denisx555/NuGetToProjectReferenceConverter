using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;

namespace NuGetToProjectReferenceConverter.Services.DbgSolution
{
    public class DbgSolutionService : IDbgSolutionService
    {
        private const string ReplacedProjectsFolderName = "ReplacedProjects";

        private readonly IServiceProvider _serviceProvider;
        private ReplacedProjectsFolderItem _currentReplacedProjectsFolder = null;

        public DbgSolutionService(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _serviceProvider = serviceProvider;
        }

        public IEnumerable<EnvDTE.Project> GetProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));

            var solution = dte.Solution;

            foreach (EnvDTE.Project project in solution.Projects)
            {
                foreach (EnvDTE.Project projectResult in GetNextItemsProject(project))
                {
                    yield return projectResult;
                }
            }
        }

        private IEnumerable<EnvDTE.Project> GetNextItemsProject(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                foreach (EnvDTE.ProjectItem item in project.ProjectItems)
                {
                    if (item.SubProject != null)
                    {
                        foreach (EnvDTE.Project projectResult in GetNextItemsProject(item.SubProject))
                        {
                            yield return projectResult;
                        }
                    }
                }
            }
            else
            {
                yield return project;
            }
        }

        private ReplacedProjectsFolderItem GetCurrentReplacedProjectsFolder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_currentReplacedProjectsFolder != null)
            {
                return _currentReplacedProjectsFolder;
            }

            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
            var solution = (Solution2)dte.Solution;

            // Найти или создать папку для перепривязанных проектов
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
                _currentReplacedProjectsFolder = new ReplacedProjectsFolderItem(replacedProjectsFolder);
            }

            return _currentReplacedProjectsFolder;
        }

        public void AddProjectToCurrentReplacedProjectsFolder(string projectPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var currentReplacedProjectsFolder = GetCurrentReplacedProjectsFolder();
            var projectName = Path.GetFileNameWithoutExtension(projectPath);

            // Проверка на существование проекта в решении
            foreach (EnvDTE.ProjectItem item in currentReplacedProjectsFolder.ProjectItems)
            {
                if (item.Name == projectName)
                {
                    // Проект уже существует в папке
                    return;
                }
            }

            currentReplacedProjectsFolder.AddFromFile(projectPath);
        }

        public string GetSolutionDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
            var solutionDirectory = Path.GetDirectoryName(dte.Solution.FullName);
            return solutionDirectory;
        }
    }
}
