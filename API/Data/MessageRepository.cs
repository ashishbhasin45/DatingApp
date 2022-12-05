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
            var messages = await _dataContext.Messages
                .Include(u => u.Sender).ThenInclude(p => p.Photos)
                .Include(u => u.Recipient).ThenInclude(p => p.Photos)
                .Where(
                    m => m.RecipientUserName == recipientUsername &&
                    m.SenderDeleted == false && m.SenderUserName == currentUsername ||
                    m.RecipientUserName == currentUsername && m.RecipientDeleted == false &&
                    m.SenderUserName == recipientUsername
                )
                .OrderBy(m => m.MessageSent)
                .ToListAsync();

            var unreadMessages = messages.Where(m => m.DateRead == null
                && m.RecipientUserName == currentUsername).ToList();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }

                await _dataContext.SaveChangesAsync();
            }

            return _mapper.Map<IEnumerable<MessageDto>>(messages);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _dataContext.SaveChangesAsync() > 0;
        }
    }
}
