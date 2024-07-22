using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wordbler.Classes
{
    class Letter
    {
        public char Alphabet { get; set; }
        public int Pos { get; set; }

        public Letter(char letter, int pos)
        {
            Alphabet = letter;
            Pos = pos;
        }
    }
}