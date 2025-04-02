using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Presentation.API.Hubs;

public class ChatHub : Hub
{
    private static readonly Dictionary<string, List<string>> UserConnections = new();
    private static readonly Dictionary<string, string?> ConnectionUsernames = new();
    private static readonly Dictionary<string, string> UserConversations = new(); // username1-username2 : conversationId
    private static readonly List<Conversation> Conversations = new();
    private static readonly List<Message> Messages = new();

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        Console.WriteLine($"User connected: {connectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        if (ConnectionUsernames.TryGetValue(connectionId, out var username))
        {
            if (username is null)
            {
                return;
            }

            if (UserConnections.TryGetValue(username, out var userConnections))
            {
                userConnections.Remove(connectionId);
                if (userConnections.Count == 0)
                {
                    UserConnections.Remove(username);
                }
            }
            ConnectionUsernames.Remove(connectionId);

            // Notify conversation participants and delete conversations
            var conversationsToDelete = Conversations.Where(c => c.Participants.Contains(username)).ToList();
            foreach (var conversation in conversationsToDelete)
            {
                // Notify participants *before* deleting
                foreach (var participant in conversation.Participants)
                {
                    if (participant != username && UserConnections.TryGetValue(participant, out var connections))
                    {
                        foreach (var connId in connections)
                        {
                            await Clients.Client(connId).SendAsync("UserLeftConversation", conversation.ConversationId, username);
                        }
                    }
                }

                // Delete messages
                Messages.RemoveAll(m => m.ConversationId == conversation.ConversationId);
                // Delete conversation
                Conversations.Remove(conversation);

                // Remove from UserConversations dictionary
                foreach (var participant in conversation.Participants)
                {
                    foreach (var otherParticipant in conversation.Participants)
                    {
                        if (participant != otherParticipant)
                        {
                            var key = string.Join("-", new string[] { participant, otherParticipant }.OrderBy(s => s));
                            UserConversations.Remove(key);
                        }
                    }
                }
            }

            await Clients.All.SendAsync("UserDisconnected", username);
        }
        Console.WriteLine($"User disconnected: {connectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task<string> CreateConversation(string[] participants)
    {
        Array.Sort(participants); // Ensure consistent ordering
        var conversationKey = string.Join("-", participants);

        if (UserConversations.ContainsKey(conversationKey))
        {
            return UserConversations[conversationKey]; // Return existing conversation ID
        }

        var conversationId = Guid.NewGuid().ToString();
        var newConversation = new Conversation
        {
            ConversationId = conversationId,
            Participants = participants.ToList(),
            CreatedAt = DateTime.UtcNow.ToString("O") // Add this line
        };
        Conversations.Add(newConversation);
        UserConversations[conversationKey] = conversationId;

        // Notify participants (simplified - consider optimized approach)
        foreach (var participant in participants)
        {
            if (UserConnections.TryGetValue(participant, out var connections))
            {
                foreach (var connectionId in connections)
                {
                    await Clients.Client(connectionId).SendAsync("ConversationCreated", newConversation);
                }
            }
        }

        return conversationId;
    }

    public async Task<List<Message>> GetConversationMessages(string conversationId)
    {
        return Messages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.Timestamp).ToList();
    }

    public async Task SendMessage(string conversationId, string message)
    {
        var senderUsername = ConnectionUsernames[Context.ConnectionId];
        var newMessage = new Message
        {
            MessageId = Guid.NewGuid().ToString(),
            ConversationId = conversationId,
            SenderUsername = senderUsername,
            Content = message,
            Timestamp = DateTime.UtcNow,
            ReadBy = new List<string>() // Initialize ReadBy
        };
        Messages.Add(newMessage);

        var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);
        if (conversation != null)
        {
            foreach (var participant in conversation.Participants)
            {
                if (UserConnections.TryGetValue(participant, out var connections))
                {
                    foreach (var connectionId in connections)
                    {
                        await Clients.Client(connectionId).SendAsync("ReceiveMessage", newMessage);
                    }
                }
            }
        }
    }

    public async Task<List<Conversation>> GetConversationsForUser(string username)
    {
        return Conversations.Where(c => c.Participants.Contains(username)).ToList();
    }

    public async Task Typing(string to)
    {
        if (ConnectionUsernames.TryGetValue(Context.ConnectionId, out var senderUsername))
        {
            if (UserConnections.TryGetValue(to, out var connections))
            {
                foreach (var connectionId in connections)
                {
                    if (to != senderUsername) // Only send to the recipient
                    {
                        await Clients.Client(connectionId).SendAsync("UserTyping", senderUsername);
                    }
                }
            }
        }
    }

    public async Task StoppedTyping(string to)
    {
        if (ConnectionUsernames.TryGetValue(Context.ConnectionId, out var senderUsername))
        {
            if (UserConnections.TryGetValue(to, out var connections))
            {
                foreach (var connectionId in connections)
                {
                    if (to != senderUsername) // Only send to the recipient
                    {
                        await Clients.Client(connectionId).SendAsync("UserStoppedTyping", senderUsername);
                    }
                }
            }
        }
    }

    public async Task<ActiveUser[]> GetActiveUsers()
    {
        List<ActiveUser> users = new();
        if (ConnectionUsernames.TryGetValue(Context.ConnectionId, out var currentUsername))
        {
            foreach (var pair in UserConnections)
            {
                if (pair.Key != currentUsername)
                {
                    users.Add(new ActiveUser { Username = pair.Key, ConnectionIds = pair.Value.ToArray() });
                }
            }
        }

        return users.ToArray();
    }

    public async Task SetUsername(string username)
    {
        var connectionId = Context.ConnectionId;
        var normalizedUsername = username.ToLower(); // Normalize to lowercase

        if (UserConnections.ContainsKey(normalizedUsername) && !UserConnections[normalizedUsername].Contains(connectionId))
        {
            await Clients.Caller.SendAsync("ErrorMessage", "Username already in use.");

            // Notify everyone in conversations with the existing user
            foreach (var conversation in Conversations.Where(c => c.Participants.Contains(normalizedUsername)))
            {
                foreach (var participant in conversation.Participants)
                {
                    if (UserConnections.TryGetValue(participant, out var connections))
                    {
                        foreach (var connId in connections)
                        {
                            if (ConnectionUsernames.TryGetValue(Context.ConnectionId, out var currentUsername))
                            {
                                if (ConnectionUsernames.TryGetValue(connId, out var participantUsername) && participantUsername != currentUsername)
                                {
                                    await Clients.Client(connId).SendAsync("TriedToUse", username);
                                }
                            }
                        }
                    }
                }
            }

            return;
        }

        if (UserConnections.TryGetValue(normalizedUsername, out var connection))
        {
            connection.Add(connectionId);
        }
        else
        {
            UserConnections[normalizedUsername] = new List<string> { connectionId };
        }
        ConnectionUsernames[connectionId] = normalizedUsername;

        await Clients.All.SendAsync("UserConnected", new UserConnection { Username = normalizedUsername, ConnectionId = connectionId });
    }
    
    public async Task MarkMessagesAsRead(string conversationId, string[] messageIds)
    {
        var currentUsername = ConnectionUsernames[Context.ConnectionId];
        if (currentUsername == null) return; // Handle the case where the username is not set

        foreach (var messageId in messageIds)
        {
            var message = Messages.FirstOrDefault(m => m.MessageId == messageId && m.ConversationId == conversationId);
            if (message != null && !message.ReadBy.Contains(currentUsername))
            {
                message.ReadBy.Add(currentUsername);

                // Notify other participants (optional, for real-time updates)
                var conversation = Conversations.FirstOrDefault(c => c.ConversationId == conversationId);
                if (conversation != null)
                {
                    foreach (var participant in conversation.Participants)
                    {
                        if (participant != currentUsername && UserConnections.TryGetValue(participant, out var connections))
                        {
                            foreach (var connectionId in connections)
                            {
                                await Clients.Client(connectionId).SendAsync("MessageRead", conversationId, messageId, currentUsername);
                            }
                        }
                    }
                }
            }
        }
    }

    public class ActiveUser
    {
        public string Username { get; set; }
        public string[] ConnectionIds { get; set; }
    }

    public class UserConnection
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
    }

    public class Conversation
    {
        public string ConversationId { get; set; }
        public List<string> Participants { get; set; }
        public string CreatedAt { get; set; }
    }

    public class Message
    {
        public string MessageId { get; set; }
        public string ConversationId { get; set; }
        public string SenderUsername { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public List<string> ReadBy { get; set; }
    }
}