namespace ThreeNH.SQCX.Application.JobDocuments
{
    public interface IJobDocumentPersistence
    {
        string Load(string filePath);
        void Save(string filePath, string content);
    }
}
