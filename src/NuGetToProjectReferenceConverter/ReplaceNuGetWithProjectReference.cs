﻿using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell;
using NuGetToProjectReferenceConverter.Services.DbgMapFile;
using NuGetToProjectReferenceConverter.Services.DbgSolution;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectItem = Microsoft.Build.Evaluation.ProjectItem;

namespace NuGetToProjectReferenceConverter
{
    public class ReplaceNuGetWithProjectReference
    {
        private readonly IDbgSolutionService _dbgSolutionService;
        private readonly IDbgMapFileService _dbgMapFileService;

        public ReplaceNuGetWithProjectReference(IDbgSolutionService dbgSolutionService, IDbgMapFileService dbgMapFileService)
        {
            _dbgSolutionService = dbgSolutionService ?? throw new ArgumentNullException(nameof(dbgSolutionService));
            _dbgMapFileService = dbgMapFileService ?? throw new ArgumentNullException(nameof(dbgMapFileService));

            _dbgMapFileService.LoadOrCreateIfNotExists();
        }

        public void Execute()
        {
            var projects = _dbgSolutionService.GetProjects().ToArray();
            foreach (var project in projects)
            {
                ReplaceNuGetReferencesWithProjectReferences(project);
            }

            _dbgMapFileService.Save();
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
                        var relativeProjectReferencePath = GetRelativePath(project.FullName, projectReferencePath);

                        projectReferences.Add(msbuildProject.AddItem("ProjectReference", relativeProjectReferencePath).First());

                        // Добавление перепривязанного проекта в решение
                        _dbgSolutionService.AddProjectToCurrentReplacedProjectsFolder(projectReferencePath);
                    }
                }

                msbuildProject.Save();
            }
        }

        private string FindProjectPathByPackageId(string packageId)
        {
            if (!_dbgMapFileService.Get(packageId, out string result))
            {
                _dbgMapFileService.AddOrUpdate(packageId, null);
            }

            return result;
        }

        private string GetRelativePath(string fromPath, string toPath)
        {
            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
