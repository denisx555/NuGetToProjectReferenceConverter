using System.Collections.Generic;

namespace NuGetToProjectReferenceConverter.Services.DbgSolution
{
    public interface IDbgSolutionService
    {
        IEnumerable<EnvDTE.Project> GetProjects();
        void AddProjectToCurrentReplacedProjectsFolder(string projectPath);
        string GetSolutionDirectory();
    }
}
