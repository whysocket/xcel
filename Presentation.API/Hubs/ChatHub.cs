using Microsoft.AspNetCore.SignalR;

namespace Presentation.API.Hubs;

public class ChatHub : Hub
{
    private static readonly Dictionary<string, string> Usernames = new Dictionary<string, string>();
    private static readonly HashSet<string> ConnectedUsers = new HashSet<string>();

    public override async Task OnConnectedAsync()
    {
        string connectionId = Context.ConnectionId;
        ConnectedUsers.Add(connectionId);
        Console.WriteLine($"User connected: {connectionId}");
        await Clients.All.SendAsync("UserConnected", new { connectionId, username = Usernames.GetValueOrDefault(connectionId, connectionId) });
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        string connectionId = Context.ConnectionId;
        ConnectedUsers.Remove(connectionId);
        Usernames.Remove(connectionId);
        Console.WriteLine($"User disconnected: {connectionId}");
        await Clients.All.SendAsync("UserDisconnected", connectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string recipient, string message)
    {
        await Clients.Client(recipient).SendAsync("ReceiveMessage", Context.ConnectionId, message);
    }

    public async Task<object[]> GetActiveUsers()
    {
        List<object> users = new List<object>();
        foreach(var connectionId in ConnectedUsers)
        {
            users.Add(new { connectionId, username = Usernames.GetValueOrDefault(connectionId, connectionId) });
        }
        return users.ToArray();
    }

    public async Task SetUsername(string connectionId, string username)
    {
        Usernames[connectionId] = username;
    }
}