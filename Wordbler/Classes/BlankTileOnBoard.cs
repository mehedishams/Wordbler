using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Wordbler.Classes
{
    class BlankTileOnBoard
    {
        public Point Cell { get; set; }
        public Color Colour { get; set; }
        public string PremiumContent { get; set; }

        public BlankTileOnBoard(Point cell, Color colour, string premiumContent)
        {
            Cell = cell;
            Colour = colour;
            PremiumContent = premiumContent;
        }
    }
}