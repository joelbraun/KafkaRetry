using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Consumer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();
                services.AddSingleton<IRetryQueueProducer<int, string>, RetryQueueProducer<int, string>>();
                services.AddTransient<IEmployeeSqlRepository, EmployeeSqlRepository>();
                services.AddHostedService<ConsumerService>();
            })
            .ConfigureLogging((hostingContext, logging) => {
                logging.AddConsole();
            });

            await builder.RunConsoleAsync();
        }
    }
}
