using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        //[HttpPost("register-without-identity")]
        //public async Task<ActionResult<UserDto>> Register(RegisterDto newUser)
        //{
        //    if (await UserExists(newUser.Username)) return BadRequest("Username is already taken");

        //    var user = _mapper.Map<AppUser>(newUser);
        //    using var hmac = new HMACSHA512();

        //    user.UserName = newUser.Username.ToLower();
        //    user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(newUser.Password));
        //    user.PasswordSalt = hmac.Key;

        //    _dataContext.Users.Add(user);
        //    await _dataContext.SaveChangesAsync();

        //    return new UserDto
        //    {
        //        Username = user.UserName,
        //        Token = _tokenService.CreateToken(user),
        //        KnownAs = user.KnownAs,
        //        Gender = user.Gender
        //    };
        //}

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto newUser)
        {
            if (await UserExists(newUser.Username)) return BadRequest("Username is already taken");

            var user = _mapper.Map<AppUser>(newUser);

            user.UserName = newUser.Username.ToLower();
            var result = await _userManager.CreateAsync(user, newUser.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);
            var roleResult = await _userManager.AddToRoleAsync(user, "Member");
            if (!roleResult.Succeeded) return BadRequest(roleResult.Succeeded);

            return new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }

        //[HttpPost("login-without-identity")]
        //public async Task<ActionResult<UserDto>> Login(LoginDto loginInfo)
        //{
        //    //// Eager loading for the photos in User using Include.
        //    var user = await this._dataContext.Users
        //        .Include(p => p.Photos)
        //        .SingleOrDefaultAsync(x => x.UserName == loginInfo.Username);
        //    if (user == null) return Unauthorized("Invalid User");

        //    using var hmac = new HMACSHA512(user.PasswordSalt);

        //    var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginInfo.Password));

        //    if (!Enumerable.SequenceEqual(computedHash, user.PasswordHash))
        //    {
        //        return Unauthorized("Invalid Password"); ;
        //    }

        //    return new UserDto
        //    {
        //        Username = user.UserName,
        //        Token = _tokenService.CreateToken(user),
        //        PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
        //        KnownAs = user.KnownAs,
        //        Gender = user.Gender
        //    };
        //}

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginInfo)
        {
            //// Eager loading for the photos in User using Include.
            var user = await this._userManager.Users
                .Include(p => p.Photos)
                .SingleOrDefaultAsync(x => x.UserName == loginInfo.Username);
            if (user == null) return Unauthorized("Invalid User");
            var result = await _userManager.CheckPasswordAsync(user, loginInfo.Password);
            if (!result) return Unauthorized("Invalid password");

            return new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url,
                KnownAs = user.KnownAs,
                Gender = user.Gender
            };
        }


        private async Task<bool> UserExists(string username)
        {
            return await this._userManager.Users.AnyAsync(t => t.UserName == username.ToLower());
        }
    }
}
