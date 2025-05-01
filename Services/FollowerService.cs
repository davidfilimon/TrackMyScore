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
        {
            if (followerId == followingId) return;

            // verifies if both parties exist
            var followerExists = await _context.Users.AnyAsync(u => u.Id == followerId);
            var followingExists = await _context.Users.AnyAsync(u => u.Id == followingId);

            if(!followerExists || !followingExists)
            {
                Console.WriteLine($"One of the users does not exist, Follower: {followerId}, Following: {followingId}.");
            }

            // searches for any existing follow relation towards the followed person
            var exists = await _context.Followers.AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId); // bool value
            if (!exists)
            {
                _context.Followers.Add(new Follower
                {
                    FollowerId = followerId,
                    FollowingId = followingId,
                    FollowedAt = DateTime.UtcNow
                    
                });

                await _context.SaveChangesAsync();
            }
        }

        public async Task UnfollowUser(int followerId, int followingId)
        {
            // searches for the follow relation
            var follow = await _context.Followers.FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId); // follower object value

            // if it exists it is removed
            if (follow != null)
            {
                _context.Followers.Remove(follow);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<User>> GetFollowers(int userId)
        {
            return await _context.Followers
                .Where(f => f.FollowingId == userId) // searches for matching users that follow the user
                .Join(
                _context.Users,
                f => f.FollowerId,
                u => u.Id,
                (f, u) => u)
                .ToListAsync();
        }

        public async Task<List<User>> GetFollowing(int userId)
        {
            return await _context.Followers
                .Where(f => f.FollowerId == userId) // searches for matching users that follow the user
                .Join(
                _context.Users,
                f => f.FollowingId,
                u => u.Id,
                (f, u) => u)
                .ToListAsync();
        }


        public async Task<int> GetNumberFollowers(int userId)
        {
            return await _context.Followers
                .CountAsync(f => f.FollowingId == userId);
        }
        public async Task<int> GetNumberFollowing(int userId)
        {
            return await _context.Followers.CountAsync(f => f.FollowerId == userId);
        }

        public async Task<bool> IsFollowing(int userId, int followingId)
        {
            return await _context.Followers
                .AnyAsync(f => f.FollowerId == userId && f.FollowingId == followingId); // returning true if it is following
        }

    }
}
