using System.IO;
using System.Text;
using ThreeNH.SQCX.Application.JobDocuments;

namespace ThreeNH.SQCX.Infrastructure.Persistence
{
    public sealed class FileSystemJobDocumentStore : IJobDocumentStore
    {
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public string Read(string filePath)
        {
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        public void Write(string filePath, string content)
        {
            File.WriteAllText(filePath, content, Utf8WithoutBom);
        }
    }
}
