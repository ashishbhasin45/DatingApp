using API.Data;
using API.DTOs;
using API.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _dataContext;

        public AccountController(DataContext dataContext)
        {
            _dataContext = dataContext;
        }

        [HttpPost("register")]
        public async Task<ActionResult<AppUser>> Register(RegisterDto newUser)
        {
            if (await UserExists(newUser.Username)) return BadRequest("Username is already taken");

            using var hmac = new HMACSHA512();

            var user = new AppUser
            {
                UserName = newUser.Username.ToLower(),
                PassswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(newUser.Password)),
                PasswordSalt = hmac.Key
            };

            _dataContext.Users.Add(user);
            await _dataContext.SaveChangesAsync();

            return user;
        }
        
        [HttpPost("login")]
        public async Task<ActionResult<AppUser>> Login(LoginDto loginInfo)
        {
            var user = await this._dataContext.Users.SingleOrDefaultAsync(x => x.UserName == loginInfo.Username);
            if (user == null) return Unauthorized("Invalid User");

            using var hmac = new HMACSHA512(user.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginInfo.Password));

            if (!Enumerable.SequenceEqual(computedHash, user.PassswordHash))
            {
                return Unauthorized("Invalid User"); ;
            }

            return user;
        }

        private async Task<bool> UserExists(string username)
        {
            return await this._dataContext.Users.AnyAsync(t => t.UserName == username.ToLower());
        }
    }
}
