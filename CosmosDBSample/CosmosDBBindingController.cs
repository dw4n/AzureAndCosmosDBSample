using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace CosmosDBSample
{
    public class CosmosDBBindingController
    {

        const string _DATABASE = "Profile";
        const string _CONTAINER = "User";
        private static string CONNECTIONSTRING = Environment.GetEnvironmentVariable("CosmosDBConnection");

        private static CosmosClient _client;

        public CosmosDBBindingController(CosmosClient client)
        {
            _client ??= client;
        }

        /// <summary>
        /// Return JSON Model for reference
        /// </summary>
        /// <param name = "req" ></ param >
        /// < param name="log"></param>
        /// <returns></returns>
        [FunctionName("ReturnModelJSON")]
        public async Task<IActionResult> ReturnModelJSON(
          [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ReturnModelJSON")] HttpRequest req,
          ILogger log)
        {
            try
            {
                log.LogInformation("ReturnModelJSON");

                var result = new Model.User()
                {
                    Id = Guid.NewGuid().ToString(),
                    Email = "sample@email.com",
                    Name = "Si Bolang",
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Create user data manually via SDK COSMOS v3
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("CreateUserData")]
        public async Task<IActionResult> CreateUserData(
          [HttpTrigger(AuthorizationLevel.Function, "Post", Route = "User")] HttpRequest req,
          ILogger log)
        {
            try
            {
                log.LogInformation("CreateUserData");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var postUserData = JsonConvert.DeserializeObject<Model.User>(requestBody);

                postUserData.Id = Guid.NewGuid().ToString();
                postUserData.CreatedDate = DateTime.Now;
                postUserData.ModifiedDate = DateTime.Now;

                var client = new CosmosClient(CONNECTIONSTRING);
                Container cosmosContainer = client.GetDatabase(_DATABASE).GetContainer(_CONTAINER);

                var createdItem = await cosmosContainer.CreateItemAsync(postUserData);

                return new OkObjectResult(createdItem.StatusCode);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Create user data manually via SDK COSMOS v3
        /// V2 using startup DI
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("CreateUserDataV2")]
        public async Task<IActionResult> CreateUserDataV2(
          [HttpTrigger(AuthorizationLevel.Function, "Post", Route = "UserV2")] HttpRequest req,
          ILogger log)
        {
            try
            {
                log.LogInformation("CreateUserDataV2");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var postUserData = JsonConvert.DeserializeObject<Model.User>(requestBody);

                postUserData.Id = Guid.NewGuid().ToString();
                postUserData.CreatedDate = DateTime.Now;
                postUserData.ModifiedDate = DateTime.Now;

                //var client = new CosmosClient(CONNECTIONSTRING);
                Container cosmosContainer = _client.GetDatabase(_DATABASE).GetContainer(_CONTAINER);

                var createdItem = await cosmosContainer.CreateItemAsync(postUserData);

                return new OkObjectResult(createdItem.StatusCode);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Get data using Document Client sdk v2 (dan 4 soon)
        /// </summary>
        /// <param name="req"></param>
        /// <param name="client"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GetListUserData")]
        public async Task<IActionResult> GetListUserData(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "User/List")] HttpRequest req,
            [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                var result = new List<Model.User>();
               
                Uri collectionUri = UriFactory.CreateDocumentCollectionUri(_DATABASE, _CONTAINER);
                var options = new FeedOptions() { EnableCrossPartitionQuery = true };

                IDocumentQuery<Model.User> query = client.CreateDocumentQuery<Model.User>(collectionUri, 
                    feedOptions: options)
                    .Where(p => p.Id != null)
                    .AsDocumentQuery();

                while (query.HasMoreResults)
                {
                    foreach (Model.User item in await query.ExecuteNextAsync())
                    {
                        log.LogInformation(item.Name);
                        result.Add(item);
                    }
                }
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Prepare Data using Binding
        /// Get data by Id using Binding directly
        /// </summary>
        /// <param name="req"></param>
        /// <param name="user"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GetSpecificUserDataById")]
        public static async Task<IActionResult> GetSpecificUserDataById(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "User/Id/{id}")] HttpRequest req,
            [CosmosDB(
                databaseName: _DATABASE,
                collectionName: _CONTAINER,
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}",
                PartitionKey = "{id}")] Model.User user,
           ILogger log)
        {
            try
            {
                log.LogInformation(user.Name);

                return new OkObjectResult(user);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
                throw;
            }
        }


        /// <summary>
        /// Prepare Cosmos using SDK
        /// Get data using linq,
        /// Update data using client v2
        /// </summary>
        /// <param name="req"></param>
        /// <param name="client"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("UpdateSpecificUserDataById")]
        public static async Task<IActionResult> UpdateSpecificUserDataById(
           [HttpTrigger(AuthorizationLevel.Function, "put", Route = "User/Id/{id}")] HttpRequest req,
           [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
           string id,
           ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var postData = JsonConvert.DeserializeObject<Model.User>(requestBody);
                postData.Id = id;

                var result = await client.ReplaceDocumentAsync(
                    UriFactory.CreateDocumentUri(_DATABASE, _CONTAINER, id), postData);

             
                return new OkObjectResult(result.StatusCode);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message);
                throw;
            }
        }



        /// <summary>
        /// Prepare Data using Binding
        /// Delete Data using SDK V3
        /// </summary>
        /// <param name="req"></param>
        /// <param name="user"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("DeleteDataById")]
        public static async Task<IActionResult> DeleteDataById(
           [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "User/Id/{id}")] HttpRequest req,
           [CosmosDB(
                databaseName: _DATABASE,
                collectionName: _CONTAINER,
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}",
                PartitionKey = "{id}")] Model.User user,
           ILogger log)
        {
            try
            {
                log.LogInformation(user.Name + " will be deleted.");

                if (user == null) throw new ArgumentException("Error delete data, old data not found");

                var client = new CosmosClient(CONNECTIONSTRING);
                Container cosmosContainer = client.GetDatabase(_DATABASE).GetContainer(_CONTAINER);

                var information = await cosmosContainer.DeleteItemAsync<Model.User>(user.Id, new PartitionKey(user.Id));

                return new OkObjectResult(information.StatusCode);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Prepare Data using Binding
        /// Delete Data using SDK V3
        /// Use DI provided in startup 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="user"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("DeleteDataByIdV2")]
        public static async Task<IActionResult> DeleteDataByIdV2(
           [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "UserV2/Id/{id}")] HttpRequest req,
           [CosmosDB(
                databaseName: _DATABASE,
                collectionName: _CONTAINER,
                ConnectionStringSetting = "CosmosDBConnection",
                Id = "{id}",
                PartitionKey = "{id}")] Model.User user,
           ILogger log)
        {
            try
            {
                log.LogInformation(user.Name + " will be deleted.");

                if (user == null) throw new ArgumentException("Error delete data, old data not found");

                //var client = new CosmosClient(CONNECTIONSTRING);
                Container cosmosContainer = _client.GetDatabase(_DATABASE).GetContainer(_CONTAINER);

                var information = await cosmosContainer.DeleteItemAsync<Model.User>(user.Id, new PartitionKey(user.Id));

                return new OkObjectResult(information.StatusCode);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                throw;
            }
        }
    }
}
