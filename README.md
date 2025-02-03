# SQS Consumer & Producer Library for .NET

## Overview
This library provides an easy and efficient way to **produce and consume messages from AWS SQS** using C#. It supports **both Standard and FIFO queues**, allows **batch publishing and batch consuming**, and enables seamless consumer integration using **method decorators**.

## Features
- **Seamless Consumer Integration**: Use method decorators to consume messages effortlessly.
- **Supports Standard & FIFO Queues**: Automatically handles FIFO deduplication and ordering.
- **Batch Publish & Batch Consume**: Optimized message processing for performance.
- **Reliable Processing**: Messages are only deleted from the queue after successful processing.
- **SOLID Principles & Design Patterns**: Ensures maintainability, scalability, and reusability.

## Dependencies
- `AWSSDK.SQS` (Amazon SQS SDK for .NET)
- `Microsoft.Extensions.Hosting` (Background service support)
- `Microsoft.Extensions.DependencyInjection` (DI for consumer handling)

## Design Patterns Used
- **Decorator Pattern**: Used for seamless consumer integration with attributes.
- **Factory Pattern**: Manages SQS client creation for producer and consumer services.
- **Singleton Pattern**: Ensures a single instance of SQS client.
- **Dependency Injection**: Enables testability and flexibility.

## Installation
```
dotnet add package SQSX --version 1.0.0
```

## Usage
### 1. Setup in `Program.cs`
```csharp
var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((hostContext, services) =>
{
    services.AddAWSService<IAmazonSQS>();
    services.AddSingleton<ISQSProducer, SQSProducer>();
    services.AddHostedService<SQSConsumerService>();
});
var app = builder.Build();
await app.RunAsync();
```

### 2. Publishing Messages
```csharp
public class OrderService
{
    private readonly ISQSProducer _sqsProducer;
    
    public OrderService(ISQSProducer sqsProducer)
    {
        _sqsProducer = sqsProducer;
    }
    
    public async Task PublishOrderAsync()
    {
        await _sqsProducer.PublishMessageAsync("https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue", new { OrderId = 123, Status = "Created" });
    }
}
```

### 3. Consuming Messages
```csharp
public class OrderProcessor
{
    [SQSConsumer("https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue", 5)]
    public async Task<bool> ProcessOrderAsync(Message message)
    {
        Console.WriteLine($"Received: {message.Body}");
        return true;
    }
}
```

## Advantages
✅ **Effortless Consumer Registration** using decorators.
✅ **Optimized for Performance** with batch processing.
✅ **Extensible & Scalable** due to SOLID principles.
✅ **Reduces Boilerplate Code** for SQS integration.


## Contributing
Pull requests are welcome! Ensure your changes adhere to best practices and include tests.

## License
MIT License

