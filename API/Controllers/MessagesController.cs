
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class MessagesController : BaseApiController
    {
        //private readonly IUserRepository _userRepository;
        //private readonly IMessageRepository _messageRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public MessagesController(IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            //this._userRepository = userRepository;
            //this._messageRepository = messageRepository;
            this._unitOfWork = unitOfWork;
            this._mapper = mapper;
        }

        [HttpPost]
        public async Task<ActionResult<MessageDto>> CreateMessage(CreateMessageDto createMessageDto)
        {
            var userName = User.GetUserName();

            if (userName == createMessageDto.RecipientUserName.ToLower())
                return BadRequest("You cannpt send the message to yourself");

            var sender = await _unitOfWork.UserRepository.GetUserbyUsernameAsync(userName);
            var recipient = await _unitOfWork.UserRepository.GetUserbyUsernameAsync(createMessageDto.RecipientUserName);

            if (recipient == null) return NotFound();

            var message = new Message
            {
                Sender = sender,
                Recipient = recipient,
                SenderUserName = sender.UserName,
                RecipientUserName = recipient.UserName,
                Content = createMessageDto.Content
            };

            _unitOfWork.MessageRepository.AddMessage(message);
            if(await _unitOfWork.Complete())
                return Ok(_mapper.Map<MessageDto>(message));

            return BadRequest("Failed to send message");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MessageDto>>> GetMessageForUser([FromQuery] MessageParams messageParams)
        {
            messageParams.UserName = User.GetUserName();

            var messages = await _unitOfWork.MessageRepository.GetMessagesForUserAsync(messageParams);

            Response.AddPaginationHeader(new PaginationHeader(messages.CurrentPage, messages.PageSize,
                messages.TotalCount, messages.TotalPages));

            return Ok(messages);
        }

        //[HttpGet("thread/{username}")]
        // no longer used now
        //public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username)
        //{
        //    var currentUserName = User.GetUserName();
        //    return Ok(await _unitOfWork.MessageRepository.GetMessageThread(currentUserName, username));
        //}

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var userName = User.GetUserName();
            var message = await _unitOfWork.MessageRepository.GetMessageAsync(id);
            if (message.SenderUserName != userName && message.RecipientUserName != userName)
                return Unauthorized();

            if (message.SenderUserName == userName) message.SenderDeleted = true;
            if (message.RecipientUserName == userName) message.RecipientDeleted = true;

            if(message.SenderDeleted && message.RecipientDeleted)
            {
                _unitOfWork.MessageRepository.DeleteMessage(message);
            }

            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Problem deleting the message");
        }
    }
}
