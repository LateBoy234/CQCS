namespace ThreeNH.SQCX.Application.JobDocuments
{
    public interface IJobDocumentStore
    {
        string Read(string filePath);
        void Write(string filePath, string content);
    }
}
