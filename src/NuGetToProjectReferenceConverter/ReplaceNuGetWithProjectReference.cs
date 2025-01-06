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
    public class ReplaceNuGetWithProjectReference
    {
        private readonly ISolutionService _solutionService;
        private readonly IMapFileService _mapFileService;
        private readonly IPathService _pathService;

        public ReplaceNuGetWithProjectReference(ISolutionService solutionService,
            IMapFileService mapFileService,
            IPathService pathService)
        {
            _solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
            _mapFileService = mapFileService ?? throw new ArgumentNullException(nameof(mapFileService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

            _mapFileService.LoadOrCreateIfNotExists();
        }

        public void Execute()
        {
            var projects = _solutionService.GetProjects().ToArray();
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

                        // Преобразование абсолютного пути в относительный                        
                        var relativeProjectReferencePath = _pathService.ToRelativePath(Path.GetDirectoryName(project.FullName),
                            projectReferencePath);

                        projectReferences.Add(msbuildProject.AddItem("ProjectReference", relativeProjectReferencePath).First());

                        // Добавление перепривязанного проекта в решение
                        _solutionService.AddProjectToCurrentReplacedProjectsFolder(projectReferencePath);
                    }
                }

                msbuildProject.Save();
            }
        }

        private string FindProjectPathByPackageId(string packageId)
        {
            if (!_mapFileService.Get(packageId, out string result))
            {
                _mapFileService.AddOrUpdate(packageId, null);
            }

            return result;
        }
    }
}
