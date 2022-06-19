using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.EventGrid.Models;
using System.Collections.Generic;

namespace EvenSample
{
    public static class SendDataToEvenHubSample
    {
        [FunctionName("SendDataToEvenHubSample")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [EventHub("user", 
                Connection = "Evh-pdpazure-dan-send")]
                IAsyncCollector<string> evenhubUser,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<CosmosDBSample.Model.User>(requestBody);

                var userData = new CosmosDBSample.Model.User()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = data.Name,
                    Email = data.Email
                };

                await evenhubUser.AddAsync(JsonConvert.SerializeObject(userData));
                log.LogInformation($"User ID : {userData.Id} Send to Evenhub.");

                return new OkObjectResult(userData);
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult(ex.Message);
                throw;
            }
        }
    }
}
