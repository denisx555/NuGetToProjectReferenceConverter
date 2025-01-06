using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell;
using NuGetToProjectReferenceConverter.Services.MapFile;
using NuGetToProjectReferenceConverter.Services.Paths;
using NuGetToProjectReferenceConverter.Services.Solutions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectItem = Microsoft.Build.Evaluation.ProjectItem;

namespace NuGetToProjectReferenceConverter
{
    /// <summary>
    /// Provides functionality to replace NuGet package references with project references.
    /// Предоставляет функциональность для замены ссылок на пакеты NuGet на ссылки на проекты.
    /// </summary>
    public class NuGetToProjectReferenceConverter
    {
        private readonly ISolutionService _solutionService;
        private readonly IMapFileService _mapFileService;
        private readonly IPathService _pathService;

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetToProjectReferenceConverter"/> class.
        /// Инициализирует новый экземпляр класса <see cref="NuGetToProjectReferenceConverter"/>.
        /// </summary>
        /// <param name="solutionService">The solution service. Сервис решения.</param>
        /// <param name="mapFileService">The map file service. Сервис файла карты.</param>
        /// <param name="pathService">The path service. Сервис путей.</param>
        public NuGetToProjectReferenceConverter(ISolutionService solutionService,
            IMapFileService mapFileService,
            IPathService pathService)
        {
            _solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
            _mapFileService = mapFileService ?? throw new ArgumentNullException(nameof(mapFileService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

            _mapFileService.LoadOrCreateIfNotExists();
        }

        /// <summary>
        /// Executes the conversion of NuGet package references to project references.
        /// Выполняет преобразование ссылок на пакеты NuGet в ссылки на проекты.
        /// </summary>
        public void Execute()
        {
            var projects = _solutionService.GetAllProjects().ToArray();
            foreach (var project in projects)
            {
                ReplaceNuGetReferencesWithProjectReferences(project);
            }

            _mapFileService.Save();
        }

        private void ReplaceNuGetReferencesWithProjectReferences(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            if (string.IsNullOrWhiteSpace(project.FullName))
            {
                return;
            }

            if (project.Kind != EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                var projectCollection = new ProjectCollection();
                var msbuildProject = projectCollection.LoadProject(project.FullName);

                var packageReferences = msbuildProject.GetItems("PackageReference").ToList();
                var projectReferences = new List<ProjectItem>();

                foreach (var packageReference in packageReferences)
                {
                    var packageId = packageReference.EvaluatedInclude;
                    var projectReferencePath = FindProjectPathByPackageId(packageId);

                    if (!string.IsNullOrEmpty(projectReferencePath))
                    {
                        msbuildProject.RemoveItem(packageReference);

                        // Convert absolute path to relative path
                        var relativeProjectReferencePath = _pathService.ToRelativePath(Path.GetDirectoryName(project.FullName),
                            projectReferencePath);

                        projectReferences.Add(msbuildProject.AddItem("ProjectReference", relativeProjectReferencePath).First());

                        // Add the re-referenced project to the solution
                        _solutionService.AddProjectToReplacedProjectsFolder(projectReferencePath);
                    }
                }

                msbuildProject.Save();
            }
        }

        private string FindProjectPathByPackageId(string packageId)
        {
            if (!_mapFileService.Get(packageId, out string projectPath))
            {
                _mapFileService.AddOrUpdate(packageId, null);
            }

            return projectPath;
        }
    }
}
