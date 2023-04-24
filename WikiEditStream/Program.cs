using System.Text.Json;
using Confluent.Kafka;

public class Program{
     static async Task Main(){
        var clientConfig = new ClientConfig();
            clientConfig.BootstrapServers="localhost:9092";
            await Produce("recent_changes", clientConfig);
    }

    private static async Task Produce(string topicName, ClientConfig config)
    {
        Console.WriteLine($"{nameof(Produce)} starting");

            // The URL of the EventStreams service.
            string eventStreamsUrl = "https://stream.wikimedia.org/v2/stream/recentchange";

            // Declare the producer reference here to enable calling the Flush
            // method in the finally block, when the app shuts down.
            IProducer<string, string> producer = null;
            try
            {
                // Build a producer based on the provided configuration.
                // It will be disposed in the finally block.
                producer = new ProducerBuilder<string, string>(config).Build();

                using(var httpClient = new HttpClient())

                using (var stream = await httpClient.GetStreamAsync(eventStreamsUrl))
                                
                using (var reader = new StreamReader(stream))
                {
                    // Read continuously until interrupted by Ctrl+C.
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        // The Wikimedia service sends a few lines, but the lines
                        // of interest for this demo start with the "data:" prefix. 
                        if(!line.StartsWith("data:"))
                        {
                            continue;
                        }

                        // Extract and deserialize the JSON payload.
                        int openBraceIndex = line.IndexOf('{');
                        string jsonData = line.Substring(openBraceIndex);
                        Console.WriteLine($"Data string: {jsonData}");

                        // Parse the JSON to extract the URI of the edited page.
                        var jsonDoc = JsonDocument.Parse(jsonData);
                        var metaElement = jsonDoc.RootElement.GetProperty("meta");
                        var uriElement = metaElement.GetProperty("uri");
                        var key = uriElement.GetString(); // Use the URI as the message key.

                        // For higher throughput, use the non-blocking Produce call
                        // and handle delivery reports out-of-band, instead of awaiting
                        // the result of a ProduceAsync call.
                        producer.Produce(topicName, new Message<string, string> { Key = key, Value = jsonData },
                            (deliveryReport) =>
                            {
                                if (deliveryReport.Error.Code != ErrorCode.NoError)
                                {
                                    Console.WriteLine($"Failed to deliver message: {deliveryReport.Error.Reason}");
                                }
                                else
                                {
                                    Console.WriteLine($"Produced message to: {deliveryReport.TopicPartitionOffset}");
                                }
                            });
                    }
                }

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                var queueSize = producer?.Flush(TimeSpan.FromSeconds(5));
                if (queueSize > 0)
                {
                    Console.WriteLine("WARNING: Producer event queue has " + queueSize + " pending events on exit.");
                }
                producer?.Dispose();
            }
    }
}
