using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordbler.Classes
{
    public class PlayerDetails
    {
        public string Name { get; set; }
        public Image Mascot { get; set; }
        public PlayerDetails(string name, Image mascot)
        {
            Name = name;
            Mascot = mascot;
        }
        public int TotalScore { get; set; }
        public List<TurnsWithScores> ScoreDetails = new List<TurnsWithScores>();
    }
}