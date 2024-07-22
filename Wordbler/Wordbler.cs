using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Wordbler.Classes;
using static Wordbler.Classes.Globals;
using System.Threading;
using System.Timers;
using System.Text;
using System.Reflection;

namespace Wordbler
{
    public partial class Wordbler : Form
    {        
        WindowScaler Scaler;                                        // Contains the scaling arithmatic for auto-resizing.
        Random Rnd;                                                 // Randomizer for random letter picking.
        int ScaleFactor, CalibrationFactor;                         // For auto-resizing of controls based on resolution.
        char[,] Matrix;                                             // The board mapping in the matrix.

        Stack<RackCell> RackCellList = new Stack<RackCell>();       // Stack of rack cells which were being dragged.
        Stack<BoardCell> BoardCellList = new Stack<BoardCell>();    // Stack of board cells where a letter is dropped.
        Queue<Letter> DrawnLetters = new Queue<Letter>();           // Only needed for the first player selection.
        List<Letter> ExchangingLetters = new List<Letter>();        // Lists the selected letters for exchange.
        private List<RackCell> BlankTiles = new List<RackCell>();   // Keeps track of the blank tiles on the rack. Needed for CTRL+Z and reverting letters.
        private List<BlankTileOnBoard> BoardBlankTiles = new List<BlankTileOnBoard>();   // Keeps track of the board cell premium content once a blank tile is dropped on them; used mainly for reverting and undoing.

        RackCell Source = new RackCell();                   // Records the rack cell details when the player starts dragging a tile.
        public char WildCard;                               // The current wildcard value as chosen by the player.
        int TotalPlayers;                                   // For keeping track of turns and activating/deactivating players.
        private int SHAKE_PIXELS = MAX_SHAKE_PIXELS;        // How many pixels to shake. Resets to MAX_SHAKE_PIXELS after shakng.
        private int SHAKE_COUNT;                            // For bag shaking. Counts up to 13.
        private int FRAME_COUNT;                            // For flying animation. Counts up to 25.
        private bool StarCheckNeeded;                       // The first word should pass through the star. Rest of the words doesn't need to pass through the star.
        private int Failcount;                              // Keeps track of number of consecutive failes. 6 consecutive fails end the game.
        private int GameWinner;                             // Needed for the firecrackers click event only. Contains the winning player - 0,1,2 or 3.
        private bool PremimumIdentifierToggle;              // Needed for when the a revert happens for invalid words and whether the board premium cells should retain the previous text (2W, 3W, 2L, 3L) or colour only.

        private Font DEFAULT_FONT = new Font("Microsoft Sans Serif", 12);
        private Font STAR_CELL_FONT = new Font("Microsoft Sans Serif", 48);
        private readonly Color BACK_COLOR_FOR_VALID_WORDS = SystemColors.ControlDarkDark;
        private readonly Color BACK_COLOR_FOR_BLANK_TILE = Color.Orchid;
        private readonly Color BACK_COLOR_EXCHANGE_LETTER_CELL = Color.Crimson;
        private readonly Color BACK_COLOR_3W = Color.Crimson;
        private readonly Color BACK_COLOR_2W = Color.Pink;
        private readonly Color BACK_COLOR_3L = Color.RoyalBlue;
        private readonly Color BACK_COLOR_2L = Color.LightBlue;
        private readonly Color BACK_COLOR_STAR_TILE = Color.Green;

        private double SteppingX;
        private double SteppingY;        
        static Point FlyingLetterInitialPosOnBag;          // The position of the flying letter on top of the bag. Set at form load.
        static Point LetterBagInitialPos;                  // The position of the bag on the form. Set at form load.
        double LastXOfFlyingLabel, LastYOfFlyingLabel;
        
        public Wordbler()
        {
            InitializeComponent();
        }

        private void Wordbler_Load(object sender, EventArgs e)
        {
            try
            {
                togglePremiumTextToolStripMenuItem_Click(sender, e);        // Toggle the cells to colour only - looks cleaner.
                toggleLettersLegendToolStripMenuItem_Click(sender, e);        // Toggle the letter values display.
                lblFailCount.Visible = false;
                StarCheckNeeded = PremimumIdentifierToggle = true;
                Failcount = 0;

                ScaleBoardIfNecessary(out Scaler, Screen.PrimaryScreen.Bounds, out ScaleFactor, out CalibrationFactor);
                Width = Scaler.GetMetrics(Width, "Width");                  // Form width and height set up.
                Height = Scaler.GetMetrics(Height, "Height");
                Left = Screen.GetBounds(this).Width / 2 - Width / 2;        // Form centering.
                Top = Screen.GetBounds(this).Height / 2 - Height / 2 - 30;  // 30 is a calibration factor.
                ScaleControls(Controls);
                ScaleFont();

                FlyingLetterInitialPosOnBag = new Point(lblFlyingLetter.Left, lblFlyingLetter.Top);    // Store the initial position of the flying label.
                LetterBagInitialPos = new Point(pbLetterBag.Left, pbLetterBag.Top);                    // Store the initial position of the letter bag.

                Matrix = new char[GRID_SIZE, GRID_SIZE];
                Rnd = new Random();
                SHAKE_COUNT = FRAME_COUNT = 0;

                GameEngine engine = GameEngine.GetInstance();       // Singleton.
                engine.LoadLetterBag();                             // Load letters in the bag.
                CurrentWordDirection = WordDirectionEnum.None;      // No orientation at the moment.
                LoadPlayers();                                      // Load players.

                // Deactivating all players; any value other than 0, 1, 2, 3 should deactivate all available players. Used -1 here.
                // Deactivating is needed to deny dragging of letters until the animation is complete.
                ActivateDeactivatePlayers(-1);
                timerBagShaking.Start();
                DelayAndProcessWorks(1200);

                CurrentPlayer = ResolveFirstPlayer();
                if (CurrentPlayer == -1)                            // This will never happen; but still as a catastrophe measure.
                {
                    MessageBox.Show($"First player could not be decided. The first player will start the game."
                                    , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CurrentPlayer = 0;
                }

                ClearTiles();
                MessageBox.Show($"Player {CurrentPlayer + 1} ({Players[CurrentPlayer].Name}) draws a letter closest to 'A' or draws a blank tile." +
                                $"S/he will start the game.", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReturnLetters();
                LoadLetters();
                ActivateDeactivatePlayers(CurrentPlayer);
                CurrentTurn = 1;    // Turns started.       
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred in the Wordbler_Load() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {ex.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }     
        }

        /// <summary>
        /// This is needed if a tile is placed at the very beginning of the game.
        /// </summary>
        private void ClearTiles()
        {
            try
            {
                Control ctl;
                for (int i = 0; i < Players.Count; i++)
                    for (int j = 0; j < MAX_LETTERS; j++)
                    {
                        ctl = Controls.Find($"p{i}l{j}", true)[0];
                        ctl.BackColor = SystemColors.Control;
                    }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ClearTiles() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Returns the letters to the bag after the first player is decided.
        /// </summary>
        private void ReturnLetters()
        {
            try
            {
                Letter l;
                Control ctl;
                Point cellAbsLoc;

                for (int i = 0; i < Players.Count; i++)
                {
                    ctl = Controls.Find($"p{i}l0", true)[0];    // E.g.: p2l0.                

                    // Obtain absolute location of the rack's cell from the top left of the window.
                    cellAbsLoc = ctl.FindForm().PointToClient(ctl.Parent.PointToScreen(ctl.Location));

                    l = DrawnLetters.Dequeue();
                    lblFlyingLetter.Text = ctl.ToString();
                    ctl.Text = string.Empty;
                    FlyLetter(cellAbsLoc, FlyingLetterInitialPosOnBag);

                    LetterBag[l.Pos] = l.Alphabet;
                    lblLettersRemaining.Text = $"Letters remaining: {++NUM_LETTERS_IN_BAG}";
                    Application.DoEvents();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ReturnLetters() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShakeBag()
        {
            try
            {
                for (int i = 0; i < MAX_SHAKE_COUNT; i++)
                {
                    pbLetterBag.Top += SHAKE_PIXELS;
                    SHAKE_PIXELS = -SHAKE_PIXELS;
                    Thread.Sleep(THREAD_SLEEP);
                    Application.DoEvents();
                }
                SHAKE_PIXELS = MAX_SHAKE_PIXELS;    // Reset the Shake pixels.
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ShakeBag() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Resolves who should be starting the game.
        /// If anybody draws a blank tile will be the first player to start.
        /// Else, the player who draws close to 'A' will be the first to start.
        /// If two players drawing the same letter close to 'A',
        /// then the first player (clockwise) is given the chance.
        /// </summary>
        /// <returns></returns>
        private int ResolveFirstPlayer()
        {
            try
            {
                // First draw a single letter to each of the player's first cell in the rack.            
                for (int i = 0; i < Players.Count; i++)
                    LoadSingleLetter(i, 0);

                // If any player draws a blank tile, s/he will be the first to start.
                // Visibility confirms if the player is available. Just to remind, if 3 players were to play,
                // then p3l0 will not be visible, hence will be out of context and consideration.
                if (p0l0.Visible && string.IsNullOrEmpty(p0l0.Text))
                    return 0;
                if (p1l0.Visible && string.IsNullOrEmpty(p1l0.Text))
                    return 1;
                if (p2l0.Visible && string.IsNullOrEmpty(p2l0.Text))
                    return 2;
                if (p3l0.Visible && string.IsNullOrEmpty(p3l0.Text))
                    return 3;

                Control ctl;
                int min = 999, distance;                                    // Start with a higher number.
                int minWinner = -1;                                         // Any number other than 0,1,2,3 will do.
                for (int i = 0; i < Players.Count; i++)                     // Loop through all players.
                {
                    ctl = Controls.Find($"p{i}l0", true)[0];                // E.g.: p2l0.
                    distance = ctl.Text.ToCharArray()[0] - 'A';
                    if (distance < min)                                     // E.g.: if text of p2l0 is 'E', then distance is 4.
                    {
                        min = distance;                                     // then make min the current distance,
                        minWinner = i;                                      // and change the minWinner to the current player.
                    }
                }
                return minWinner;                                           // Return the first player as resolved.
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ResolveFirstPlayer() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        /// <summary>
        /// Loads single letter from the letter bag and animates that letter to fly from the bag to the player's cell.
        /// </summary>
        private void LoadSingleLetter(int player, int cellNum)
        {
            try
            {
                char c;
                int index;
                Control cell;
                Point cellAbsLoc;   // For calculating absolute location of the rack's cell from the top left of the window.
                while (true)
                {
                    index = Rnd.Next(0, NUM_LETTERS_IN_BAG);    // Get a random index.
                    c = LetterBag[index];                       // Take the letter from the bag at that index.
                    if (c == '\0')                              // If it is a null character, then opt for the next letter.
                        continue;

                    if (StarCheckNeeded)                            // If it is only for resoving the first player,
                        DrawnLetters.Enqueue(new Letter(c, index)); // then keep track of the letter, you need to return them after the first player is decided.

                    lblFlyingLetter.Text = c.ToString();            // Set text of the flying letter as the letter obtained from the bag.
                    cell = Controls.Find($"p{player}l{cellNum}", true)[0];  // E.g.: p2l0.

                    cellAbsLoc = cell.FindForm().PointToClient(cell.Parent.PointToScreen(cell.Location));
                    FlyLetter(FlyingLetterInitialPosOnBag, cellAbsLoc);     // Animate the letter to fly from bag to rack cell.
                    cell.Text = lblFlyingLetter.Text;                       // Put the letter on the rack cell.

                    if (c == (char)32)                                      // If it is the blank tile,
                        cell.BackColor = BACK_COLOR_FOR_BLANK_TILE;         // then set the blank cell back colour.

                    lblFlyingLetter.Left = FlyingLetterInitialPosOnBag.X;   // Take the flying letter (airplane) back to the bag.
                    lblFlyingLetter.Top = FlyingLetterInitialPosOnBag.Y;
                    lblLettersRemaining.Text = $"Letters remaining: {--NUM_LETTERS_IN_BAG}";    // Update the letter count.

                    if (NUM_LETTERS_IN_BAG < 0)    // If there is not enough letter in the bag to fill in the whole rack, then display a msg.
                    {
                        MessageBox.Show("The letter bag is empty. Cannot draw any more letter. The game will finish soon in some turns!!"
                            , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    }

                    LetterBag[index] = '\0';        // The letter at the index is taken. Hence make it null.
                    break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the LoadSingleLetter() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Flying animation of a letter from source to destination.
        /// </summary>
        /// <param name="source">Airport (tile) from where the letter takes off.</param>
        /// <param name="dest">Airport (tile) where the letter lands on.</param>
        /// <param name="text">The passenger (letter) of the plane</param>
        private void FlyLetter(Point source, Point dest)
        {
            try
            {
                Point diff;
                lblFlyingLetter.Left = source.X;
                lblFlyingLetter.Top = source.Y;
                lblFlyingLetter.Visible = true;
                diff = new Point(dest.X - source.X, dest.Y - source.Y);     // Determine axis distance (destination - source).

                SteppingX = Math.Round(diff.X / MAX_ANIMATION_FRAMES, 4);   // Determine stepping needed by the letter - how many pixels to fly through x and y axes.
                SteppingY = Math.Round(diff.Y / MAX_ANIMATION_FRAMES, 4);

                // Double-precision stepping is used for smooth transitional effect and correct calculation.
                // For example, if SteppingX = -10.48 and flying letter's initial position is at 750,
                // then the first stepping takes it to 739.52 which is rounded to 740.
                // However, the double-precision variable still holds 739.52. So the next stepping will be
                // 739.52-10.48 = 729.12, which will be rounded to the pixel 729. If a flat stepping was used
                // then this would step at a constant rate, e.g.: 740, 730, 720... which might not lead to the 
                // actual cell location.
                LastXOfFlyingLabel = lblFlyingLetter.Left;
                LastYOfFlyingLabel = lblFlyingLetter.Top;

                for (int i = 0; i < MAX_ANIMATION_FRAMES; i++)
                {
                    LastXOfFlyingLabel += SteppingX;
                    LastYOfFlyingLabel += SteppingY;

                    lblFlyingLetter.Left = (int)LastXOfFlyingLabel;     // Convert the double-precision to int, as pixel needs to be int.
                    lblFlyingLetter.Top = (int)LastYOfFlyingLabel;      // Convert the double-precision to int, as pixel needs to be int.

                    Application.DoEvents();                             // Unless forced, the effect will not be visible.
                    Thread.Sleep(THREAD_SLEEP);                         // Allow delay else this will be too fast to see.
                }
                lblFlyingLetter.Visible = false;                        // Hide the flying label.
                lblFlyingLetter.Left = FlyingLetterInitialPosOnBag.X;   // Park the flying label on the bag.
                lblFlyingLetter.Top = FlyingLetterInitialPosOnBag.Y;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the FlyLetter() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Activates/deactivates players according to current turn.
        /// </summary>
        /// <param name="player"></param>
        private void ActivateDeactivatePlayers(int player)
        {
            try
            {
                Control rackCtl;
                Control scoreCtl;
                Control turnButtonCtl;
                for (int i = 0; i < Players.Count; i++)                         // Loop through all players.
                {
                    scoreCtl = Controls.Find($"lblp{i}Score", true)[0];         // E.g.: lblP0Score.
                    rackCtl = Controls.Find($"rack{i}", true)[0];               // E.g.: rack0.
                    turnButtonCtl = Controls.Find($"pbTurn{i}", true)[0];       // E.g.: pbTurn0.
                    rackCtl.Enabled = scoreCtl.Enabled = i == player;           // If current player's turn, then enable name and rack.
                    turnButtonCtl.Visible = i == player;                        // And make the green button visible.
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ActivateDeactivatePlayers() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Loads all the players - their names and mascots.
        /// </summary>
        private void LoadPlayers()
        {
            try
            {
                Control ctl;
                for (int i = 0; i < Players.Count; i++)
                {
                    ctl = Controls.Find($"rack{i}", true)[0];                   // E.g.: rack0.
                    ctl.Visible = true;

                    ctl = Controls.Find($"pbMascot{i}", true)[0];               // E.g.: pbMascot0.
                    ctl.Visible = true;
                    (ctl as PictureBox).Image = Players[i].Mascot;

                    ctl = Controls.Find($"lblP{i}Score", true)[0];              // E.g.: lblP1Score.
                    ctl.Text = $"{Players[i].Name}'s Score: 0";
                    ctl.Visible = true;
                }
                TotalPlayers = Players.Count;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the LoadPlayers() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Obtain random letters from the letter bag.
        /// </summary>
        private void LoadLetters()
        {
            try
            {
                for (int player = 0, i = 0; player < Players.Count; player++, i = 0)
                    while (i < MAX_LETTERS)
                    {
                        LoadSingleLetter(player, i);
                        if (NUM_LETTERS_IN_BAG < 0)    // If there is not enough letter in the bag to fill in the whole rack, then quit from here.
                            return;
                        i++;
                    }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the LoadLetters() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }               

        private void ScaleControls(Control.ControlCollection controls)
        {
            try
            {
                foreach (Control ctl in controls)
                {
                    Scale(ctl);
                    if (ctl is Panel || ctl is GroupBox)   // Recursive call if it is a panel or group box (which has more controls inside it).
                        ScaleControls(ctl.Controls);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ScaleControls() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }         
        }

        /// <summary>
        /// Adds the rack cell to 'Source' which is a 'RackCell' type.
        /// </summary>
        /// <param name="sender">The rack cell where a drag started.</param>
        /// <returns>The letter that was on that cell, or one space if it was a blank tile.</returns>
        private string AddSource(Label sender)
        {
            try
            {
                string letter = sender.Text;    // E.g.: 'A'.
                string name = sender.Name;      // E.g.: p2l4.

                int player = Convert.ToInt16(name.Substring(1, 1));                         // E.g.: 2 in p2l4.
                int cellInRack = Convert.ToInt16(name.Substring(name.IndexOf("l") + 1));    // E.g.: 4 in p2l4.
                Source.Add(player, cellInRack, letter);     // Add the player number, cell of gthe rack and the letter to the list.
                return letter;                              // Return the letter that was on that cell, or one space if it was a blank tile.
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the AddSource() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }

        #region Player 1 dragdrop start
        private void p0l0_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p0l1_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p0l2_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p0l3_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p0l4_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p0l5_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p0l6_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }
        #endregion

        #region Player 2 dragdrop start
        private void p1l0_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p1l1_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p1l2_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p1l3_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p1l4_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p1l5_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p1l6_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }
        #endregion

        #region Player 3 dragdrop start
        private void p2l0_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p2l1_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p2l2_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p2l3_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p2l4_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p2l5_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p2l6_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }
        #endregion

        #region Player 4 dragdrop start
        private void p3l0_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p3l1_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p3l2_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p3l3_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p3l4_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p3l5_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }

        private void p3l6_MouseDown(object sender, MouseEventArgs e)
        {
            if (ModifierKeys != Keys.Control)
            {
                string letter = AddSource(sender as Label);
                (sender as Label).DoDragDrop(letter, DragDropEffects.Copy);
            }
            else if ((sender as Label).Text.ToCharArray().GetUpperBound(0) != -1)
                AddLettersToExchangeTotheList(sender as Label);
        }
        #endregion

        /// <summary>
        /// Processes the drop actions after a letter is dropped on the board.
        /// </summary>
        /// <param name="boardCell">The cell on the board where the letter was dropped.</param>
        /// <param name="e">System drag-drop event arguments.</param>
        private void ProcessDragDrop(Label boardCell, DragEventArgs e)
        {
            try
            {
                string name = boardCell.Name;
                int x = Convert.ToInt16(name.Substring(1, name.IndexOf("_") - 1));              // E.g.: 2 in l2_14.
                int y = Convert.ToInt16(name.Substring(name.IndexOf("_") + 1));                 // E.g.: 14 in l2_l4.
                Control rackCell = Controls.Find($"p{Source.Player}l{Source.Cell}", true)[0];   // E.g.: p2l4.

                // *******************************************************************************************************
                // ****************** If the dragging tile was already dragged in the current turn. **********************
                // *******************************************************************************************************
                if (rackCell.Text.ToCharArray().GetUpperBound(0) == -1)
                {
                    MessageBox.Show("Can't drag this tile. It was already dragged before; there is nothing in it!"
                        , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }

                // *******************************************************************************************************
                // ****************** If the tile is being placed on an occupied cell on the board. **********************
                // *******************************************************************************************************
                else if (Matrix[x, y] != '\0')
                    MessageBox.Show("Occupied. Can't place here!"
                        , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                // *******************************************************************************************************
                // *************** If it is a blank tile, then the player needs to choose a letter for it. ***************
                // *******************************************************************************************************
                else
                {
                    if (Source.Letter == ' ')
                    {
                        WildCardChooser card = new WildCardChooser(this);
                        card.Top = Cursor.Position.X;   // Position the 'WildCardChooser' form on the current mouse position.
                        card.Left = Cursor.Position.Y;
                        card.ShowDialog();
                        if (WildCard == '\0')           // If nothing was chosen, then don't proceed. Drag-drop won't happen.
                        {
                            MessageBox.Show("You didn't choose any letter for the wild card. Choose a letter for the wild card or proceed with another letter in your rack!"
                                , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }
                        string premimuContent = GetPreimumCellContent(boardCell);
                        RackCellList.Push(new RackCell(Source.Player, Source.Cell, WildCard));
                        BlankTiles.Add(new RackCell(Source.Player, Source.Cell, WildCard));

                        // If it is the central star-cell, then set the font size to regular 12pt.
                        // Just to remind, the design time font size of the star cell was set to 36pt to increase the visibility of the star.
                        if (x == 7 && y == 7)
                            boardCell.Font = DEFAULT_FONT;

                        boardCell.Text = WildCard.ToString();
                        BoardBlankTiles.Add(new BlankTileOnBoard(new Point(x, y), boardCell.BackColor, GetPreimumCellContent(boardCell)));
                        boardCell.BackColor = BACK_COLOR_FOR_BLANK_TILE;
                        BoardCellList.Push(new BoardCell(x, y, premimuContent, WildCard.ToString()));
                        BlankTileRackLocations.Add(new Point(x, y));                        
                        Matrix[x, y] = WildCard;
                        WildCard = '\0';    // After putting the wildcard letter, set the global variable to NULL to be used for the second wildcard.

                        rackCell.BackColor = SystemColors.Control; // Change the colour back to system control colour.
                        ExchangingLetters.Remove(ExchangingLetters.Find(a => a.Alphabet == rackCell.Text.ToCharArray()[0])); // Remove the letter from the exchanging list.
                        rackCell.Text = string.Empty;
                    }

                    // *******************************************************************************************************
                    // ***************************** Else this is a regular drag-drop. ***************************************
                    // *******************************************************************************************************
                    else
                    {
                        string premiumContent = GetPreimumCellContent(boardCell);

                        // If it is the central star-cell, then set the font size to regular 12pt.
                        // Just to remind, the design time font size of the star cell was set to 36pt to increase the visibility of the star.
                        if (x == 7 && y == 7)
                            boardCell.Font = DEFAULT_FONT;
                        boardCell.Text = (string)e.Data.GetData(DataFormats.Text);

                        BoardCellList.Push(new BoardCell(x, y, premiumContent, boardCell.Text));
                        Matrix[x, y] = boardCell.Text.ToCharArray()[0];
                        RackCellList.Push(new RackCell(Source));

                        rackCell.BackColor = SystemColors.Control; // Change the colour back to system control colour.
                        ExchangingLetters.Remove(ExchangingLetters.Find(a => a.Alphabet == rackCell.Text.ToCharArray()[0])); // Remove the letter from the exchanging list.
                        rackCell.Text = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred in the ProcessDragDrop() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {ex.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Retrieves the premium cell content (3W, 2W, 3L, 2L) if any.
        /// If there is such content on the cell, then they are returned immediately.
        /// If there is no such content (which might be cleared off for choosing to toggle),
        /// then it returns the premimum content according to colour.
        /// </summary>
        /// <param name="boardCell"></param>
        /// <returns></returns>
        private string GetPreimumCellContent(Label boardCell)
        {
            try
            {
                if (!string.IsNullOrEmpty(boardCell.Text))
                    return boardCell.Text;  // This contains the special content (if any) of the board cell before dragging. E.g.: 2W, 3L etc.
                else
                {
                    if (boardCell.BackColor == BACK_COLOR_3W)
                        return "3W";
                    else if (boardCell.BackColor == BACK_COLOR_2W)
                        return "2W";
                    else if (boardCell.BackColor == BACK_COLOR_3L)
                        return "3L";
                    else if (boardCell.BackColor == BACK_COLOR_2L)
                        return "2L";
                }
                return "";
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the GetPreimumCellContent() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }

        /// <summary>
        /// Utilizes the Ctrl+Z to take back the letter(s) for the current player for his/her current turn.
        /// https://stackoverflow.com/questions/400113/best-way-to-implement-keyboard-shortcuts-in-a-windows-forms-application
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {

            try
            {
                if (RackCellList.Count == 0 || BoardCellList.Count == 0)                        // If there is nothing in the stacks, don't proceed.
                    return base.ProcessCmdKey(ref msg, keyData);
                if (keyData == (Keys.Control | Keys.Z))                                         // If Ctrl+Z is pressed,
                {
                    RackCell rackCell = RackCellList.Pop();                                         // then pop the last rack cell details.
                    BoardCell boardCell = BoardCellList.Pop();                                      // Also pop the last board cell details.

                    Control rackCellCtl = Controls.Find($"p{rackCell.Player}l{rackCell.Cell}", true)[0];   // E.g.: p2l4
                    Control boardCellCtl = Controls.Find($"l{boardCell.X}_{boardCell.Y}", true)[0];        // E.g.: l3_12
                    Matrix[boardCell.X, boardCell.Y] = '\0';                                            // Clear the corresponding cell in the matrix to enable next drag-drop on it.

                    if (BlankTiles.Count > 0)
                    {
                        RackCell c = BlankTiles.FirstOrDefault(a => a.Player == rackCell.Player && a.Cell == rackCell.Cell);
                        if (c != null)
                        {
                            rackCellCtl.Text = " ";                                     // Put the letter (e.g. W) back to the rack's cell.
                            BlankTiles.Remove(c);
                            rackCellCtl.BackColor = BACK_COLOR_FOR_BLANK_TILE;
                        }
                    }
                    else rackCellCtl.Text = rackCell.Letter.ToString();                                     // Put the letter (e.g. W) back to the rack's cell.
                    
                    // If it is the central star-cell, then set the font size to regular 12pt.
                    // Just to remind, the design time font size of the star cell was set to 36pt to increase the visibility of the star.
                    if (boardCell.X == 7 && boardCell.Y == 7)
                        boardCellCtl.Font = STAR_CELL_FONT;

                    BlankTileOnBoard blankTile = BoardBlankTiles.FirstOrDefault(a => a.Cell.X == boardCell.X && a.Cell.Y == boardCell.Y);
                    if (blankTile != null)
                    {
                        boardCellCtl.BackColor = blankTile.Colour;
                        BoardBlankTiles.Remove(blankTile);
                    }

                    boardCellCtl.Text = boardCell.PremiumContent;                                  // Put special contents (if any) (e.g. 2L) back to the board's cell.
                    return true;
                }
                return base.ProcessCmdKey(ref msg, keyData);
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ProcessCmdKey() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }
        
        private void btnDone_Click(object sender, EventArgs e)
        {
            try
            {
                GameEngine engine = GameEngine.GetInstance();

                // If the player puts letters in different rows or columns, then that is invalid placement.
                if (!engine.ValidOrientation(BoardCellList, Matrix))
                    return;

                // If the letters are not adjacent to each other, or with existing letters on the board, then that is invalid placement.
                if (!engine.ValidAdjacency(BoardCellList, Matrix))
                    return;

                // StarCheckNeeded - for the first word that should pass through the central star tile.
                if (StarCheckNeeded && !engine.FirstWordThroughCentralStarTile(BoardCellList))
                    return;

                if (!StarCheckNeeded && !engine.CurrentWordCrossedThroughExistingWord(BoardCellList.Select(a => a).ToList(), Matrix, CurrentPlayer, CurrentTurn))
                    return;

                if (engine.ValidateWord(BoardCellList, Matrix, CurrentPlayer, CurrentTurn, StarCheckNeeded))
                {
                    lblFailCount.Visible = false;
                    Failcount = 0;                                  // Reset fail count.

                    Control ctl = Controls.Find($"lblP{CurrentPlayer}Score", true)[0];   // E.g.: lblP1Score.
                    ctl.Text = $"{Players[CurrentPlayer].Name}'s Score: {Players[CurrentPlayer].TotalScore}";

                    btnExchange.Enabled = btnPass.Enabled = btnDone.Enabled = false;
                    timerBagShaking.Start();
                    pbLetterBag.Top = LetterBagInitialPos.Y;        // Move the letter bag to its initial position.
                    pbLetterBag.Left = LetterBagInitialPos.X;

                    SetTileBackColor(BoardCellList);
                    CurrentWordDirection = WordDirectionEnum.None;
                    BoardCellList.Clear();                          // Clear board cell list.

                    ShrinkLetterBag();                              // Shrink letter bag.
                    if (LetterBag.GetUpperBound(0) == -1)
                    {
                        if (GameEndWUsingAllLettersAndNoMoreLettersInBag())
                            return;
                        else EndGame();
                    }
                    FillInBlankRackCells();                         // Fill in blank cells on the rack with new random letters.                    

                    ClearExchangingTilesAndList();                  // Clear the tiles that the current player selected for exchange.
                    if (++CurrentPlayer < TotalPlayers)             // See if the next player is not the highest player.
                        ActivateDeactivatePlayers(CurrentPlayer);   // If not, activate for his/her turn.
                    else
                    {
                        CurrentPlayer = 0;                          // Else current player count again starts from the beginning.
                        ActivateDeactivatePlayers(CurrentPlayer);   // Hence activate for his/her turn.
                    }
                    RackCellList.Clear();                           // Clear rack cell list.
                    StarCheckNeeded = false;                        // First word passed through central star tile; no more check needed.
                    btnExchange.Enabled = btnPass.Enabled = btnDone.Enabled = true;    // Enable buttons for the next player.
                    BlankTiles.Clear();
                }
                else
                {
                    ClearExchangingTilesAndList();                  // Clear the tiles that the current player selected for exchange.
                    btnExchange.Enabled = btnPass.Enabled = btnDone.Enabled = false;
                    Forfeit();                                      // Invalid word, hence this is a forfeit.
                    btnExchange.Enabled = btnPass.Enabled = btnDone.Enabled = true;

                    lblFailCount.Visible = true;
                    lblFailCount.Text = $"Fail count: {++Failcount}";
                    if (Failcount == MAX_FAIL_COUNT)
                        EndGame();
                }
                BoardBlankTiles.Clear();
                CurrentWordDirection = WordDirectionEnum.None;
                CurrentTurn++;                                      // Increase turn count by 1.
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred in the btnDone_Click() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {ex.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Checks if the game ends. The game ends if there is no more letter in one player's rack,
        /// and also there is no more letter in the bag to fill his/her rack.
        /// That no more letter is in the bag to fill the rack would have been checked already by the previous call to 'FillInBlankRackCells'
        /// in the calling method (btnDone_Click).
        /// </summary>
        /// <returns>True if the rack of the current player is empty.</returns>
        private bool GameEndWUsingAllLettersAndNoMoreLettersInBag()
        {
            try
            {
                Control ctl;
                for (int j = 0; j < MAX_LETTERS; j++)
                {
                    ctl = Controls.Find($"p{CurrentPlayer}l{j}", true)[0];      // E.g.: p2l0.
                    if (!string.IsNullOrEmpty(ctl.Text))
                        return false;
                }
                EndGameWhenAPlayerIsAwardedOthersScores();
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the GameEndWUsingAllLettersAndNoMoreLettersInBag() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// If there is no more letter in the current player's rack, and there is no more letter to draw in the rack,
        /// then other player's letter values are deducted from theirs and added to the current player's score.
        /// </summary>
        private void EndGameWhenAPlayerIsAwardedOthersScores()
        {
            try
            {
                Control ctl;
                StringBuilder str = new StringBuilder();
                IndividualScore score;
                Control scoreCtl;

                int scoreToDeduct, totalScoresToAwardToCurrentPlayer = 0;
                for (int i = 0; i < Players.Count; i++)
                {
                    if (i == CurrentPlayer) continue;                               // Don't penalize the current player.                
                    scoreToDeduct = 0;
                    str.Append($"Player{i + 1}'s ({Players[i].Name}'s) Score reduction details:{Environment.NewLine}");
                    for (int j = 0; j < MAX_LETTERS; j++)
                    {
                        ctl = Controls.Find($"p{i}l{j}", true)[0];                  // E.g.: p2l0.
                        if (!string.IsNullOrEmpty(ctl.Text))
                        {
                            score = new IndividualScore(ctl.Text.ToCharArray()[0], "", Point.Empty);
                            str.Append($"Score to reduce for remaining letter {score.Letter}: {score.Score}{Environment.NewLine}");
                            scoreToDeduct += score.Score;
                        }
                        Players[i].TotalScore -= scoreToDeduct;                     // Deduct the total from the player's total score.                    
                    }
                    scoreCtl = Controls.Find($"lblP{i}Score", true)[0];             // E.g.: lblP1Score.
                    scoreCtl.Text = $"{Players[i].Name}'sScore: {Players[i].TotalScore}";

                    str.Append($"Total score reduction for Player{i + 1} ({Players[i].Name}'s) is: {scoreToDeduct}{Environment.NewLine}{Environment.NewLine}");
                    Players[i].ScoreDetails.Add(new TurnsWithScores(CurrentTurn, $"Game ended by finishing all letters in bag.{Environment.NewLine}{str.ToString()}", null));
                    totalScoresToAwardToCurrentPlayer += scoreToDeduct;
                }
                str.Append($"Total bonus awarded to {Players[CurrentPlayer].Name}) is: {totalScoresToAwardToCurrentPlayer}");
                Players[CurrentPlayer].TotalScore += totalScoresToAwardToCurrentPlayer;
                Players[CurrentPlayer].ScoreDetails.Add(new TurnsWithScores(CurrentTurn, $"Game ended by finishing all letters in bag.{Environment.NewLine}{str.ToString()}", null));

                scoreCtl = Controls.Find($"lblP{CurrentPlayer}Score", true)[0];     // E.g.: lblP1Score.
                scoreCtl.Text = $"{Players[CurrentPlayer].Name}'s Score: {Players[CurrentPlayer].TotalScore}";

                MessageBox.Show(str.ToString(), Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the EndGameWhenAPlayerIsAwardedOthersScores() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EndGame()
        {
            try
            {
                Control ctl;
                StringBuilder str = new StringBuilder(), strIndividual = new StringBuilder();
                IndividualScore score;
                int scoreToDeduct;
                Control scoreCtl;

                for (int i = 0; i < Players.Count; i++)
                {
                    scoreToDeduct = 0;
                    strIndividual.Append($"Player{i + 1}'s ({Players[i].Name}'s) Score reduction details:{Environment.NewLine}");
                    for (int j = 0; j < MAX_LETTERS; j++)
                    {
                        ctl = Controls.Find($"p{i}l{j}", true)[0];      // E.g.: p2l0.
                        if (!string.IsNullOrEmpty(ctl.Text))
                        {
                            score = new IndividualScore(ctl.Text.ToCharArray()[0], "", Point.Empty);
                            strIndividual.Append($"Score to reduce for remaining letter {score.Letter}: {score.Score}{Environment.NewLine}");
                            scoreToDeduct += score.Score;
                        }
                    }
                    strIndividual.Append($"Total score reduction for Player{i + 1} ({Players[i].Name}'s) is: {scoreToDeduct}{Environment.NewLine}{Environment.NewLine}");
                    Players[i].TotalScore -= scoreToDeduct;         // Deduct the total from the player's total score.
                    Players[i].ScoreDetails.Add(new TurnsWithScores(CurrentTurn, $"Game ended by reaching maximum fails {MAX_FAIL_COUNT}.{Environment.NewLine}{str.ToString()}", null));

                    scoreCtl = Controls.Find($"lblP{i}Score", true)[0];     // E.g.: lblP1Score.
                    scoreCtl.Text = $"{Players[i].Name}'s Score: {Players[i].TotalScore}";
                    str.Append(strIndividual);
                    strIndividual.Clear();
                }
                MessageBox.Show(str.ToString(), Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                DeclareWinner();
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the EndGame() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Decides the winner by finding the highest scorer.
        /// </summary>
        private void DeclareWinner()
        {
            try
            {
                int highestScore = -99999;                                      // Start with a low score.
                int highestScorer = 0;
                Control ctl;

                for (int i = 0; i < Players.Count; i++)                         // Loop through all players.
                {
                    if (Players[i].TotalScore > highestScore)                   // If the current player has higher score than the record,
                    {
                        highestScore = Players[i].TotalScore;                   // then keep track of the new high score,
                        highestScorer = i;                                      // and the player number.
                    }
                    ctl = Controls.Find($"rack{highestScorer}", true)[0];       // At the same time, hide the rack of the player.
                    ctl.Visible = false;
                }

                // Declare the winner with the score.
                MessageBox.Show($"Player{highestScorer + 1} ({Players[highestScorer].Name}) wins with a score of {Players[highestScorer].TotalScore}"
                    , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);

                ctl = Controls.Find($"pbMascot{highestScorer}", true)[0];       // Find the mascot of the winning player.
                GameWinner = highestScorer;
                pbFireCrackers.Left = ctl.Left;                                 // Move the firecrackers gif image to the mascot.
                pbFireCrackers.Top = ctl.Top;
                pbFireCrackers.Visible = true;                                  // Display the firecrackers.
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the DeclareWinner() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clears any remaining exchanging letters and sets the tile colour back to normal.
        /// This is needed when the player selected some letters for exchanging (letters added to list and cell colour becomes Crimson),
        /// but didn't exchange and the turn moves on. Before the turn moves to the next player, these need to be cleared.
        /// </summary>
        private void ClearExchangingTilesAndList()
        {
            try
            {
                Control ctl;
                foreach (Letter l in ExchangingLetters)
                {
                    ctl = Controls.Find($"p{CurrentPlayer}l{l.Pos}", true)[0];      // E.g.: p2l0.
                    ctl.BackColor = SystemColors.Control;                           // Change the rack cell back colour to normal.
                    ExchangingLetters.Remove(l);                                    // Remove the letter from the list.
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ClearExchangingTilesAndList() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Forfeit()
        {
            try
            {
                ShrinkLetterBag();
                if (LetterBag.GetUpperBound(0) == -1)
                    EndGame();
                RevertCells();
                CurrentPlayer++;
                if (CurrentPlayer < TotalPlayers)
                    ActivateDeactivatePlayers(CurrentPlayer);
                else
                {
                    CurrentPlayer = 0;
                    ActivateDeactivatePlayers(CurrentPlayer);
                }
                BoardCellList.Clear();
                RackCellList.Clear();
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the Forfeit() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Resizes the bag after a turn is complete.
        /// Logic:
        /// 1) Start traversing from beginning. Once a NULL is detected at 'pos' then
        /// 2) Pick the last non-NULL letter from the bag and place it in 'pos'. Nullify the 'lastPickPos'.
        /// </summary>
        private void ShrinkLetterBag()
        {
            try
            {
                int len = LetterBag.GetUpperBound(0);                       // Initial length of the bag.
                int lastPickPos = len;                                      // Last index is the length of the array.
                for (int pos = 0; pos < lastPickPos; pos++)                 // Start traversing from the beginning.
                {
                    if (LetterBag[pos] == '\0')                             // If a NULL is detected
                    {
                        while (lastPickPos > pos && LetterBag[lastPickPos] == '\0')
                            lastPickPos--;
                        LetterBag[pos] = LetterBag[lastPickPos];            // Copy that last character to the beginning NULL index.
                        LetterBag[lastPickPos] = '\0';                      // Nullify the last pick index.
                    }
                }
                NUM_LETTERS_IN_BAG = lastPickPos;
                Array.Resize(ref LetterBag, NUM_LETTERS_IN_BAG);
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ShrinkLetterBag() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FillInBlankRackCells()
        {
            try
            {
                ShakeBag();
                DelayAndProcessWorks(200);
                foreach (RackCell cell in RackCellList)
                    LoadSingleLetter(cell.Player, cell.Cell);
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the FillInBlankRackCells() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void SetTileBackColor(Stack<BoardCell> boardCellList)
        {
            try
            {
                Control cell = null;
                foreach (BoardCell c in boardCellList)
                {
                    cell = Controls.Find($"l{c.X}_{c.Y}", true)[0];         // E.g.: l3_12
                    if (cell.BackColor != BACK_COLOR_FOR_BLANK_TILE)        // If it is a blank tile, then leave the colour to orchid.
                        cell.BackColor = BACK_COLOR_FOR_VALID_WORDS;        // Set the backcolor of the special tile to regular control color; this is no longer a special tile.                
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the SetTileBackColor() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Returns the letters from board to rack in case of invalid coining of words.
        /// In case any wild card was placed, that is also returned as a wild card.
        /// </summary>
        private void RevertCells()
        {
            try
            {
                Control rackCellCtl = null, boardCellCtl = null;
                Point rackCellAbsLoc;
                Point boardCellAbsLoc;
                RackCell rackCell;
                BoardCell boardCell;

                while (RackCellList.Count > 0)
                {
                    rackCell = RackCellList.Pop();
                    boardCell = BoardCellList.Pop();

                    rackCellCtl = Controls.Find($"p{rackCell.Player}l{rackCell.Cell}", true)[0]; // E.g.: p2l4
                    rackCellAbsLoc = rackCellCtl.FindForm().PointToClient(rackCellCtl.Parent.PointToScreen(rackCellCtl.Location));

                    boardCellCtl = Controls.Find($"l{boardCell.X}_{boardCell.Y}", true)[0];      // E.g.: l4_12
                    boardCellAbsLoc = boardCellCtl.FindForm().PointToClient(boardCellCtl.Parent.PointToScreen(boardCellCtl.Location));
                                        
                    if (BlankTiles.Count > 0)
                    {
                        RackCell c = BlankTiles.FirstOrDefault(a => a.Player == rackCell.Player && a.Cell == rackCell.Cell);
                        if (c != null)
                        {
                            // Remove the entry from the global 'BlankTilesLocation' if the location matches.
                            BlankTileRackLocations.Remove(BlankTileRackLocations.FirstOrDefault(a => a.X == boardCell.X && a.Y == boardCell.Y));

                            lblFlyingLetter.Text = " ";
                            BlankTiles.Remove(c);
                            rackCellCtl.BackColor = BACK_COLOR_FOR_BLANK_TILE;
                        }
                    }
                    else lblFlyingLetter.Text = boardCellCtl.Text;

                    // If it is the central star-cell, then set the font size to regular 12pt.
                    // Just to remind, the design time font size of the star cell was set to 36pt to increase the visibility of the star.
                    if (boardCell.X == 7 && boardCell.Y == 7)
                        boardCellCtl.Font = STAR_CELL_FONT;

                    if (PremimumIdentifierToggle)
                        boardCellCtl.Text = boardCell.PremiumContent.ToString();            // Put back any special content like 2L, 3W etc.
                    else boardCellCtl.Text = "";                                            // Premium colour only.

                    BlankTileOnBoard blankTile = BoardBlankTiles.FirstOrDefault(a => a.Cell.X == boardCell.X && a.Cell.Y == boardCell.Y);
                    if (blankTile != null)
                    {
                        boardCellCtl.BackColor = blankTile.Colour;
                        BoardBlankTiles.Remove(blankTile);
                    }

                    FlyLetter(boardCellAbsLoc, rackCellAbsLoc);
                    rackCellCtl.Text = lblFlyingLetter.Text;
                    Matrix[boardCell.X, boardCell.Y] = '\0';                                // Clear the corresponding cell in the matrix to enable next drag-drop on it.
                }
                BlankTiles.Clear(); // This might not be needed as we are using Remove().
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the RevertCells() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region First column dragdrops
        private void l0_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l0_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l0_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Second column dragdrops
        private void l1_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l1_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l1_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Third column dragdrops
        private void l2_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l2_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l2_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Fourth column dragdrops
        private void l3_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l3_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l3_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }
        
        private void l3_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Fifth column dragdrops
        private void l4_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l4_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l4_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Sixth column dragdrops
        private void l5_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l5_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l5_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Seventh column dragdrops
        private void l6_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l6_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l6_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Eighth column dragdrops
        private void l7_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l7_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l7_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Ninth column dragdrops
        private void l8_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l8_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l8_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Tenth column dragdrops
        private void l9_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l9_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l9_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Eleventh column dragdrops
        private void l10_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l10_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l10_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Twelfth column dragdrops
        private void l11_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l11_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l11_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Thirteenth column dragdrops
        private void l12_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l12_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l12_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Fourteenth column dragdrops
        private void l13_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l13_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l13_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        #region Fifteenth column dragdrops
        private void l14_0_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_0_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_1_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_2_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_3_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_4_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_5_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_6_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_6_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_7_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_7_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_8_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_8_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_9_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_9_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_10_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_10_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_11_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_11_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_12_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_12_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_13_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_13_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void l14_14_DragDrop(object sender, DragEventArgs e)
        {
            ProcessDragDrop(sender as Label, e);
        }

        private void l14_14_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        /// <summary>
        /// Adds letter to exchange to the list of letters.
        /// Mouse-clicking on the letter while holding CTRL key triggers this method.
        /// If the label's back colour is already crimson, then it removes the letter from the list.
        /// Else it adds the letter to the list.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="color"></param>
        private void AddLettersToExchangeTotheList(Label label)
        {
            try
            {
                // If the label's back colour is already crimson, then it removes the letter from the list.
                if (label.BackColor == BACK_COLOR_3W)
                {
                    label.BackColor = SystemColors.Control; // Change the colour back to system control colour.
                    ExchangingLetters.Remove(ExchangingLetters.Find(x => x.Alphabet == label.Text.ToCharArray()[0])); // Remove the letter from the exchanging list.
                }
                else    // Else it adds the letter to the list.
                {
                    int pos = Convert.ToInt16(label.Name.Substring(3));     // Keep the position of the letter in the rack.
                    ExchangingLetters.Add(new Letter(label.Text.ToCharArray()[0], pos));
                    label.BackColor = BACK_COLOR_3W;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the AddLettersToExchangeTotheList() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult res = MessageBox.Show("Are you sure you want to exit?", Properties.Resources.APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
                Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        /// <summary>
        /// Exchanges the letters that are highlighted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnExchange_Click(object sender, EventArgs e)
        {
            try
            {
                Control ctl;
                Point cellAbsLoc;

                if (ExchangingLetters.Count == 0)                   // No letters selected for exchange.
                {
                    MessageBox.Show("Select letters to exchange. Click on the letter to exchange while holding down CTRL-key."
                        , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (LetterBag.GetUpperBound(0) < MAX_LETTERS - 1)   // Mimimum 7 letters must stay in the bag for an exchange to happen.
                {
                    MessageBox.Show($"The bag contains less than {MAX_LETTERS} letters. Exchange cannot occur."
                        , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ***********************************************************************************************
                // **** First check if the player already dragged letters on the board. **************************
                // **** Dragging and exchanging are mutually exclusive. ******************************************
                // ***********************************************************************************************
                if (RackCellList.Count > 0 || BoardCellList.Count > 0)
                {
                    MessageBox.Show("You already dragged letters on the board. Can't exchange now. First return the letters (CTRL+Z)."
                        , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ***********************************************************************************************
                // ********************** Secondly, return the letters from rack to bag. *************************
                // ***********************************************************************************************
                foreach (Letter l in ExchangingLetters)
                {
                    ctl = Controls.Find($"p{CurrentPlayer}l{l.Pos}", true)[0];      // E.g.: p2l0.

                    // Obtain absolute location of the rack's cell from the top left of the window.
                    cellAbsLoc = ctl.FindForm().PointToClient(ctl.Parent.PointToScreen(ctl.Location));

                    lblFlyingLetter.Text = ctl.ToString();
                    ctl.BackColor = SystemColors.Control;                           // Change the rack cell back colour to normal.
                    ctl.Text = string.Empty;
                    FlyLetter(cellAbsLoc, FlyingLetterInitialPosOnBag);

                    LetterBag[l.Pos] = l.Alphabet;
                    lblLettersRemaining.Text = $"Letters remaining: {++NUM_LETTERS_IN_BAG}";
                    Application.DoEvents();
                }

                // ***********************************************************************************************
                // ********************** Thirdly, load new letters from bag to rack. ****************************
                // ***********************************************************************************************
                foreach (Letter l in ExchangingLetters)
                    LoadSingleLetter(CurrentPlayer, l.Pos);
                ExchangingLetters.Clear();                                          // Clear the exchanging letters list.

                lblFailCount.Visible = true;
                lblFailCount.Text = $"Fail count: {++Failcount}";
                if (Failcount == MAX_FAIL_COUNT)
                    EndGame();

                // ***********************************************************************************************
                // ********************** Fourth, move the turn to the next player. ******************************
                // ***********************************************************************************************
                Players[CurrentPlayer].ScoreDetails.Add(new TurnsWithScores(CurrentTurn, $"Exchanged letters.{Environment.NewLine}{Environment.NewLine}", null));
                if (++CurrentPlayer < TotalPlayers)             // See if the next player is not the highest player.
                    ActivateDeactivatePlayers(CurrentPlayer);   // If not, activate for his/her turn.
                else
                {
                    CurrentPlayer = 0;                          // Else current player count again starts from the beginning.
                    ActivateDeactivatePlayers(CurrentPlayer);   // Hence activate for his/her turn.
                }
                RackCellList.Clear();                           // Clear rack cell list.
                btnExchange.Enabled = btnPass.Enabled = btnDone.Enabled = true;    // Enable buttons for the next player.
                BlankTiles.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred in the btnExchange_Click() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {ex.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void timerBagShaking_Tick(object sender, EventArgs e)
        {
            try
            {
                if (++SHAKE_COUNT >= MAX_SHAKE_PIXELS)
                {
                    SHAKE_PIXELS = MAX_SHAKE_PIXELS;            // Reset the Shake pixels.
                    pbLetterBag.Top = LetterBagInitialPos.Y;
                    pbLetterBag.Left = LetterBagInitialPos.X;
                    timerBagShaking.Stop();
                }
                else
                {
                    pbLetterBag.Top += SHAKE_PIXELS;
                    SHAKE_PIXELS = -SHAKE_PIXELS;
                    Thread.Sleep(THREAD_SLEEP);
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred in the timerBagShaking_Tick() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {ex.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Returns the letters to rack if there is any on the board (that didn't coin a valid word).
        /// Then moves the turn to the next player.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnPass_Click(object sender, EventArgs e)
        {
            try
            {
                btnExchange.Enabled = btnPass.Enabled = btnDone.Enabled = false;
                Players[CurrentPlayer].ScoreDetails.Add(new TurnsWithScores(CurrentTurn, $"Passed (forfeited current turn).", null));
                Forfeit();

                lblFailCount.Visible = true;
                lblFailCount.Text = $"Fail count: {++Failcount}";
                if (Failcount == MAX_FAIL_COUNT)
                    EndGame();
                else btnExchange.Enabled = btnPass.Enabled = btnDone.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred in the btnPass_Click() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {ex.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Click Events for Score Details.
        private void pbMascot0_Click(object sender, EventArgs e)
        {
            DisplayScoreDetails d = new DisplayScoreDetails();
            d.AddPlayer(Players[0]);
            d.ShowDialog();
        }

        private void pbMascot1_Click(object sender, EventArgs e)
        {
            DisplayScoreDetails d = new DisplayScoreDetails();
            d.AddPlayer(Players[1]);
            d.ShowDialog();
        }

        private void pbMascot2_Click(object sender, EventArgs e)
        {
            DisplayScoreDetails d = new DisplayScoreDetails();
            d.AddPlayer(Players[2]);
            d.ShowDialog();
        }

        private void pbMascot3_Click(object sender, EventArgs e)
        {
            DisplayScoreDetails d = new DisplayScoreDetails();
            d.AddPlayer(Players[3]);
            d.ShowDialog();
        }

        private void pbTurn0_Click(object sender, EventArgs e)
        {
            pbMascot0_Click(sender, e);
        }

        private void pbTurn1_Click(object sender, EventArgs e)
        {
            pbMascot1_Click(sender, e);
        }

        private void pbTurn2_Click(object sender, EventArgs e)
        {
            pbMascot2_Click(sender, e);
        }

        private void pbTurn3_Click(object sender, EventArgs e)
        {
            pbMascot3_Click(sender, e);
        }

        private void pbFireCrackers_Click(object sender, EventArgs e)
        {
            Control ctl = Controls.Find($"pbMascot{GameWinner}", true)[0];  // Find the mascot of the winning player.
            pbFireCrackers.Left = ctl.Left;                                 // Move the firecrackers gif image to the mascot.
            pbFireCrackers.Top = ctl.Top;
            pbFireCrackers.Visible = true;                                  // Display the firecrackers.
            MethodInfo scoreMethod = GetType().GetMethod($"pbMascot{GameWinner}_Click");    // Call the corresponding click method by name.
            scoreMethod.Invoke(this, new object[] { BindingFlags.CreateInstance });
        } 
        #endregion

        private void timerFlyLetter_Tick(object sender, EventArgs e)
        {
            try
            {
                if (++FRAME_COUNT >= MAX_ANIMATION_FRAMES)
                    timerFlyLetter.Stop();
                else
                {
                    LastXOfFlyingLabel = lblFlyingLetter.Left;
                    LastYOfFlyingLabel = lblFlyingLetter.Top;

                    LastXOfFlyingLabel += SteppingX;
                    LastYOfFlyingLabel += SteppingY;

                    lblFlyingLetter.Left = (int)LastXOfFlyingLabel; // Convert the double-precision to int, as pixel needs to be int.
                    lblFlyingLetter.Top = (int)LastYOfFlyingLabel;  // Convert the double-precision to int, as pixel needs to be int.

                    Application.DoEvents();                         // Unless forced, the effect will not be visible.
                    Thread.Sleep(THREAD_SLEEP);                     // Allow delay else this will be too fast to see.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred in the timerFlyLetter_Tick() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {ex.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Toggles on and off the premium cell texts (3W, 2W, 3L, 2L).
        /// The central star tile is left untouched.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void togglePremiumTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                PremimumIdentifierToggle = grpPremiumLegend.Visible;
                grpPremiumLegend.Visible = !grpPremiumLegend.Visible;
                if (grpPremiumLegend.Visible)
                {
                    foreach (Label l in panelLetters.Controls)
                        if (l.Text == "3W" || l.Text == "2W" || l.Text == "3L" || l.Text == "2L")
                            l.Text = "";
                }
                else
                {
                    foreach (Label l in panelLetters.Controls)
                        if (l.BackColor == BACK_COLOR_3W)
                            l.Text = "3W";
                        else if (l.BackColor == BACK_COLOR_2W)
                            l.Text = "2W";
                        else if (l.BackColor == BACK_COLOR_3L)
                            l.Text = "3L";
                        else if (l.BackColor == BACK_COLOR_2L)
                            l.Text = "2L";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred in the togglePremiumTextToolStripMenuItem_Click() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {ex.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toggleLettersLegendToolStripMenuItem_Click(object sender, EventArgs e)
        {
            grpLetterScoreLegend.Visible = !grpLetterScoreLegend.Visible;
        }

        private void Wordbler_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult res = MessageBox.Show("Are you sure you want to exit?", Properties.Resources.APP_TITLE, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
                Environment.Exit(1);
            else e.Cancel = true;
        }

        private void Scale(Control ctl)
        {
            try
            {
                ctl.Font = new Font(FontFamily.GenericSansSerif, Scaler.GetMetrics((int)ctl.Font.Size), FontStyle.Regular);
                ctl.Width = Scaler.GetMetrics(ctl.Width, "Width");
                ctl.Height = Scaler.GetMetrics(ctl.Height, "Height");
                ctl.Top = Scaler.GetMetrics(ctl.Top, "Top");
                ctl.Left = Scaler.GetMetrics(ctl.Left, "Left");
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the Scale() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ScaleFont()
        {
            try
            {
                STAR_CELL_FONT = new Font(FontFamily.GenericSansSerif, Scaler.GetMetrics((int)STAR_CELL_FONT.Size), FontStyle.Regular);
                DEFAULT_FONT = new Font(FontFamily.GenericSansSerif, Scaler.GetMetrics((int)DEFAULT_FONT.Size), FontStyle.Regular);
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the Scale() method of the 'Wordbler' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}