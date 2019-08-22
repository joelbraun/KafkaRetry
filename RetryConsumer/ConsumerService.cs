using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Consumer
{
    public class ConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IEmployeeSqlRepository _employeeSqlRepository;
        private readonly IRetryQueueProducer<int, string> _retryQueueProducer;

        public ConsumerService(IConfiguration configuration, 
            IEmployeeSqlRepository employeeSqlRepository,
            IRetryQueueProducer<int, string> retryQueueProducer) 
        {
            _configuration = configuration;
            _employeeSqlRepository = employeeSqlRepository;
            _retryQueueProducer = retryQueueProducer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var kafkaConfig = new ConsumerConfig
            { 
                GroupId = _configuration["ConsumerGroup"],
                BootstrapServers = _configuration["KafkaServer"]
            };

            using (var consumer = new ConsumerBuilder<int, string>(kafkaConfig).Build())
            {
                consumer.Subscribe(_configuration["Topic"]);

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try 
                        {
                            var consumeResult = consumer.Consume(stoppingToken);
                            Console.WriteLine($"Retry consumed message '{consumeResult.Value}' at: '{consumeResult.TopicPartitionOffset}'.");

                            if (!TryConsume(consumeResult, stoppingToken)) 
                            {
                                await _retryQueueProducer.RetryAsync(consumeResult);
                            }
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                finally
                {
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    consumer.Close();
                }
            }
        }

        private bool TryConsume(ConsumeResult<int, string> consumeResult, CancellationToken token) 
        {
            // If a retry threshold is present for the consumer, delay until it is met.
            var delayTime = _configuration.GetValue<int?>("DelayTimeMs");
            if (delayTime.HasValue) 
            {
                var retryTime = consumeResult.Timestamp.UtcDateTime.AddMilliseconds(delayTime.Value);

                if (retryTime >= DateTime.UtcNow) 
                {
                    token.WaitHandle.WaitOne(retryTime - DateTime.UtcNow);
                }
            }

            return _employeeSqlRepository
                .UpdateEmployeeName(consumeResult.Key, consumeResult.Value, consumeResult.Timestamp.UtcDateTime);
        }
    }
}