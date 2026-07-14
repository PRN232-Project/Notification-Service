using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using PRN232.Notification.Grpc;
using PRN232.Notification.Infrastructure.SignalR;

namespace PRN232.Notification.Api.Services;

public class NotificationGrpcEndpoint : NotificationGrpcService.NotificationGrpcServiceBase
{
    private readonly IHubContext<GradingHub> _hubContext;
    private readonly ILogger<NotificationGrpcEndpoint> _logger;

    public NotificationGrpcEndpoint(
        IHubContext<GradingHub> hubContext,
        ILogger<NotificationGrpcEndpoint> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public override async Task<SendNotificationReply> SendNotification(
        SendNotificationRequest request,
        ServerCallContext context)
    {
        var payload = new
        {
            lecturerId = request.LecturerId,
            roomId = request.RoomId,
            examId = request.ExamId,
            submissionId = request.SubmissionId,
            type = request.Type,
            title = request.Title,
            message = request.Message,
            createdAtUtc = DateTime.UtcNow
        };

        var targetGroup = !string.IsNullOrWhiteSpace(request.RoomId)
            ? request.RoomId
            : request.ExamId;

        if (!string.IsNullOrWhiteSpace(targetGroup))
        {
            await _hubContext.Clients.Group(targetGroup).SendAsync("ReceiveNotification", payload, context.CancellationToken);
        }

        _logger.LogInformation("Forwarded gRPC notification to group {TargetGroup}.", targetGroup);

        return new SendNotificationReply
        {
            Success = true,
            Message = "Notification forwarded."
        };
    }
}
