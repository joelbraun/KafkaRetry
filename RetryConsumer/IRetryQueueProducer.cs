using System.Threading.Tasks;
using Confluent.Kafka;

public interface IRetryQueueProducer<TKey, TValue> 
{
    Task RetryAsync(ConsumeResult<TKey, TValue> failedMessage);
}