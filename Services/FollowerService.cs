using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using TrackMyScore.Database;
using TrackMyScore.Models;

namespace TrackMyScore.Services
{
    public class FollowerService
    {

        private readonly AppDbContext _context;

        public FollowerService(AppDbContext context)
        {
            _context = context;
        }

        public async Task FollowUser(int followerId, int followingId)
        { // following an user method
            if (followerId == followingId) return;

            // verifies if both parties exist
            var follower = await _context.Users.FirstOrDefaultAsync(u => u.Id == followerId);
            var following = await _context.Users.FirstOrDefaultAsync(u => u.Id == followingId);

            // searches for any existing follow relation towards the followed person
            var exists = await _context.Followers.AnyAsync(f => f.Follower.Id == followerId && f.Following.Id == followingId); // bool value
            if (!exists)
            {
                if(follower != null && following != null){
                _context.Followers.Add(new Followers
                {
                    Follower = follower,
                    Following = following,
                    FollowedAt = DateTime.UtcNow              
                });
                } else {
                    Console.WriteLine($"One of the users does not exist, Follower: {followerId}, Following: {followingId}.");
                }
            
                await _context.SaveChangesAsync();
            }
        }

        public async Task UnfollowUser(int followerId, int followingId)
        { // unfollowing an user method
            var follower = await _context.Users.FirstOrDefaultAsync(u => u.Id == followerId);
            var following = await _context.Users.FirstOrDefaultAsync(u => u.Id == followingId);
            // searches for the follow relation
            var follow = await _context.Followers.FirstOrDefaultAsync(f => f.Follower == follower && f.Following == following); // follower object value

            // if it exists it is removed
            if (follow != null)
            {
                _context.Followers.Remove(follow);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetFollowers(int userId)
        { // getting logged user's followers
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return await _context.Followers
                .Where(f => f.Following == user) // searches for matching users that follow the user
                .Select(f => f.Follower)
                .ToListAsync();
        }

        public async Task<List<User>> GetFollowing(int userId)
        { // getting logged user's followings
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return await _context.Followers
                .Where(f => f.Follower == user) // searches for matching users that follow the user
                .Select(f => f.Following) 
                .ToListAsync();
        }


        public async Task<int> GetNumberFollowers(int userId)
        { // number of followers
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            return await _context.Followers
                .CountAsync(f => f.Following == user);
        }
        public async Task<int> GetNumberFollowing(int userId)
        { // number of following
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            return await _context.Followers.CountAsync(f => f.Follower == user);
        }

        public async Task<bool> IsFollowing(int userId, int followingId)
        { // check the following relation between users
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var followingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == followingId);   
            return await _context.Followers
                .AnyAsync(f => f.Follower == user && f.Following == followingUser); // returning true if it is following
        }

    }
}
