using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private DataContext _dataContext;
        private readonly IMapper _mapper;

        public MessageRepository(DataContext dataContext, IMapper mapper)
        {
            _dataContext = dataContext;
            this._mapper = mapper;
        }

        public void AddMessage(Message message)
        {
            _dataContext.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _dataContext.Messages.Remove(message);
        }

        public async Task<Message> GetMessageAsync(int messageId)
        {
            return await _dataContext.Messages.FindAsync(messageId);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUserAsync(MessageParams messageParams)
        {
            var query = _dataContext.Messages
                .OrderByDescending(x => x.MessageSent)
                .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(u => u.RecipientUserName == messageParams.UserName 
                && u.RecipientDeleted == false),
                "Outbox" => query.Where(u => u.SenderUserName == messageParams.UserName 
                && u.SenderDeleted == false),
                _ => query.Where(u => u.RecipientUserName == messageParams.UserName
                && u.RecipientDeleted == false && u.DateRead == null)
            };

            var messages = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

            return await PagedList<MessageDto>
                .CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var query = _dataContext.Messages
                .Where(
                    m => m.RecipientUserName == recipientUsername &&
                    m.SenderDeleted == false && m.SenderUserName == currentUsername ||
                    m.RecipientUserName == currentUsername && m.RecipientDeleted == false &&
                    m.SenderUserName == recipientUsername
                )
                .OrderBy(m => m.MessageSent)
                .AsQueryable();
                

            var unreadMessages = query.Where(m => m.DateRead == null
                && m.RecipientUserName == currentUsername).ToList();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }
            }

            return await query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider).ToListAsync();
        }

        //public async Task<bool> SaveAllAsync()
        //{
        //    return await _dataContext.SaveChangesAsync() > 0;
        //}

        public void AddGroup(Group group)
        {
            _dataContext.Groups.Add(group);
        }

        public void RemoveConnection(Connection connection)
        {
            _dataContext.Connections.Remove(connection);
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            return await _dataContext.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await _dataContext.Groups
                .Include(g => g.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _dataContext.Groups
                .Include(x => x.Connections)
                .Where(x => x.Connections.Any(c => c.ConnectionId == connectionId))
                .FirstOrDefaultAsync();
        }
    }
}
