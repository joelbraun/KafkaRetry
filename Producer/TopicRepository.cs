using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Really no need for a full repository, but this shouldn't live in the primary service.
/// Also, remaking the admin client probably isn't a best practice.
/// </summary>
public class TopicRepository : ITopicRepository {

    private readonly IConfiguration _configuration;
    
    public TopicRepository(IConfiguration configuration) 
    {
        _configuration = configuration;
    }

    public async Task TryCreateTopic(string topicName)
    {
        var adminConfig = new AdminClientConfig 
        { 
            BootstrapServers = _configuration["KafkaServer"]
        };

        using (var adminClient = new AdminClientBuilder(adminConfig).Build())
        {
            try
            {
                await adminClient.CreateTopicsAsync(new TopicSpecification[] {
                    new TopicSpecification
                    {
                        Name = topicName,
                        ReplicationFactor = 1,
                        NumPartitions = 1
                    }
                });
            }
            catch (CreateTopicsException e)
            {
                Console.WriteLine($"An error occured creating topic {e.Results[0].Topic}: {e.Results[0].Error.Reason}");
            }
        }
    }
}