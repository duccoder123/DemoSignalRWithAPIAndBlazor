using API.HubServices;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalRHubs
{
    public class SignalRConnectionHub(HubService hubService) : Hub
    {

        public static List<string> ConnectedClients = [];
        private static List<string> PendingMessage = [];
        private static bool RetrieveOldMessagesCalled = false;

        public override async Task OnConnectedAsync()
        { 
            ConnectedClients.Add(Context.ConnectionId);
            await Clients
                     .All
                     .SendAsync("AllClientsNotification", Context.ConnectionId, ConnectedClients);
        }



        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            ConnectedClients.Remove(Context.ConnectionId);
            hubService.ConnectionGroups.Remove(Context.ConnectionId, out string? groupName);
            var groupMembers = hubService.GetMembers(groupName!);
            //await Clients.Group(groupName!).SendAsync("NotifyGroupOfNewJoin", groupMembers);
            await Clients
                     .All
                     .SendAsync("AllClientsNotification", $"{Context.ConnectionId} just left", ConnectedClients);
        }

        public async Task SendMessageToGroup(string groupName, string message)
            => await Clients.Group(groupName).SendAsync("Chat", Context.ConnectionId, message);

        public async Task SendMessageToIndividual(string receiverConnectionId, string message)
        {
            if (!RetrieveOldMessagesCalled)
                PendingMessage.Add($"{Context.ConnectionId} : {message}");
            else
                PendingMessage?.Clear();

            await Clients.Client(receiverConnectionId).SendAsync("Chat", Context.ConnectionId, message);
        }

        public async Task JoinGroup(string groupName)
        {
            var findGroup = hubService.FindGroupName(groupName);
            if (findGroup)
            {
                bool isUserInGroup = hubService.IsUserInGroup(Context.ConnectionId);
                if (!isUserInGroup)
                {
                    hubService.ConnectionGroups[Context.ConnectionId] = groupName;
                    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                }

                var groupMemers = hubService.GetMembers(groupName);
                await Clients.Group(groupName).SendAsync("NotifyGroupOfNewJoin", groupMemers);
            }
        }

        public async Task RetrieveOldMessages()
        {
            List<string> Messages = [];
            if (PendingMessage.Count == 0)
                return;
            foreach (var statement in PendingMessage)
                Messages.Add(statement);
            RetrieveOldMessagesCalled = true;
            await Clients.Client(Context.ConnectionId).SendAsync("RetrieveOldMessages", Messages);
        }

    }
}
