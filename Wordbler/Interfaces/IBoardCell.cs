using System.Drawing;

namespace Wordbler.Interfaces
{
    interface IBoardCell
    {
        int X { get; set; }
        int Y { get; set; }
        string PremiumContent { get; set; }
        string AfterDragCellContent { get; set; }
    }
}
