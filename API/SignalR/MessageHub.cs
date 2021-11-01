using System;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace API.SignalR
{
    public class MessageHub : Hub
    {
        private readonly IMessageRepository _messageRepo;
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepo;
        private readonly IHubContext<PresenceHub> _presenceHub;
        private readonly PresenceTracker _tracker;
        private readonly IUnitOfWork _unitOfWork;

        public MessageHub(IMapper mapper, IHubContext<PresenceHub> presenceHub, PresenceTracker tracker, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _tracker = tracker;
            _presenceHub = presenceHub;
            _mapper = mapper;
            _userRepo = unitOfWork.UserRepository;
            _messageRepo = unitOfWork.MessageRepository;
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var username = httpContext.User.GetUsername();
            var otherUser = httpContext.Request.Query["user"].ToString();
            var groupName = GetGroupName(username, otherUser);

            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            var group = await AddToGroup(groupName);

            await Clients.Group(groupName).SendAsync("UpdatedGroup", group);

            var messages = await _messageRepo.GetMessageThread(username, otherUser);

            if (_unitOfWork.HasChanges())
            {
                await _unitOfWork.Complete();
            }

            await Clients.Caller.SendAsync("ReceivedMessageThread", messages);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);

            var group = await RemoveFromGroup();

            await Clients.Group(group.Name).SendAsync("UpdatedGroup", group);
        }

        public async Task SendMessage(CreateMessageDto createMessageDto)
        {

            var username = Context.GetHttpContext().User.GetUsername();

            if (username.ToLower() == createMessageDto.RecipientUsername.ToLower())
            {
                throw new HubException("You cannot sent message to yourself");
            }

            var sender = await _userRepo.GetUserByUsernameAsync(username);
            var recipient = await _userRepo.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null)
            {
                throw new HubException("Recipient not found");
            }

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUserName = sender.UserName,
                RecipientUsername = recipient.UserName,
                MessageSent = DateTime.UtcNow,
                Content = createMessageDto.Content,
            };

            var groupName = GetGroupName(sender.UserName, recipient.UserName);
            var group = await _messageRepo.GetMessageGroup(groupName);

            if (group.Connections.Any(x => x.Username == recipient.UserName))
            {
                message.DateRead = DateTime.UtcNow;
            }
            else
            {
                var connections = await _tracker.GetConnectionForUser(recipient.UserName);

                if (connections != null)
                {
                    await _presenceHub.Clients.Clients(connections).SendAsync("NewMessageReceived", new
                    {
                        username = sender.UserName,
                        knownAs = sender.KnownAs,
                    });
                }
            }

            _messageRepo.AddMessage(message);

            if (!await _unitOfWork.Complete())
            {
                throw new HubException("Failed to send message");
            }

            var data = _mapper.Map<MessageDto>(message);

            await Clients.Group(groupName).SendAsync("NewMessage", _mapper.Map<MessageDto>(message));
        }

        private async Task<Group> RemoveFromGroup()
        {
            var group = await _messageRepo.GetGroupForConnection(Context.ConnectionId);

            if (group == null)
            {
                throw new HubException("Group not found");
            }

            var conneciton = group.Connections.FirstOrDefault(c => c.ConnectionId == Context.ConnectionId);

            if (conneciton == null)
            {
                return group;
            }

            group.Connections.Remove(conneciton);

            if (!await _unitOfWork.Complete())
            {
                throw new HubException("Failed to remove from group");
            }

            return group;
        }

        private async Task<Group> AddToGroup(string groupName)
        {
            var username = Context.GetHttpContext().User.GetUsername();
            var group = await _messageRepo.GetMessageGroup(groupName);
            var connection = new Connection(Context.ConnectionId, username);

            if (group == null)
            {
                group = new Group(groupName);
                _messageRepo.AddGroup(group);
            }

            group.Connections.Add(connection);

            if (!await _unitOfWork.Complete())
            {
                throw new HubException("Failed to join group");
            }

            return group;
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;

            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}