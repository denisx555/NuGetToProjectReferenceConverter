namespace NuGetToProjectReferenceConverter.Services.DbgMapFile
{
    public interface IDbgMapFileService
    {
        void AddOrUpdate(string packageId, string value);
        bool Get(string packageId, out string result);
        void LoadOrCreateIfNotExists();
        void Save();
    }
}
