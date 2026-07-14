using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace PRN232.Notification.Infrastructure.SignalR;

public class GradingHub : Hub
{
    public async Task JoinRoomGroup(string roomId)
    {
        if (!string.IsNullOrEmpty(roomId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            System.Console.WriteLine($"--> Client {Context.ConnectionId} joined room group: {roomId}");
        }
    }

    public async Task JoinExamGroup(string examId)
    {
        if (!string.IsNullOrEmpty(examId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, examId);
            System.Console.WriteLine($"--> Client {Context.ConnectionId} joined exam group: {examId}");
        }
    }

    public async Task LeaveRoomGroup(string roomId)
    {
        if (!string.IsNullOrEmpty(roomId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            System.Console.WriteLine($"--> Client {Context.ConnectionId} left room group: {roomId}");
        }
    }

    public async Task LeaveExamGroup(string examId)
    {
        if (!string.IsNullOrEmpty(examId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, examId);
            System.Console.WriteLine($"--> Client {Context.ConnectionId} left exam group: {examId}");
        }
    }
}
