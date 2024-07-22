using System;
using System.Collections.Generic;
using System.Drawing;

namespace Wordbler.Classes
{
    public class ValidWordWithScore
    {
        public string Word { get; set; }
        public List<IndividualScore> Score { get; set; }
        public Point Axis { get; set; }
        public ValidWordWithScore(string word, List<IndividualScore> score, Point axis)
        {
            Word = word;
            Score = score;
            Axis = axis;
        }
    }
}