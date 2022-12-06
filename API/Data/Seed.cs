using API.Entities;
using API.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.Data
{
    public class Seed
    {
        //// SeedUsers without Identity
        //public static async Task SeedUsers(DataContext context)
        //{
        //    if (await context.Users.AnyAsync()) return;

        //    var UserData = await System.IO.File.ReadAllTextAsync("Data/UserSeedData.json");
        //    var users = JsonSerializer.Deserialize<List<AppUser>>(UserData);
        //    foreach (var user in users)
        //    {
        //        using var hmac = new HMACSHA512();
        //        user.UserName = user.UserName.ToLower();
        //        user.PassswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("passwordbob"));
        //        user.PasswordSalt = hmac.Key;

        //        context.Users.Add(user);
        //    }

        //    await context.SaveChangesAsync();
        //}

        public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {

            if (await userManager.Users.AnyAsync()) return;

            var UserData = await System.IO.File.ReadAllTextAsync("Data/UserSeedData.json");
            var users = JsonSerializer.Deserialize<List<AppUser>>(UserData);
            var roles = new List<AppRole> 
            {
                new AppRole {Name = Constants.MemberRole},
                new AppRole {Name = Constants.AdminRole},
                new AppRole {Name = Constants.ModeratorRole}
            };

            foreach (var role in roles)
            {
                await roleManager.CreateAsync(role);
            }

            foreach (var user in users)
            {
                user.UserName = user.UserName.ToLower();
                await userManager.CreateAsync(user, "Pa$$w0rd");
                await userManager.AddToRoleAsync(user, Constants.MemberRole);
            }

            var admin = new AppUser
            {
                UserName = "admin"
            };

            await userManager.CreateAsync(admin, "Pa$$w0rd");
            await userManager.AddToRolesAsync(admin, new[] { Constants.AdminRole, Constants.ModeratorRole });
        }
    }
}
