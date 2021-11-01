using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly IUserRepository _userRepo;
        private readonly ILikesRepository _likeRepo;
        private readonly IUnitOfWork _unitOfWork;

        public LikesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _likeRepo = unitOfWork.LikesRepository;
            _userRepo = unitOfWork.UserRepository;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> AddLike(string username)
        {
            var sourceUserId = User.GetUserId();
            var sourceUser = await _likeRepo.GetUserWithLikes(sourceUserId);

            if (sourceUser.UserName == username)
            {
                return BadRequest(new ProblemDetails { Title = "You cannot like yourself" });
            }

            var likedUser = await _userRepo.GetUserByUsernameAsync(username);

            if (likedUser == null)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid liked username" });
            }

            var userLike = await _likeRepo.GetUserLike(sourceUserId, likedUser.Id);

            if (userLike != null)
            {
                return BadRequest(new ProblemDetails { Title = "You already like this user" });
            }

            userLike = new UserLike
            {
                LikedUser = likedUser
            };

            sourceUser.LikedUsers.Add(userLike);

            if (!await _unitOfWork.Complete())
            {
                return BadRequest(new ProblemDetails { Title = "Failed to like user" });
            }

            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDto>>> GetUserLike([FromQuery] LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();

            var userLikes = await _likeRepo.GetUserLikes(likesParams);

            Response.AddPaginationHeader(userLikes.CurrentPage, userLikes.PageSize, userLikes.TotalCount, userLikes.TotalPages);

            return Ok(userLikes);
        }
    }
}