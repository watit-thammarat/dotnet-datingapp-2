using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IUserRepository _userRepo;
        private readonly IMessageRepository _messageRepo;
        private readonly IMapper _mapper;

        public MessagesController(IUserRepository userRepo, IMessageRepository messageRepo, IMapper mapper)
        {
            _mapper = mapper;
            _messageRepo = messageRepo;
            _userRepo = userRepo;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var username = User.GetUsername();

            if (username.ToLower() == createMessageDto.RecipientUsername.ToLower())
            {
                return BadRequest(new ProblemDetails { Title = "You cannot sent message to yourself" });
            }

            var sender = await _userRepo.GetUserByUsernameAsync(username);
            var recipient = await _userRepo.GetUserByUsernameAsync(createMessageDto.RecipientUsername);

            if (recipient == null)
            {
                return BadRequest(new ProblemDetails { Title = "Recipient not found" });
            }

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUserName = sender.UserName,
                RecipientUsername = recipient.UserName,
                MessageSent = DateTime.Now,
                Content = createMessageDto.Content,
            };

            _messageRepo.AddMessage(message);

            if (!await _messageRepo.SaveAllAsync())
            {
                return BadRequest(new ProblemDetails { Title = "Failed to send message" });
            }

            var data = _mapper.Map<MessageDto>(message);

            return Ok(data);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser([FromQuery] MessageParams messageParams)
        {
            messageParams.Username = User.GetUsername();

            var data = await _messageRepo.GetMessagesForUser(messageParams);

            Response.AddPaginationHeader(data.CurrentPage, data.PageSize, data.TotalCount, data.TotalPages);

            return Ok(data);
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username)
        {
            var currentUsername = User.GetUsername();
            var data = await _messageRepo.GetMessageThread(currentUsername, username);

            return Ok(data);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUsername();
            var message = await _messageRepo.GetMessage(id);

            if (message == null)
            {
                return NotFound();
            }

            if (message.SenderUserName != username && message.RecipientUsername != username)
            {
                return Unauthorized();
            }

            if (message.SenderUserName == username)
            {
                message.SenderDeleted = true;
            }

            if (message.RecipientUsername == username)
            {
                message.RecipientDeleted = true;
            }

            if (message.SenderDeleted && message.RecipientDeleted)
            {
                _messageRepo.DeleteMessage(message);
            }

            if (!await _messageRepo.SaveAllAsync())
            {
                return BadRequest(new ProblemDetails { Title = "Failed to delete message" });
            }

            return Ok();
        }

    }
}