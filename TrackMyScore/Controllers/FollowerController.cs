using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TrackMyScore.Models;
using TrackMyScore.Services;

namespace TrackMyScore.Controllers
{
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
                return RedirectToAction("Login", "Account");
            }

            await _followerService.FollowUser(userId, followingId);

            return RedirectToAction("Profile", "Account", new { id = followingId });

        }

        [HttpPost("unfollow/{followingId}")]
        public async Task<IActionResult> Unfollow(int followingId)
        {
            int userId = int.Parse(Request.Cookies["userId"]);

            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }


            await _followerService.UnfollowUser(userId, followingId);

            return RedirectToAction("Profile", "Account", new {id = followingId});

        }

        


    }
}
