using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Producer
{
    public class ProducerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ProducerConfig _producerConfiguration;
        private readonly ITopicRepository _topicRepository;

        public ProducerService(IConfiguration configuration, 
            ITopicRepository topicRepository) 
        {
            _configuration = configuration;
            _topicRepository = topicRepository;
            _producerConfiguration = new ProducerConfig
            {
                BootstrapServers = _configuration["KafkaServer"]
            };
        }

        private async Task BeginProduction()
        {
            // Try and create the primary topic (for first time requests)
            await _topicRepository.TryCreateTopic(_configuration["Topic"]);
            
            // Try and create the delay topic (for failed requests)
            await _topicRepository.TryCreateTopic(_configuration["RetryTopic"]);

            // Create the deadletter topic (for failed retry requests)
            await _topicRepository.TryCreateTopic(_configuration["DeadletterTopic"]);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BeginProduction();

            var producerBuilder = new ProducerBuilder<int, string>(_producerConfiguration);

            using (var p = producerBuilder.Build())
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var dr = await p.ProduceAsync(_configuration["Topic"], new Message<int, string>
                    {
                        // Only keys 1 - 9 exist in the database, so to create some failures we'll pick PKs that
                        // don't exist.
                        Key = new Random().Next(1, 18),
                        Value = $"{new Random().Next(1, 180)}"
                    });
                    Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");

                    // sleep 1s, but respond to cancellation
                    stoppingToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}