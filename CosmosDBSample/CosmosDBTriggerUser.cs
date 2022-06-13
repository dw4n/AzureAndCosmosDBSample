using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace CosmosDBSample
{
    public static class CosmosDBTriggerUser
    {

        const string _DATABASE = "Profile";
        const string _CONTAINERSOURCE = "User";
        const string _CONTAINERTARGET = "UserStatistic";
        const string _DATABASELEASE = "leases";
        private static string CONNECTIONSTRING = Environment.GetEnvironmentVariable("CosmosDBConnection");


        [FunctionName("CosmosDBTriggerUser")]
        public static async Task RunAsync(
            [CosmosDBTrigger(
                databaseName: _DATABASE,
                collectionName: _CONTAINERSOURCE,
                ConnectionStringSetting = "CosmosDBConnection",
                LeaseCollectionName = _DATABASELEASE,
                CreateLeaseCollectionIfNotExists = true)] IReadOnlyList<Document> input,
            ILogger log)
        {
            if (input != null && input.Count > 0)
            {
                log.LogInformation("Documents modified " + input.Count);

                var client = new CosmosClient(CONNECTIONSTRING);
                Container cosmosContainer = client.GetDatabase(_DATABASE).GetContainer(_CONTAINERTARGET);

                foreach (var document in input)
                {
                    log.LogInformation("Document Id " + document.Id);

                    var id = document.GetPropertyValue<string>("id");
                    var username = document.GetPropertyValue<string>("name");

                    var currentData = cosmosContainer.GetItemLinqQueryable<Model.UserStatistic>(true)
                         .Where(p => p.UserId == id)
                         .AsEnumerable()
                         .FirstOrDefault();

                    // If not found, create, else update
                    if (currentData == null)
                    {
                        currentData = new Model.UserStatistic()
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserId = id,
                            UserName = username,
                            ChangeCounter = 0,
                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                        };
                        await cosmosContainer.CreateItemAsync(currentData);
                    }
                    else
                    {
                        currentData.UserName = username;
                        currentData.ChangeCounter = currentData.ChangeCounter + 1;
                        currentData.ModifiedDate = DateTime.Now;

                        await cosmosContainer.ReplaceItemAsync<Model.UserStatistic>(currentData,
                            currentData.Id, new Microsoft.Azure.Cosmos.PartitionKey(currentData.Id));
                    }
                }

            }
        }
    }
}
