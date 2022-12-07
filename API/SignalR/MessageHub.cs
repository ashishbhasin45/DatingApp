using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Text.Json;

namespace API.SignalR
{
    [Authorize]
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        //// Injecting presence hub into message hub to send notification 
        ///about new messages to online users
        private readonly IHubContext<PresenceHub> _presenceHub;

        public MessageHub(IMessageRepository messageRepository, IUserRepository userRepository,
            IMapper mapper, IHubContext<PresenceHub> presenceHub)
        {
            this._messageRepository = messageRepository;
            this._userRepository = userRepository;
            _mapper = mapper;
            _presenceHub = presenceHub;
        }

        public IMapper Mapper { get; }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            //Console.WriteLine("httpcontext headers {0}", JsonSerializer.Serialize(httpContext.Request.Headers));
            var otherUser = httpContext.Request.Query["user"];
            var currentUser = Context.User.GetUserName();
            var groupName = GetGroupName(currentUser, otherUser);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            
            var group = await AddToGroup(groupName);
            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);
           
            var messages = await _messageRepository
                .GetMessageThread(currentUser, otherUser);

            await Clients.Caller.SendAsync("RecieveMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var group = await RemoveFromMessageGroup();
            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {
            var userName = Context.User.GetUserName();

            if (userName == createMessageDto.RecipientUserName.ToLower())
                throw new HubException("You cannot send the message to yourself");

            var sender = await _userRepository.GetUserbyUsernameAsync(userName);
            var recipient = await _userRepository.GetUserbyUsernameAsync(createMessageDto.RecipientUserName);

            if (recipient == null) throw new HubException("not found user");

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUserName = sender.UserName,
                RecipientUserName = recipient.UserName,
                Content = createMessageDto.Content
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await _messageRepository.GetMessageGroup(groupName);
            if(group.Connections.Any(t => t.Username == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await PresenceTracker.GetConnectionsForUser(recipient.UserName);
                if(connections != null)
                {
                    //// send to essentially all the devices the recipient is connected to
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageRecieved",
                        new { username = sender.UserName, knownAs = sender.KnownAs });
                }
            }

            _messageRepository.AddMessage(message);
            if (await _messageRepository.SaveAllAsync()) {
                await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
            }
        }

        private string GetGroupName(string caller, string callee)
        {
            var stringCompare = string.CompareOrdinal(caller, callee) < 0;
            return stringCompare ? $"{caller}-{callee}" : $"{callee}-{caller}";
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var group = await _messageRepository.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, Context.User.GetUserName());
            if (group == null){
                group = new Group(groupName);
                _messageRepository.AddGroup(group);
            }

            group.Connections.Add(connection);
            if (await _messageRepository.SaveAllAsync()) return group;

            throw new HubException("Failed to add to the group");
        }

        private async Task<Group> RemoveFromMessageGroup()
        {
            var group = await _messageRepository.GetGroupForConnection(Context.ConnectionId);
            var connection = group.Connections.FirstOrDefault(x => x.ConnectionId == Context.ConnectionId);
            _messageRepository.RemoveConnection(connection);
            if(await _messageRepository.SaveAllAsync())
            {
                return group;
            }

            throw new HubException("Failed to remove the group");
        }
    }
}
