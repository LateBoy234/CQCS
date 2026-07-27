using System;

namespace ThreeNH.SQCX.Application.JobDocuments
{
    public sealed class JobDocumentPersistence : IJobDocumentPersistence
    {
        private readonly IJobDocumentStore _store;

        public JobDocumentPersistence(IJobDocumentStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public string Load(string filePath)
        {
            ValidatePath(filePath);
            return _store.Read(filePath);
        }

        public void Save(string filePath, string content)
        {
            ValidatePath(filePath);
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            _store.Write(filePath, content);
        }

        private static void ValidatePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A document file path is required.", nameof(filePath));
        }
    }
}
