using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JsonTests.Data;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;

namespace JsonTests.Func
{
    public static class JsonTestFunction
    {
        private static object setupLock = new Object();
        private static bool setupComplete = false;

        private static readonly IDocumentClient DbClient;
        private static readonly InventoryRepository InventoryRepo;
        private static readonly DocumentDbSettings DbCfg = new DocumentDbSettings()
        {
            DatabaseId = "AzureFunctionTests",
            CollectionId = "InventoryTestData"
        };

        private static string propertiesJson = "{\"PackSize\":10,\"Colour\":\"Red\",\"Supplier\":{\"Name\":\"Spaceys Sprockets\",\"LineManager\":\"Jetson, G\"} }";


        static JsonTestFunction()
        {
            //Std double-check lock to set up DocumentDB Client
            if (!setupComplete)
            {
                lock (setupLock)
                {
                    if (!setupComplete)
                    {
                        //Build up the data tier repository stack
                        //Db Client first - just use the local emulator
                        DbClient = DocumentDbUtil.CreateClient("https://localhost:8081", "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

                        //And pipe that into the repo
                        InventoryRepo = new InventoryRepository(DbClient, DbCfg);

                        //And Done
                        setupComplete = true;
                    }
                }
            }
        }

        /// <summary>
        /// Really simply function - builds up a
        /// new Inventory entity from a pre-baked set of information,
        /// does some type checking before/after Storing and again 
        /// after retrieval
        /// </summary> 
        [FunctionName("JsonTest")]
        public static async Task<HttpResponseMessage> RunJsonTest(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)]HttpRequestMessage req,
                TraceWriter log)
        {
            log.Info("JsonTest picked up a request.");

            //Cook up some abirtary Json - includs a string, int and a nested object property
            Dictionary<string, object> propertiesDictionary = JsonConvert.DeserializeObject< Dictionary<string, object>>(propertiesJson);

            //And use that to populate our data item
            var sprocketItem = new InventoryItem()
            {
                ProductName = "Sprocket xl-25b",
                StockQuantity = 25000,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            foreach (var p in propertiesDictionary)
            {
                sprocketItem[p.Key] = p.Value;
            }

            //At this point we'd expect sprocketItem["Supplier"] to be a JObject
            log.Info($"Raw Item Supplier is type {sprocketItem["Supplier"].GetType().FullName}");
            log.Info($"Type Comparison is {sprocketItem["Supplier"].GetType() == typeof(Newtonsoft.Json.Linq.JObject)}");

            //Store it in the repo layer
            var storedItem = await InventoryRepo.PutInventoryItem(sprocketItem);

            //Now what do we have coming back out?
            if (storedItem.Properties.ContainsKey("Supplier"))
            {

                log.Info($"Stored Item Supplier is type {storedItem["Supplier"].GetType().FullName}");
                log.Info($"Type Comparison is {storedItem["Supplier"].GetType() == typeof(Newtonsoft.Json.Linq.JObject)}");
            }
            else
            {
                log.Warning("Stored Inventory Item lost the \"Supplier\" property");
            }

            //Refresh from source
            var retrievedItem = await InventoryRepo.GetInventoryItem(storedItem.Id);

            if (retrievedItem != null)
            {
                if (retrievedItem.Properties.ContainsKey("Supplier"))
                {

                    log.Info($"Retrieved Item Supplier is type {retrievedItem["Supplier"].GetType().FullName}");
                    log.Info(
                        $"Type Comparison is {retrievedItem["Supplier"].GetType() == typeof(Newtonsoft.Json.Linq.JObject)}");
                }
                else
                {
                    log.Warning("Retrieved Inventory Item lost the \"Supplier\" property");
                }
            }
            else
            {
                log.Error("Failed to retrieve Inventory Item from Repository");
            }
            


            return req.CreateResponse(HttpStatusCode.OK, "Done");
        }
    }
}
