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
using Microsoft.Azure.EventGrid;
using System.Collections.Generic;

namespace EvenSample
{
    public static class SendDataToEvenGridSample
    {
        private static string KEY = Environment.GetEnvironmentVariable("EventGridKey");
        private static string ENDPOINT = Environment.GetEnvironmentVariable("EventGridEndPoint");

        /// <summary>
        /// Send data with custom event using Azure Function
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("SendDataToEvenGridSample")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {

                var subject = "Create/"; // can be other 
                var evenType = "Model.User"; // namespace
                var userData = new CosmosDBSample.Model.User()
                    { 
                        Id = Guid.NewGuid().ToString() 
                    };


                var eventData = new EventGridEvent()
                {
                    Id = Guid.NewGuid().ToString(),
                    DataVersion = "1.0",
                    EventTime = DateTime.UtcNow,
                    Subject = subject,
                    EventType = evenType,
                    Data = JsonConvert.SerializeObject(userData)
                };

                var topicHostName = new Uri(ENDPOINT).Host;
                TopicCredentials topicCredentials = new TopicCredentials(KEY);
                var theEVGClient = new EventGridClient(topicCredentials);

                await theEVGClient.PublishEventsAsync(topicHostName, new List<EventGridEvent>
                {
                    eventData
                });

                return new OkObjectResult(eventData);
            }
            catch (Exception)
            {

                throw;
            }

        }
    }
}
