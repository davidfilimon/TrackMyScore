using Microsoft.EntityFrameworkCore;
using TrackMyScore.Models;

namespace TrackMyScore.Database
{
    public class AppDbContext : DbContext
    {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {    
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Follower> Followers { get; set; }
        public DbSet<FavoriteGame> FavoriteGames { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<JoinRoom> JoinRooms { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Players> Players { get; set; }

    }
}
