using Wordbler.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordbler.Classes
{
    class RackCell : IRackCell
    {
        public int Player { get; set; }
        public int Cell { get; set; }
        public char Letter { get; set; }
        public RackCell() { }

        public RackCell(RackCell cell)
        {
            Player = cell.Player;
            Cell = cell.Cell;
            Letter = cell.Letter;
        }

        public RackCell(int player, int cell, char letter)
        {
            Player = player;
            Cell = cell;
            Letter = letter;
        }

        public void Add(int player, int cell, string letter)
        {
            Player = player;
            Cell = cell;
            Letter = string.IsNullOrEmpty(letter) ? '\0' : letter.ToCharArray()[0];
        }
    }
}