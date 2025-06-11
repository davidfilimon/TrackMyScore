using Microsoft.EntityFrameworkCore;
using TrackMyScore.Database;

public class DashboardStats
{ // initialize the stats
    public int TotalUsers { get; set; }
    public int UsersLastDay { get; set; }
    public int UsersLastWeek { get; set; }
    public int UsersLastMonth { get; set; }

    public int TotalMatches { get; set; }
    public int MatchesLastDay { get; set; }
    public int MatchesLastWeek { get; set; }
    public int MatchesLastMonth { get; set; }

    public int TotalTournaments { get; set; }
    public int TournamentsLastDay { get; set; }
    public int TournamentsLastWeek { get; set; }
    public int TournamentsLastMonth { get; set; }
}

public class StatsService
{
    private readonly AppDbContext _db;
    public StatsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var now     = DateTime.UtcNow;
        var today   = now.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo= today.AddMonths(-1);

        return new DashboardStats
        {
            // users
            TotalUsers = await _db.Users.CountAsync(),
            UsersLastDay = await _db.Users.CountAsync(u => u.AccountCreationDate >= DateOnly.FromDateTime(today)),
            UsersLastWeek = await _db.Users.CountAsync(u => u.AccountCreationDate >= DateOnly.FromDateTime(weekAgo)),
            UsersLastMonth = await _db.Users.CountAsync(u => u.AccountCreationDate >= DateOnly.FromDateTime(monthAgo)),

            // matches
            TotalMatches = await _db.Matches.CountAsync(),
            MatchesLastDay = await _db.Matches.CountAsync(m => m.StartDate >= today),
            MatchesLastWeek = await _db.Matches.CountAsync(m => m.StartDate >= weekAgo),
            MatchesLastMonth = await _db.Matches.CountAsync(m => m.StartDate >= monthAgo),

            // tournaments
            TotalTournaments = await _db.Tournaments.CountAsync(),
            TournamentsLastDay = await _db.Tournaments.CountAsync(t => t.StartDate >= today),
            TournamentsLastWeek = await _db.Tournaments.CountAsync(t => t.StartDate >= weekAgo),
            TournamentsLastMonth = await _db.Tournaments.CountAsync(t => t.StartDate >= monthAgo),
        };
    }
}
