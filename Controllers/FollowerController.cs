using Microsoft.AspNetCore.Mvc;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
    [Route("follower")]
    public class FollowerController : Controller
    {

        private readonly FollowerService _followerService; // injecting the follower service

        
        public FollowerController(FollowerService followerService)
        {
            _followerService = followerService;
        }

        [HttpPost("follow/{followingId}")]
        public async Task<IActionResult> Follow(int followingId)
        { // follow method
            int userId = int.Parse(Request.Cookies["userId"]); // checking for logged user

            if(userId == 0)
            {
                return Unauthorized();
            }

            await _followerService.FollowUser(userId, followingId); // casting the follow user method through the services

            return Ok();

        }
        [HttpPost("unfollow/{followingId}")]
        public async Task<IActionResult> Unfollow(int followingId)
        { // follow method
            int userId = int.Parse(Request.Cookies["userId"]); // checking for logged user

            if (userId == 0)
            {
                return Unauthorized();
            }

            await _followerService.UnfollowUser(userId, followingId); // casting the unfollow user method through the services

            return Ok();

        }

    }
}
