namespace SQSX.Attributes
{
    /// <summary>
    /// Custom attribute used to mark methods as SQS consumers. 
    /// This attribute binds a method to a specific SQS queue URL and optionally defines the maximum number of messages that the method should process in one batch.
    /// It helps in automatically discovering and invoking methods that are designed to handle messages from SQS queues in a background service.
    ///
    /// Example usage:
    /// <code>
    /// [SQSMessageHandler("https://sqs.us-east-1.amazonaws.com/123456789012/my-queue")]
    /// public async Task ProcessMessage(Message message)
    /// {
    ///     // Message processing logic
    /// }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class SQSMessageHandler : Attribute
    {
        public string QueueUrl { get; }
        public int MaxMessages { get; }

        public SQSMessageHandler(string queueUrl, int maxMessages = 10)
        {
            QueueUrl = queueUrl;
            MaxMessages = maxMessages;
        }
    }
}
