using System.Collections.Generic;

namespace NuGetToProjectReferenceConverter.Services.Solutions
{
    public interface ISolutionService
    {
        IEnumerable<EnvDTE.Project> GetProjects();
        void AddProjectToCurrentReplacedProjectsFolder(string projectPath);
        string GetSolutionDirectory();
    }
}
