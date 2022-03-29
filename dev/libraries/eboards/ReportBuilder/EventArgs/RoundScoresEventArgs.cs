using System.Collections.Generic;

namespace ReportBuilder.EventArgs
{
    public class RoundScoresEventArgs<T> : System.EventArgs where T : RoundScores
    {
        public List<T> RoundScores { get; }

        public RoundScoresEventArgs (List<T> roundScores)
        {
            RoundScores = roundScores;
        }
    }
}
