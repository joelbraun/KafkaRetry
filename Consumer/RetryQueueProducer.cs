using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

public class RetryQueueProducer<TKey, TValue> : IRetryQueueProducer<TKey, TValue>
{
    private readonly IConfiguration _configuration;
    private readonly IProducer<TKey, TValue> _producer;

    public RetryQueueProducer(IConfiguration configuration) 
    {
        _configuration = configuration;
        var producerBuilder = new ProducerBuilder<TKey, TValue>(
        new ProducerConfig
        {
            BootstrapServers = _configuration["KafkaServer"]
        });
        _producer = producerBuilder.Build();
    }

    public async Task RetryAsync(ConsumeResult<TKey, TValue> failedMessage) => 
        await _producer.ProduceAsync(_configuration["RetryTopic"],
        new Message<TKey, TValue>
        {
            Key = failedMessage.Key,
            Value = failedMessage.Value
        });
}