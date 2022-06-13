//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Azure.Cosmos;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using Microsoft.Azure.WebJobs.Extensions.Http;
//using Microsoft.Extensions.Logging;

//namespace CosmosDBSample
//{
//    public static class DocsByUsingCosmosClient
//    {  
//        [FunctionName("DocsByUsingCosmosClient")]
//        public static async Task<IActionResult> Run(
//            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
//                Route = null)]HttpRequest req,
//            [CosmosDB(
//                databaseName: "ToDoItems",
//                containerName: "Items",
//                Connection = "CosmosDBConnection")] CosmosClient client,
//            ILogger log)
//        {
//            log.LogInformation("C# HTTP trigger function processed a request.");

//            var searchterm = req.Query["searchterm"].ToString();
//            if (string.IsNullOrWhiteSpace(searchterm))
//            {
//                return (ActionResult)new NotFoundResult();
//            }

//            Container container = client.GetDatabase("ToDoItems").GetContainer("Items");

//            log.LogInformation($"Searching for: {searchterm}");

//            QueryDefinition queryDefinition = new QueryDefinition(
//                "SELECT * FROM items i WHERE CONTAINS(i.Description, @searchterm)")
//                .WithParameter("@searchterm", searchterm);
//            using (FeedIterator<Model.User> resultSet = container.GetItemQueryIterator<Model.User>(queryDefinition))
//            {
//                while (resultSet.HasMoreResults)
//                {
//                    FeedResponse<Model.User> response = await resultSet.ReadNextAsync();
//                    Model.User item = response.First();
//                    log.LogInformation(item.Description);
//                }
//            }

//            return new OkResult();
//        }
//    }
//}