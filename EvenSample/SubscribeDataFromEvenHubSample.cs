using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EvenSample
{
    public class SubscribeDataFromEvenHubSample
    {
        [FunctionName("SubscribeDataFromEvenHubSample")]
        public async Task Run(
            [EventHubTrigger(
                "user", 
                Connection = "Evh-pdpazure-dan-send") 
            ] EventData[] events, 
            ILogger log)
        {
            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    var data = JsonConvert.DeserializeObject<CosmosDBSample.Model.User>(messageBody);
                    
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    
                    // Logic or save data to db
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
