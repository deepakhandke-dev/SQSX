using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace SQSX.Services
{
    public class SQSProducerService
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly ILogger<SQSProducerService> _logger;

        public SQSProducerService(IAmazonSQS sqsClient, ILogger<SQSProducerService> logger)
        {
            _sqsClient = sqsClient;
            _logger = logger;
        }

        /// <summary>
        /// Sends a message asynchronously to an SQS queue with the option to specify a message group ID and deduplication ID for FIFO queues.
        /// This method ensures that the message is delivered to the specified queue and provides flexibility for controlling message ordering and duplication behavior.
        /// 
        /// The method accepts an optional `messageGroupId` to allow messages to be grouped within a FIFO queue and an optional `deduplicationId` to prevent duplicate messages from being processed.
        /// 
        /// If the queue is a standard queue, the `messageGroupId` and `deduplicationId` parameters can be omitted.
        ///
        /// <param name="queueUrl">The URL of the target SQS queue.</param>
        /// <param name="messageBody">The body content of the message to be sent.</param>
        /// <param name="messageGroupId">The message group ID for FIFO queues to ensure message ordering. (Optional)</param>
        /// <param name="deduplicationId">The deduplication ID to prevent message duplication in FIFO queues. (Optional)</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the message was successfully sent.</returns>
        /// </summary>
        public async Task<bool> SendMessageAsync(string queueUrl, string messageBody, string? messageGroupId = null, string? deduplicationId = null)
        {
            try
            {
                var request = new SendMessageRequest
                {
                    QueueUrl = queueUrl,
                    MessageBody = messageBody,
                };

                if (queueUrl.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(messageGroupId))
                        throw new ArgumentException("FIFO queues require a MessageGroupId.");

                    request.MessageGroupId = messageGroupId;
                    request.MessageDeduplicationId = deduplicationId ?? Guid.NewGuid().ToString();
                }

                var response = await _sqsClient.SendMessageAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to queue {QueueUrl}", queueUrl);
                return false;
            }
        }

        /// <summary>
        /// Sends a batch of messages asynchronously to an SQS queue, with optional support for message grouping in FIFO queues.
        /// This method allows multiple messages to be sent in a single request, improving throughput and reducing the number of API calls.
        /// 
        /// The `messageGroupId` parameter can be provided for FIFO queues to ensure the correct ordering of messages within a group.
        /// If the queue is a standard queue, the `messageGroupId` parameter can be omitted.
        ///
        /// <param name="queueUrl">The URL of the target SQS queue.</param>
        /// <param name="messages">A list of message bodies to be sent to the queue.</param>
        /// <param name="messageGroupId">The message group ID for FIFO queues to ensure message ordering. (Optional)</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the batch of messages was successfully sent.</returns>
        /// </summary>
        public async Task<bool> SendMessageBatchAsync(string queueUrl, List<string> messages, string? messageGroupId = null)
        {
            if (messages == null || messages.Count == 0)
                throw new ArgumentException("Message batch cannot be empty.");

            try
            {
                var batchRequestEntries = messages.Select((message, index) =>
                    new SendMessageBatchRequestEntry
                    {
                        Id = index.ToString(), // Unique within batch
                        MessageBody = message,
                        MessageGroupId = queueUrl.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase) ? messageGroupId : null,
                        MessageDeduplicationId = queueUrl.EndsWith(".fifo", StringComparison.OrdinalIgnoreCase) ? Guid.NewGuid().ToString() : null
                    }).ToList();

                var request = new SendMessageBatchRequest
                {
                    QueueUrl = queueUrl,
                    Entries = batchRequestEntries
                };

                var response = await _sqsClient.SendMessageBatchAsync(request);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending batch messages to queue {QueueUrl}", queueUrl);
                return false;
            }
        }
    }
}
