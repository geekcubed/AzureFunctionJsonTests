using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace JsonTests.Data
{
    /// <summary>
    /// Bunch of utility methods for working with DocumentDB
    /// 
    /// Copyright (c) 2016 Biotware Ltd.
    /// </summary>
    public static class DocumentDbUtil
    {
        /// <summary>
        /// Creates a DocumentDb client.
        /// </summary>
        /// <param name="endpoint">The endpoint</param>
        /// <param name="authKey">The authentication key</param>
        /// <returns></returns>
        public static IDocumentClient CreateClient(string endpoint, string authKey, ConnectionPolicy connectionPolicy = null)
        {
            Uri endpointUri = new Uri(endpoint);

            //Emulator Check
            //DocDb Emulator doesn't support endpoint discovery,
            //If we're pointing at a localhost, we're likely using the Emulator
            //So make sure we force discovery off
            if (endpointUri.IsLoopback)
            {
                if (connectionPolicy == null)
                {
                    connectionPolicy = new ConnectionPolicy();
                }

                connectionPolicy.EnableEndpointDiscovery = false;
            }

            //Creat the client instance
            var client = new DocumentClient(endpointUri, authKey, connectionPolicy);
            
            //Reliable Client extension now nolonger needed
            //.AsReliable(new FixedInterval(15, TimeSpan.FromMilliseconds(200)));

            //For performance, it's recommended that you open the 
            //client connection ahead of time
            //But this falls apart with SimpleInjector as we run into a deadlock(?)
            //When trying to wait for this method to complete
            //Sooooo, lets not do that then and just accept a minor hit on first run
            //await client.UnderlyingClient.OpenAsync();

            //Done :)
            return client;
        }


        /// <summary>
        /// Gets a reference to a DocumentDB database, or creates if 
        /// it does not already exist
        /// </summary>
        /// <param name="Client">The client.</param>
        /// <param name="databaseId">The database identifier.</param>
        /// <returns></returns>
        public static async Task<Database> GetOrCreateDatabaseAsync(IDocumentClient Client, string databaseId)
        {
            Database database = Client.CreateDatabaseQuery().Where(db => db.Id == databaseId).ToArray().FirstOrDefault();
            if (database == null)
            {
                database = await Client.CreateDatabaseAsync(new Database { Id = databaseId });
            }

            return database;
        }

        /// <summary>
        /// Returns a refernce to a collection with a DocumentDb database, or
        /// creates if it does not exist
        /// </summary>
        /// <param name="Client">DocumentDb client connection</param>
        /// <param name="db">Reference to the containing DocumentDb database</param>
        /// <param name="collectionId">The Id of the Collection to return a reference for</param>
        /// <param name="collectionSpec">Specifications for the Collection (used to create if it does not exist)</param>
        /// <returns></returns>
        public static async Task<DocumentCollection> GetOrCreateCollectionAsync(IDocumentClient Client,
            Database db, string collectionId, DocumentCollectionSpec collectionSpec = null)
        {
            DocumentCollection collection = Client.CreateDocumentCollectionQuery(db.SelfLink).Where(c => c.Id == collectionId).ToArray().FirstOrDefault();

            if (collection == null)
            {
                collection = await CreateNewCollection(Client, db, collectionId, collectionSpec);
            }
            return collection;
        }

        /// <summary>
        /// Creates the new collection.
        /// </summary>
        /// <param name="client">The client.</param>
        /// <param name="database">The database.</param>
        /// <param name="collectionId">The collection identifier.</param>
        /// <param name="collectionSpec">The collection spec.</param>
        /// <returns></returns>
        public static async Task<DocumentCollection> CreateNewCollection(
            IDocumentClient client,
            Database database,
            string collectionId,
            DocumentCollectionSpec collectionSpec)
        {
            DocumentCollection collectionDefinition = new DocumentCollection { Id = collectionId };

            int throughput = 400;
            if (collectionSpec != null)
            {

                //Partitions
                if (collectionSpec != null && collectionSpec.CollectionIsPartitioned)
                {
                    collectionDefinition.PartitionKey.Paths.Add(collectionSpec.PartitionPath);
                }

                //Set throughput for new collection
                throughput = collectionSpec.Throughput;
            }

            //Create the Collection
            DocumentCollection collection = await 
                client.CreateDocumentCollectionAsync(
                    database.SelfLink, 
                    collectionDefinition, 
                    new RequestOptions { OfferThroughput = throughput }
                );

            return collection;
        }
    }

    /// <summary>
    /// Object to encapsulate the settings required to connect to a DocDb Server
    /// </summary>
    public sealed class DocumentDbSettings
    {
        public string DatabaseId { get; set; }

        public string CollectionId { get; set; }

        public DocumentCollectionSpec CollectionSpecs { get; set; }
    }

    /// <summary>
    /// Encapsulates the settings required to instantiate a new
    /// DocumentDB Collection if the requested one does not exist 
    /// inside the specified server
    /// </summary>
    public class DocumentCollectionSpec
    {
        /// <summary>
        /// Gets or sets the IndexingPolicy to use.
        /// </summary>
        public IndexingPolicy IndexingPolicy { get; set; }

        /// <summary>
        /// Gets or sets the OfferType to use, e.g., S1, S2, S3.
        /// </summary>
        public int Throughput { get; set; }

        /// <summary>
        /// Is the Collection Partitioned?
        /// </summary>
        public bool CollectionIsPartitioned { get; set; }

        /// <summary>
        /// Property to partition the collection on
        /// </summary>
        public string PartitionPath { get; set; }
    }
}