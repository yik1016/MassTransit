﻿namespace MassTransit.DocumentDbIntegration.Tests.Saga.Data
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Newtonsoft.Json;


    public sealed class SagaRepository
    {
        static readonly SagaRepository _instance = new SagaRepository();

        public static readonly SagaRepository Instance = _instance;

        public static string DatabaseName = "sagaTest";
        public static string CollectionName = "sagas";

        public static JsonSerializerSettings JsonSerializerSettings;

        readonly DocumentClient _documentClient = new DocumentClient(Configuration.EndpointUri, Configuration.Key);

        SagaRepository()
        {
        }

        public IDocumentClient Client => _documentClient;

        public async Task Initialize()
        {
            // Should all be part of the singleton initializer, because msft says it can take time the first connect...
            await _documentClient.OpenAsync();
            await _documentClient.CreateDatabaseIfNotExistsAsync(new Database {Id = DatabaseName}).ConfigureAwait(false);
            await _documentClient
                .CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DatabaseName), new DocumentCollection {Id = CollectionName})
                .ConfigureAwait(false);
        }

        public async Task<Document> InsertSaga<TSaga>(TSaga saga, bool useJsonSerializerSettings)
            where TSaga : class, IVersionedSaga
        {
            RequestOptions options = null;

            if (useJsonSerializerSettings)
                options = new RequestOptions {JsonSerializerSettings = JsonSerializerSettingsExtensions.GetSagaRenameSettings<TSaga>()};

            return await Client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName), saga, options, true);
        }

        public async Task DeleteSaga(Guid correlationId)
        {
            try
            {
                await Client.DeleteDocumentAsync(UriFactory.CreateDocumentUri(DatabaseName, CollectionName, correlationId.ToString()));
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                // Swallow not found
            }
        }

        public async Task<TSaga> GetSaga<TSaga>(Guid correlationId, bool useJsonSerializerSettings)
            where TSaga : class, IVersionedSaga
        {
            try
            {
                RequestOptions options = null;
                JsonSerializerSettings serializerSettings = null;

                if (useJsonSerializerSettings)
                {
                    options = new RequestOptions();
                    serializerSettings = JsonSerializerSettingsExtensions.GetSagaRenameSettings<TSaga>();
                    options.JsonSerializerSettings = serializerSettings;
                }

                ResourceResponse<Document> document =
                    await Client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseName, CollectionName, correlationId.ToString()), options);

                return JsonConvert.DeserializeObject<TSaga>(document.Resource.ToString(), serializerSettings);
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<Document> GetSagaDocument(Guid correlationId)
        {
            try
            {
                ResourceResponse<Document> document =
                    await Client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseName, CollectionName, correlationId.ToString()));

                return document.Resource;
            }
            catch (DocumentClientException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
    }
}
