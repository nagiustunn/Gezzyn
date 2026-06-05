using gezzyn.Domain.Interfaces;
using Meilisearch;
using Microsoft.Extensions.Configuration;

namespace gezzyn.Infrastructure.Services
{
    public class MeiliSearchService : IMeiliSearchService
    {
        private readonly MeilisearchClient _client;
        private readonly IConfiguration _configuration;
        public MeiliSearchService(IConfiguration configuration)
        {
            _configuration = configuration;

            var url = _configuration["MeiliSearch:Url"];
            var apiKey = _configuration["MeiliSearch:ApiKey"];

            _client = new MeilisearchClient(url, apiKey);
        }

        public async Task AddOrUpdateDocuments<T>(T[] documents, string indexName, string primaryKey = "id")
        {
            try
            {
                var index = _client.Index(indexName);

                var indexExists = await _client.GetIndexAsync(indexName) != null;

                if (!indexExists)
                {
                    await _client.CreateIndexAsync(indexName, primaryKey);
                    await index.AddDocumentsAsync(documents);
                }
                else
                {
                    await index.UpdateDocumentsAsync(documents);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MeiliSearch Error: {ex.Message}");
                throw;
            }
        }


        public async Task<List<T>> GetAllDocumentsAsync<T>(string indexName)
        {
            var index = _client.Index(indexName);
            var documents = await index.GetDocumentsAsync<T>();
            return (List<T>)documents.Results;
        }

        public async Task<List<T>> Search<T>(string query, string indexName)
        {
            var index = _client.Index(indexName);
            var result = await index.SearchAsync<T>(query);
            return (List<T>)result.Hits;
        }

        public async Task DeleteDocumentAsync(string indexName, string documentId)
        {
            var index = _client.Index(indexName);
            await index.DeleteOneDocumentAsync(documentId);
        }

        public async Task DeleteAllDocumentsAsync(string indexName)
        {
            var index = _client.Index(indexName);
            await index.DeleteAllDocumentsAsync();
        }

        public async Task DeleteIndexAndDocumentsAsync(string indexName)
        {
            var index = _client.Index(indexName);
            await index.DeleteAllDocumentsAsync();
            await _client.DeleteIndexAsync(indexName);
        }
    }
}
