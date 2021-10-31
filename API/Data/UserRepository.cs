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
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public UserRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public async Task<AppUser> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(x => x.Photos)
                .FirstOrDefaultAsync(x => x.UserName == username);
        }

        public async Task<IEnumerable<AppUser>> GetUsersAsync()
        {
            return await _context.Users
                .Include(x => x.Photos)
                .ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public void Update(AppUser user)
        {
            _context.Users.Update(user);
        }

        public async Task<MemberDto> GetMemberAsync(string username)
        {
            return await _context.Users
                .Where(x => x.UserName == username)
                .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
                .FirstAsync();
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync(UserParams userParams)
        {
            var minDob = DateTime.Today.AddYears((-1 * userParams.MaxAge) - 1);
            var maxDob = DateTime.Today.AddYears(-1 * userParams.MinAge);

            var query = _context.Users
                .Where(
                    u => u.UserName != userParams.CurrentUsername &&
                    u.Gender == userParams.Gender &&
                    (u.DateOfBirth >= minDob && u.DateOfBirth <= maxDob)
                )
                .OrderBy(x => x.Id);

            query = userParams.OrderBy switch
            {
                "created" => query.OrderByDescending(u => u.Created),
                _ => query.OrderByDescending(u => u.LastActive),
            };

            var resultQuery = query
                .ProjectTo<MemberDto>(_mapper.ConfigurationProvider)
                .AsNoTracking()
                .AsQueryable();

            var members = await PagedList<MemberDto>.CreateAsync(resultQuery, userParams.PageNumber, userParams.PageSize);

            return members;
        }
    }
}