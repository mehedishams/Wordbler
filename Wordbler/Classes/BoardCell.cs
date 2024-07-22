using Wordbler.Interfaces;

namespace Wordbler.Classes
{
    class BoardCell : IBoardCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string PremiumContent { get; set; }
        public string AfterDragCellContent { get; set; }

        public BoardCell(int x, int y, string premiumContent, string afterDragCellContent)
        {
            X = x;
            Y = y;
            PremiumContent = premiumContent;
            AfterDragCellContent = afterDragCellContent;
        }
    }
}