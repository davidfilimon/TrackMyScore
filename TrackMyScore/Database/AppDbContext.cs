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

    }
}
