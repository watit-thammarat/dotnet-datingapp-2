using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepo;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;
        private readonly IUnitOfWork _unitOfWork;

        public UsersController(IMapper mapper, IPhotoService photoService, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _photoService = photoService;
            _userRepo = unitOfWork.UserRepository;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery] UserParams userParams)
        {
            var username = User.GetUsername();
            var gender = await _userRepo.GetUserGender(username);

            userParams.CurrentUsername = username;

            if (string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = gender == "male" ? "female" : "male";
            }

            var members = await _userRepo.GetMembersAsync(userParams);

            Response.AddPaginationHeader(userParams.PageNumber, userParams.PageSize, members.TotalCount, members.TotalPages);

            return Ok(members);
        }

        [Authorize(Roles = "Member,Admin")]
        [HttpGet("{username}")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            var user = await _userRepo.GetMemberAsync(username);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var username = User.GetUsername();
            var user = await _userRepo.GetUserByUsernameAsync(username);

            _mapper.Map(memberUpdateDto, user);
            _userRepo.Update(user);

            if (!await _unitOfWork.Complete())
            {
                return BadRequest(new ProblemDetails { Title = "Failed to update user" });
            }

            return NoContent();
        }

        [HttpDelete("delete-photo/{id}")]
        public async Task<ActionResult> DeletePhoto(int id)
        {
            var username = User.GetUsername();
            var user = await _userRepo.GetUserByUsernameAsync(username);
            var photo = user.Photos.FirstOrDefault(x => x.Id == id);

            if (photo == null)
            {
                return NotFound();
            }

            if (photo.IsMain)
            {
                return BadRequest(new ProblemDetails { Title = "You cannot delete your main photo" });
            }

            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);

                if (result.Error != null)
                {
                    return BadRequest(new ProblemDetails { Title = result.Error.Message });
                }
            }

            user.Photos.Remove(photo);
            _userRepo.Update(user);

            if (!await _unitOfWork.Complete())
            {
                return BadRequest(new ProblemDetails { Title = "Failed to delete photo" });
            }


            return NoContent();
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var username = User.GetUsername();
            var user = await _userRepo.GetUserByUsernameAsync(username);
            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null)
            {
                return BadRequest(new ProblemDetails { Title = result.Error.Message });
            }

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId,
                IsMain = user.Photos.Count == 0,
            };

            user.Photos.Add(photo);

            if (!await _unitOfWork.Complete())
            {
                return BadRequest(new ProblemDetails { Title = "Problem adding photo" });
            }

            var data = _mapper.Map<PhotoDto>(photo);

            return Ok(data);
        }
    }
}