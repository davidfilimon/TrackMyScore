using Microsoft.AspNetCore.Mvc;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
    [Route("follower")]
    public class FollowerController : Controller
    {

        private readonly FollowerService _followerService;

        
        public FollowerController(FollowerService followerService)
        {
            _followerService = followerService;
        }

        [HttpPost("follow/{followingId}")]
        public async Task<IActionResult> Follow(int followingId)
        {
            int userId = int.Parse(Request.Cookies["userId"]);

            if(userId == 0)
            {
                return Unauthorized();
            }

            await _followerService.FollowUser(userId, followingId);

            return Ok();

        }
        [HttpPost("unfollow/{followingId}")]
        public async Task<IActionResult> Unfollow(int followingId)
        {
            int userId = int.Parse(Request.Cookies["userId"]);

            if (userId == 0)
            {
                return Unauthorized();
            }

            await _followerService.UnfollowUser(userId, followingId);

            return Ok();

        }

    }
}
