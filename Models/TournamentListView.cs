using TrackMyScore.Database;

namespace TrackMyScore.Models{
    public class TournamentListView{
        public List<Tournament> Tournaments { get; set; }
        public List<Tournament> MyTournaments { get; set; }


        public TournamentListView(List<Tournament> tournaments, List<Tournament> myTournaments){
            Tournaments = tournaments;
            MyTournaments = myTournaments;
        }
    }
}