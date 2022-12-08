using API.DTOs;
using API.Entities;
using API.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Interfaces
{
    public interface IUserRepository
    {
        void Update(AppUser user);

        //Task<bool> SaveAllAsync();

        Task<IEnumerable<AppUser>> GetUsersAsync();

        Task<AppUser> GetUserByIdAsync(int Id);

        Task<AppUser> GetUserbyUsernameAsync(string username);

        Task<PagedList<MemberDto>> GetMemebrsAsync(UserParams userParams);

        Task<MemberDto> GetMemberAsync(string username, bool isCurrentUser);

        Task<String> GetUserGender(string userName);

        Task<AppUser> GetUserByPhotoId(int photoId);
    }
}
