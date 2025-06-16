using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TrackMyScore.Database;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
    [Route("follower")]
    public class FollowerController : Controller
    {

        private readonly FollowerService _followerService; // injecting the follower service

        private readonly AppDbContext _context;


        public FollowerController(FollowerService followerService, AppDbContext context)
        {
            _followerService = followerService;
            _context = context;
        }

        [HttpPost("follow/{followingId}")]
        public async Task<IActionResult> Follow(int followingId)
        { // follow method
            var user = await GetLoggedUserAsync();

            await _followerService.FollowUser(user.Id, followingId); // casting the follow user method through the services

            return Ok();

        }
        [HttpPost("unfollow/{followingId}")]
        public async Task<IActionResult> Unfollow(int followingId)
        { // follow method
            var user = await GetLoggedUserAsync();

            await _followerService.UnfollowUser(user.Id, followingId); // casting the unfollow user method through the services

            return Ok();

        }
        
        private async Task<User> GetLoggedUserAsync()
        { // method for getting the logged user
            string email = HttpContext.Session.GetString("email");

            if (email == null)
            {
                return null;
            }

            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

    }
}
