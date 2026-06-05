namespace gezzyn.Domain.Interfaces
{
    public interface IMeiliSearchService
    {
        Task AddOrUpdateDocuments<T>(T[] documents, string indexName, string primaryKey = "id");
        Task<List<T>> Search<T>(string query, string indexName);
        Task<List<T>> GetAllDocumentsAsync<T>(string indexName);
        Task DeleteDocumentAsync(string indexName, string documentId);
        Task DeleteAllDocumentsAsync(string indexName);
        Task DeleteIndexAndDocumentsAsync(string indexName);
    }
}
