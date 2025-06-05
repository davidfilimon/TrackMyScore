using TrackMyScore.Database;

namespace TrackMyScore.Models{
    public class TournamentListView {
        public List<Tournament> AvailableTournaments { get; set; }
        public List<Tournament> JoinedTournaments { get; set; }
        public List<Tournament> MyTournaments { get; set; }


        public TournamentListView()
        {

        }
    }
}