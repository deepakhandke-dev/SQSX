using Amazon.SQS;
using Amazon.SQS.Model;
using SQSX.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace SQSX.Services
{
    public class SQSConsumerService : BackgroundService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SQSConsumerService> _logger;
        private readonly Dictionary<string, MethodInfo> _consumers = new();

        public SQSConsumerService(IAmazonSQS sqsClient, IServiceProvider serviceProvider,
        ILogger<SQSConsumerService> logger)
        {
            _sqsClient = sqsClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
            DiscoverConsumers();
        }

        private void DiscoverConsumers()
        {
            var consumerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract)
                .ToList();

            foreach (var type in consumerTypes)
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var attribute = method.GetCustomAttribute<SQSMessageHandler>();
                    if (attribute != null)
                    {
                        _logger.LogInformation($"Discovered SQS consumer: {type.Name}.{method.Name} for queue {attribute.QueueUrl}");
                        _consumers[attribute.QueueUrl] = method;
                    }
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var queueUrl in _consumers.Keys)
                {
                    await ProcessMessages(queueUrl, stoppingToken);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task ProcessMessages(string queueUrl, CancellationToken stoppingToken)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20,
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _sqsClient.ReceiveMessageAsync(receiveRequest, stoppingToken);
                if (response.Messages.Count > 0)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var consumerMethod = _consumers[queueUrl];
                    var service = scope.ServiceProvider.GetRequiredService(consumerMethod?.DeclaringType);

                    foreach (var message in response.Messages)
                    {
                        try
                        {
                            _logger.LogInformation($"Processing message from {queueUrl}: {message.Body}");
                            var result = consumerMethod.Invoke(service, new object[] { message });

                            if (result is Task<bool> taskResult && await taskResult)
                            {
                                await DeleteMessageAsync(queueUrl, message.ReceiptHandle);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Error processing message from queue {queueUrl}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error polling queue {queueUrl}");
            }
        }

        private async Task DeleteMessageAsync(string queueUrl, string receiptHandle)
        {
            try
            {
                var response = await _sqsClient.DeleteMessageAsync(new DeleteMessageRequest
                {
                    QueueUrl = queueUrl,
                    ReceiptHandle = receiptHandle
                });

                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Message deleted from {queueUrl}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting message from {queueUrl}");
            }
        }
    }
}
