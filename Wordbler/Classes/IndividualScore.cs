using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Wordbler.Classes
{
    public class IndividualScore
    {
        public char Letter { get; set; }
        public int Score { get; set; }

        public Point Axis { get; set; }

        private string preimumContent;
        public string PremiumContent
        {
            get { return preimumContent; }
            set                                             // E.g.: 2W, 3L etc.
            {
                if (string.IsNullOrEmpty(value))
                    preimumContent = string.Empty;
                else preimumContent = value;
            }
        }
        public IndividualScore(char letter, string premiumContent, Point axis)
        {
            Letter = letter;
            PremiumContent = premiumContent;
            Score = GetLetterScore();
            Axis = axis;
        }

        /// <summary>
        /// Assigns the letter score as soon as the constructor is called.
        /// Blank tile has a face value of 0.
        /// Other letters score according to specified rule.
        /// </summary>
        /// <returns></returns>
        private int GetLetterScore()
        {
            switch (Letter)
            {
                case ' ':           // 0 score for a blank tile.
                    return 0;
                case 'A':
                case 'E':
                case 'I':
                case 'O':
                case 'U':
                case 'L':
                case 'N':
                case 'S':
                case 'T':
                case 'R':
                    return 1;
                case 'D':
                case 'G':
                    return 2;
                case 'B':
                case 'C':
                case 'M':
                case 'P':
                    return 3;
                case 'F':
                case 'H':
                case 'V':
                case 'W':
                case 'Y':
                    return 4;
                case 'K':
                    return 5;
                case 'J':
                case 'X':
                    return 8;
                case 'Q':
                case 'Z':
                    return 10;
            }
            return -1;
        }
    }
}