using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public void AddMessage(Message message)
        {
            _context.Messages.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Message> GetMessage(int id)
        {
            return await _context.Messages.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUser(MessageParams messageParams)
        {
            var query = _context.Messages
                .OrderByDescending(m => m.MessageSent)
                .AsQueryable();

            query = messageParams.Container switch
            {
                "Inbox" => query.Where(m => m.Recipient.UserName == messageParams.Username),
                "Outbox" => query.Where(m => m.Sender.UserName == messageParams.Username),
                _ => query.Where(m => m.Recipient.UserName == messageParams.Username && !m.RecipientDeleted && m.DateRead == null),
            };

            var resultQuery = query.ProjectTo<MessageDto>(_mapper.ConfigurationProvider).AsNoTracking();

            return await PagedList<MessageDto>.CreateAsync(resultQuery, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThread(string currentUsername, string recipientUsername)
        {
            var query = _context.Messages
                .Where(m =>
                    (m.SenderUserName == currentUsername && !m.SenderDeleted && m.RecipientUsername == recipientUsername) ||
                    (m.SenderUserName == recipientUsername && m.RecipientUsername == currentUsername && !m.RecipientDeleted)
                );

            var unreadMessages = await query
                .Where(m => m.DateRead == null && m.RecipientUsername == currentUsername)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var m in unreadMessages)
                {
                    m.DateRead = DateTime.Now;
                }

                await _context.SaveChangesAsync();
            }

            var resultQuery = query
                .Include(m => m.Sender).ThenInclude(u => u.Photos)
                .Include(m => m.Recipient).ThenInclude(u => u.Photos)
                .OrderBy(m => m.MessageSent)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider);

            return await resultQuery.ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}