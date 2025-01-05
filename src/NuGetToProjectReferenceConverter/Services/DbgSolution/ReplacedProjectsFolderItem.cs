using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;

namespace NuGetToProjectReferenceConverter.Services.DbgSolution
{
    public class ReplacedProjectsFolderItem
    {
        private readonly Project _project;

        public ReplacedProjectsFolderItem(Project project)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
        }

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

        public SolutionFolder GetSolutionFolder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return (SolutionFolder)_project.Object;
        }

        public void AddFromFile(string projectPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            GetSolutionFolder().AddFromFile(projectPath);
        }
    }
}
