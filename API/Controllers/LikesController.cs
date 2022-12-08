using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class LikesController : BaseApiController
    {
        //private IUserRepository _userRepository;
        //private ILikesRepository _likesRepository;
        private IUnitOfWork _unitOfWork;

        public LikesController(IUnitOfWork unitOfWork)
        {
            //_userRepository = userRepository;
            //_likesRepository = likesRepository;
            _unitOfWork = unitOfWork;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username)
        {
            username = username.ToLower();
            var sourceUserId = User.GetUserId();
            var likedUser = await _unitOfWork.UserRepository.GetUserbyUsernameAsync(username);
            var sourceUser = await _unitOfWork.LikesRepository.GetUserWithLikes(sourceUserId);

            if (likedUser == null) return NotFound();

            if (sourceUser.UserName == username)
                return BadRequest("You cannot like yourself");

            var userLike = await _unitOfWork.LikesRepository.GetUserLike(sourceUserId, likedUser.Id);

            if (userLike != null)
                return BadRequest("You have already liked the user");

            userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                TargetUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);
            if (await _unitOfWork.Complete()) return Ok();
            return BadRequest("Failed to like user");
        }

        [HttpGet]
        public async Task<ActionResult<PagedList<LikeDto>>> GetUserLikes([FromQuery]LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();
            var users = await _unitOfWork.LikesRepository.GetUserLikes(likesParams);
            Response.AddPaginationHeader(new PaginationHeader(users.CurrentPage,
                users.PageSize, users.TotalCount, users.TotalPages));

            return Ok(users);
        }
    }
}
