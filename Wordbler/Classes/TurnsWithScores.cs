using System.Collections.Generic;
namespace Wordbler.Classes
{
    public class TurnsWithScores
    {
        public int Turn { get; set; }
        public string DetailedScore { get; set; }
        public List<ValidWordWithScore> ValidWords { get; set; }    // Needed to discard this word from points calculation of the next player if it comes in the line of calculation.
        public TurnsWithScores(int turn, string detailedScore, List<ValidWordWithScore> validWords)
        {
            Turn = turn;
            DetailedScore = detailedScore;
            ValidWords = validWords;
        }
    }
}