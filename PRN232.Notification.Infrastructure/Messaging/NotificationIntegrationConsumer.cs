using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PRN232.Notification.Infrastructure.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PRN232.Notification.Infrastructure.Messaging;

public class NotificationIntegrationConsumer : BackgroundService
{
    private readonly IHubContext<GradingHub> _hubContext;
    private readonly ILogger<NotificationIntegrationConsumer> _logger;
    private readonly string _hostName;

    public NotificationIntegrationConsumer(
        IHubContext<GradingHub> hubContext,
        IConfiguration configuration,
        ILogger<NotificationIntegrationConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        _hostName = configuration["RabbitMQ:HostName"] 
                    ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST") 
                    ?? "localhost";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("--> Notification Integration Consumer starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = _hostName };
                using var connection = await factory.CreateConnectionAsync(stoppingToken);
                using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // 1. Khai báo các queues cần nghe
                await channel.QueueDeclareAsync("grading-jobs", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync("grading-results", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync("plagiarism-alerts", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

                // 2. Thiết lập các Consumers cho từng queue
                var gradingJobsConsumer = new AsyncEventingBasicConsumer(channel);
                gradingJobsConsumer.ReceivedAsync += async (sender, ea) =>
                {
                    await HandleMessageAsync(ea, "UpdateProgress", stoppingToken);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                };

                var gradingResultsConsumer = new AsyncEventingBasicConsumer(channel);
                gradingResultsConsumer.ReceivedAsync += async (sender, ea) =>
                {
                    await HandleMessageAsync(ea, "UpdateProgress", stoppingToken);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                };

                var plagiarismAlertsConsumer = new AsyncEventingBasicConsumer(channel);
                plagiarismAlertsConsumer.ReceivedAsync += async (sender, ea) =>
                {
                    await HandleMessageAsync(ea, "PlagiarismAlert", stoppingToken);
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                };

                // 3. Đăng ký nhận tin từ các queues
                await channel.BasicConsumeAsync("grading-jobs", autoAck: false, consumer: gradingJobsConsumer, cancellationToken: stoppingToken);
                await channel.BasicConsumeAsync("grading-results", autoAck: false, consumer: gradingResultsConsumer, cancellationToken: stoppingToken);
                await channel.BasicConsumeAsync("plagiarism-alerts", autoAck: false, consumer: plagiarismAlertsConsumer, cancellationToken: stoppingToken);

                _logger.LogInformation("--> Notification Consumers connected and listening to 'grading-jobs', 'grading-results', and 'plagiarism-alerts' queues.");

                // Đợi cho đến khi nhận được tín hiệu dừng
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "--> RabbitMQ connection failed in Notification Service. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(BasicDeliverEventArgs ea, string targetClientMethod, CancellationToken cancellationToken)
    {
        try
        {
            var body = ea.Body.ToArray();
            var messageJson = Encoding.UTF8.GetString(body);
            _logger.LogInformation("--> Notification Service received message: {Message}", messageJson);

            // Bóc tách ExamId từ chuỗi JSON dạng bất kỳ để gửi đúng Group
            using var doc = JsonDocument.Parse(messageJson);
            var root = doc.RootElement;
            string examId = string.Empty;

            if (root.TryGetProperty("ExamId", out var prop))
            {
                examId = prop.GetString() ?? string.Empty;
            }
            else if (root.TryGetProperty("examId", out var prop2))
            {
                examId = prop2.GetString() ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(examId))
            {
                // Chuyển tiếp tin nhắn qua SignalR đến nhóm Giảng viên phòng thi đó
                await _hubContext.Clients.Group(examId).SendAsync(targetClientMethod, messageJson, cancellationToken);
                _logger.LogInformation("--> Forwarded message to SignalR group {ExamId} via method {Method}", examId, targetClientMethod);
            }
            else
            {
                _logger.LogWarning("--> Received message without ExamId. Cannot forward via SignalR.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "--> Error forwarding message in Notification Service");
        }
    }
}
