namespace NuGetToProjectReferenceConverter.Services.DbgPath
{
    public interface IDbgPathService
    {
        string ToAbsolutePath(string value);
        string ToRelativePath(string value);
    }
}
