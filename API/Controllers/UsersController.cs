using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        //private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private IPhotoService _photoService;

        public UsersController(IUnitOfWork unitOfWork, IMapper mapper, IPhotoService photoService)
        {
            //_userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _photoService = photoService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDto>>> GetUsers([FromQuery]UserParams userParams)
        {
            var currentUserGender = await _unitOfWork.UserRepository.GetUserGender(User.GetUserName());
            userParams.CurrentUserName = User.GetUserName();

            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = currentUserGender.ToLower() == "male" ? "female" : "male";
            }

            var users = await _unitOfWork.UserRepository.GetMemebrsAsync(userParams);
            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage, users.PageSize,
                users.TotalCount, users.TotalPages));
            return this.Ok(users);
        }

        [HttpGet("{username}", Name = "GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            return await _unitOfWork.UserRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var user = await _unitOfWork.UserRepository.GetUserbyUsernameAsync(User.GetUserName());
            _mapper.Map(memberUpdateDto, user);
            //_userRepository.Update(user);

            if (await _unitOfWork.Complete()) return NoContent();

            return BadRequest("Failed to Update User");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _unitOfWork.UserRepository.GetUserbyUsernameAsync(User.GetUserName());
            if (user == null) return NotFound();

            var result = await _photoService.AddPhotoAsync(file);
            if(result.Error !=  null)
            {
                return BadRequest(result.Error.Message);
            }

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }

            user.Photos.Add(photo);
            
            if (await _unitOfWork.Complete())
            {
                return CreatedAtRoute("GetUser", new { username = user.UserName }, _mapper.Map<PhotoDto>(photo));
            }

            return BadRequest("Problem Adding Photo");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await this._unitOfWork.UserRepository.GetUserbyUsernameAsync(User.GetUserName());
            if (user == null) return NotFound();

            var photo = user.Photos.FirstOrDefault(t => t.Id == photoId);
            if(photo == null) return NotFound();
            if (photo.IsMain) return BadRequest("This is already your main photo");

            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if (currentMain != null)
            {
                currentMain.IsMain = false;
            }
            photo.IsMain = true;

            if (await this._unitOfWork.Complete()) return NoContent();

            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await this._unitOfWork.UserRepository.GetUserbyUsernameAsync(User.GetUserName());
            var photo = user.Photos.FirstOrDefault(t => t.Id == photoId);

            if (photo == null) return NotFound();

            if (photo.IsMain) return BadRequest("You cannot delete your main photo");

            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            //// here we are just adding the tracking flag for the photo as the
            /// photos is a related entity of the user, we will update the user
            user.Photos.Remove(photo);

            if (await _unitOfWork.Complete()) return Ok();

            return BadRequest("Failed To delete the photo");
        }
    }
}
