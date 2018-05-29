using System.Dynamic;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;

namespace JsonTests.Data
{
    /// <summary>
    /// Simple Repository to support get / set of Inventory objects in 
    /// DocumentDB
    /// </summary>
    public sealed class InventoryRepository
    {
        protected IDocumentClient dbClient;
        protected Database docDb;
        protected DocumentDbSettings dbSettings;

        public InventoryRepository(IDocumentClient documentDbClient, DocumentDbSettings documentDbTypeSettings)
        {
            this.dbClient = documentDbClient;
            this.dbSettings = documentDbTypeSettings;

            var dbTask = DocumentDbUtil.GetOrCreateDatabaseAsync(this.dbClient, this.dbSettings.DatabaseId);
            dbTask.Wait();
            this.docDb = dbTask.Result;
        }

        public async Task<InventoryItem> GetInventoryItem(string id)
        {
            //If the Collection is partitioned, then limit us to look in the right place
            var opt = new RequestOptions();
            if (this.dbSettings.CollectionSpecs != null && this.dbSettings.CollectionSpecs.CollectionIsPartitioned)
            {
                InventoryItem seed = new InventoryItem();
                opt.PartitionKey = seed.PartitionKey();
            }

            try
            {
                Document result = await this.dbClient.ReadDocumentAsync(
                    UriFactory.CreateDocumentUri(this.dbSettings.DatabaseId, this.dbSettings.CollectionId, id),
                    opt
                );

                return (InventoryItem)(dynamic)result;
            }
            catch (DocumentClientException)
            {
                //Most likely due to the object not existing
                ///TODO logging?
                return default(InventoryItem);
            }
        }

        public async Task<InventoryItem> PutInventoryItem(InventoryItem item)
        {
            //Grab a link to the collection
            var collection = await DocumentDbUtil.GetOrCreateCollectionAsync(this.dbClient, this.docDb, this.dbSettings.CollectionId);

            //Store the new document
            var result = await this.dbClient.CreateDocumentAsync(collection.SelfLink, item);

            return (InventoryItem)(dynamic)result.Resource;
        }

        public async Task<InventoryItem> PutInventoryItemWithHack(InventoryItem item)
        {
            //Grab a link to the collection
            var collection = await DocumentDbUtil.GetOrCreateCollectionAsync(this.dbClient, this.docDb, this.dbSettings.CollectionId);

            //Apply a Hacky fix - the regular metod loses it's Concrete properties
            //Running it manually through the serializer at this point seems to "fix" it
            ExpandoObject toStore = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(item));

            var result = await this.dbClient.CreateDocumentAsync(collection.SelfLink, toStore);

            return (InventoryItem)(dynamic)result.Resource;
        }
    }
}
