using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using static Wordbler.Classes.Globals;

namespace Wordbler.Classes
{
    class GameEngine
    {
        private const string WORD_FILE_NAME = @"..\..\Words\Words.json";
        static GameEngine Instance = new GameEngine();
        private static List<JToken> Words;
        public static GameEngine GetInstance() { return Instance; }
        private GameEngine()   // Singleton - private constructor, denies instantiation.
        {
            try
            {
                string jsonWords;
                using (StreamReader reader = new StreamReader(WORD_FILE_NAME))
                    jsonWords = reader.ReadToEnd();
                JArray json = (JArray)JsonConvert.DeserializeObject(jsonWords);
                Words = json.ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the GameEngine() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Loads the words from the dictionary and saves in a static variable.
        /// </summary>
        private void InitializeDictionary()
        {
            try
            {
                string jsonWords;
                using (StreamReader reader = new StreamReader(WORD_FILE_NAME))
                    jsonWords = reader.ReadToEnd();
                JArray jArray = (JArray)JsonConvert.DeserializeObject(jsonWords);
                Words = jArray.ToList();
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the InitializeDictionary() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Checks if the player placed the letters in a valid orientation - either horizontal or vertical.
        /// </summary>
        /// <param name="boardCellList">The list of board cells where the plyaer dropped the letters.</param>
        /// <returns>True if valid orientation, false otherwise.</returns>
        public bool ValidOrientation(Stack<BoardCell> boardCellList, char[,] matrix)
        {
            try
            {
                List<BoardCell> boardCells = boardCellList.Select(x => x).ToList();
                if (boardCellList.Count == 0)
                {
                    MessageBox.Show("No letter was placed on the board."
                        , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                else if (boardCellList.Count == 1)
                {
                    InferDirectionForSingleLetterPlacement(matrix, boardCells);
                    //if (CurrentWordDirection == WordDirectionEnum.None)
                    //    MessageBox.Show("Word direction could not be determined from the single letter.");
                    //return CurrentWordDirection != WordDirectionEnum.None;  // If direction could not be inferred, then return false.
                    return true;
                }
                else
                {
                    // If this is a vertical drop to the previous drop, then mark the direction as vertical.
                    if (boardCells[0].X == boardCells[1].X && Math.Abs(boardCells[0].Y - boardCells[1].Y) >= 1)
                        CurrentWordDirection = WordDirectionEnum.Vertical;

                    // If this is a horizontal drop to the previous drop, then mark the direction as horizontal.
                    else if (boardCells[0].Y == boardCells[1].Y && Math.Abs(boardCells[0].X - boardCells[1].X) >= 1)
                        CurrentWordDirection = WordDirectionEnum.Horizontal;

                    // Else, this must be an abnormal drop - doesn't maintain any legitimate orientation.
                    else
                    {
                        MessageBox.Show("Abnormal orientation detected. Letters must be placed horizontally or vertically. Cannot proceed!"
                            , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        CurrentWordDirection = WordDirectionEnum.None;
                        return false;
                    }

                    WordDirectionEnum dir = WordDirectionEnum.None;
                    for (int i = 1; i < boardCells.Count - 1; i++)
                    {
                        // Check orientation of each subsequent drops.
                        if (boardCells[i].X == boardCells[i + 1].X && Math.Abs(boardCells[i].Y - boardCells[i + 1].Y) >= 1)
                            dir = WordDirectionEnum.Vertical;
                        else if (boardCells[i].Y == boardCells[i + 1].Y && Math.Abs(boardCells[i].X - boardCells[i + 1].X) >= 1)
                            dir = WordDirectionEnum.Horizontal;

                        // If this orientation is not the same as the first, then this must be an abnormal orientation.
                        if (dir != CurrentWordDirection)
                        {
                            MessageBox.Show("Abnormal orientation detected. Letters must be placed horizontally or vertically. Cannot proceed!"
                                , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                            CurrentWordDirection = WordDirectionEnum.None;
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ValidOrientation() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Infers the intended orientation for a single letter by checking where the neighbourhood letters reside.
        /// Records the inferred direction in the global variable 'CurrentWordDirection'.
        /// </summary>
        /// <param name="matrix">The character matrix</param>
        /// <param name="boardCells"></param>
        private static void InferDirectionForSingleLetterPlacement(char[,] matrix, List<BoardCell> boardCells)
        {
            try
            {
                int x = boardCells[0].X;
                int y = boardCells[0].Y;
                int yBelow = y + 1;
                int yAbove = y - 1;
                int xLeft = x - 1;
                int xRight = x + 1;

                // If there is a letter above or below the singly-placed letter, then the inferred direction is vertical.
                if ((yAbove >= 0 && matrix[x, yAbove] != '\0') || (yBelow < GRID_SIZE && matrix[x, yBelow] != '\0'))
                    CurrentWordDirection = WordDirectionEnum.Vertical;

                // If there is a letter to the left or right of the singly-placed letter, then the inferred direction is horizontal.
                else if ((xRight < GRID_SIZE && matrix[xRight, y] != '\0') || (xLeft >= 0 && matrix[xLeft, y] != '\0'))
                    CurrentWordDirection = WordDirectionEnum.Horizontal;

                // Else set the direction to none.
                else CurrentWordDirection = WordDirectionEnum.None;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the InferDirectionForSingleLetterPlacement() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Checks if the letters were palced adjacent to each other or adjacent to other letters that are already on the board.
        /// </summary>
        /// <param name="boardCellList"></param>
        /// <param name="matrix"></param>
        /// <returns>True/False according to the legitimacy of the adjacency.</returns>
        internal bool ValidAdjacency(Stack<BoardCell> boardCellList, char[,] matrix)
        {
            try
            {
                if (CurrentWordDirection == WordDirectionEnum.Vertical)
                {
                    List<BoardCell> boardCells = boardCellList.Select(a => a).OrderBy(b => b.Y).ToList();   // Clone the list sorted from left to right.
                    for (int i = 0; i < boardCells.Count - 1; i++)
                    {
                        // If the distance between the consecutive letters are more than a cell, that would mean
                        // the cells in between must contain existing letters. Else this would be deemed invalid placement.
                        if (boardCells[i + 1].Y - boardCells[i].Y > 1)
                            // Start traversing from the bottom cell of the top cell, walk downwards the top cell of the bottom cell.
                            for (int j = boardCells[i].Y + 1; j < boardCells[i + 1].Y; j++)
                                // If that cell is empty, means this is an invalid placement.
                                if (matrix[boardCells[i].X, j] == '\0')
                                {
                                    MessageBox.Show("Letters don't have a valid vertical adjacency. Cannot proceed!"
                                        , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return false;
                                }
                    }
                }
                else if (CurrentWordDirection == WordDirectionEnum.Horizontal)
                {
                    List<BoardCell> boardCells = boardCellList.Select(a => a).OrderBy(b => b.X).ToList();   // Clone the list sorted from top to bottom.
                    for (int i = 0; i < boardCells.Count - 1; i++)
                    {
                        // If the distance between the consecutive letters are more than a cell, that would mean
                        // the cells in between must contain existing letters. Else this would be deemed invalid placement.
                        if (boardCells[i + 1].X - boardCells[i].X > 1)
                            // Start traversing from the next cell of the left cell, walk towards the left cell of the right cell.
                            for (int j = boardCells[i].X + 1; j < boardCells[i + 1].X; j++)
                                // If that cell is empty, means this is an invalid placement.
                                if (matrix[j, boardCells[i].Y] == '\0')
                                {
                                    MessageBox.Show("Letters don't have valid horizontal adjacency. Cannot proceed!"
                                        , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return false;
                                }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ValidAdjacency() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Checks if the letters placed by the player form valid words or not.
        /// </summary>
        /// <param name="boardCellList"></param>
        /// <returns>True/False according to the legitimacy of the valid words in the dictionary.</returns>
        internal bool ValidateWord(Stack<BoardCell> boardCellList, char[,] matrix, int currentPlayer, int currentTurn, bool starCheckNeeded)
        {
            try
            {
                BoardCell boardCell = boardCellList.ElementAt(0);
                List<ValidWordWithScore> validWords = new List<ValidWordWithScore>();
                List<string> invalidWords = new List<string>();

                // First check if the main word line formulates any valid word.
                CheckValidity(boardCell.X, boardCell.Y, CurrentWordDirection, matrix, validWords, invalidWords, boardCellList);

                // If central tile check is not needed, then this is not the first word. It should cross through (or touch) existing words.
                //if (validWords.Count > 0)
                //    if (!starCheckNeeded && !CurrentWordCrossedThroughExistingWord(boardCellList.Select(a => a).ToList(), matrix, currentPlayer, currentTurn))
                //        return false;

                // Now check if the letters placed by the player are adjacent to other letters that are already placed on the board.
                // If there is any such letters, then they should also formulate valid dictionary words. Else this turn is void, the
                // letters are returned to the rack and the turn moves to the next player.
                // E.g.: If the player places CAT above BAT, then CA and AT should also formulate valid dictionary words. Else void.
                //            C A T
                //          B A T
                if (CurrentWordDirection == WordDirectionEnum.Horizontal)
                {
                    // Take a clone of the letters that were placed on the board in sorted order from left to right.
                    List<BoardCell> boardCells = boardCellList.Select(a => a).OrderBy(b => b.X).ToList();
                    int y = boardCells[0].Y;
                    int yBelow = y + 1;
                    int yAbove = y - 1;

                    // Check if there are letters below or above the horizontally placed word.
                    // The bottom row to check could be less than or equal to the last row (GRID_SIZE).
                    // The top row to check could be greater than or equal to the first row (0).
                    for (int i = 0, x; i < boardCells.Count; i++)
                    {
                        x = boardCells[i].X;
                        if ((yBelow < GRID_SIZE && matrix[x, yBelow] != '\0') || (yAbove >= 0 && matrix[x, yAbove] != '\0'))
                            CheckValidity(x, y, WordDirectionEnum.Vertical, matrix, validWords, invalidWords, boardCellList);
                    }
                }

                // Now check if the letters placed by the player are adjacent to other letters that are already placed on the board.
                // If there is any such letters, then they should also formulate valid dictionary words. Else this turn is void, the
                // letters are returned to the rack and the turn moves to the next player.
                // E.g.: If the player places TAN aside BAT, then AT and TA should also formulate valid dictionary words. Else void.
                //            B
                //            A T
                //            T A
                //              N
                else if (CurrentWordDirection == WordDirectionEnum.Vertical)
                {
                    // Take a clone of the letters that were placed on the board in sorted order from left to right.
                    List<BoardCell> boardCells = boardCellList.Select(a => a).OrderBy(b => b.Y).ToList();
                    int x = boardCells[0].X;
                    int xLeft = x - 1;
                    int xRight = x + 1;                    

                    // Check if there are letters to the left or right of the vertically placed word.
                    // The right column to check could be less than or equal to the last column (GRID_SIZE).
                    // The left column to check could be greater than or equal to the first column (0).
                    for (int i = 0, y; i < boardCells.Count; i++)
                    {
                        y = boardCells[i].Y;
                        if ((xRight < GRID_SIZE && matrix[xRight, y] != '\0') || (xLeft >= 0 && matrix[xLeft, y] != '\0'))
                            CheckValidity(x, y, WordDirectionEnum.Horizontal, matrix, validWords, invalidWords, boardCellList);
                    }
                }

                if (invalidWords.Count > 0)
                {
                    string valids = validWords.Select(x => x.Word).ToList().Count == 0 ? "None" : string.Join(", ", validWords.Select(x => x.Word));
                    MessageBox.Show($"Valid word(s): {Environment.NewLine}{valids}.{Environment.NewLine}{Environment.NewLine}" +
                                    $"Invalid word(s):{Environment.NewLine}{string.Join(", ", invalidWords)}" +
                                    $".{Environment.NewLine}{Environment.NewLine}" +
                                    $"All the coined words should be valid.{Environment.NewLine}" +
                                    $"No score is awarded, turn moves to the next player."
                                    , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Players[currentPlayer].ScoreDetails.Add(new TurnsWithScores(currentTurn, $"Invalid word(s): {string.Join(", ", invalidWords)}.{Environment.NewLine}{Environment.NewLine}", validWords));
                    return false;
                }

                DisplayAndRecordScoreDetails(validWords, invalidWords, currentPlayer, currentTurn, boardCellList.Count == 7, starCheckNeeded);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the ValidateWord() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static void DisplayAndRecordScoreDetails(List<ValidWordWithScore> validWords, List<string> invalidWords, int currentPlayer, int currentTurn, bool bingo, bool starCheckNeeded)
        {
            try
            {
                StringBuilder str = new StringBuilder();
                int totalIndivididualScore = 0, grandTotal = 0;      // totalIndivididualScore keeps track of individual word score for the current turn. grandTotal cumulatively adds all the word scores.
                int currentLetterScore;
                foreach (ValidWordWithScore word in validWords)
                {
                    str.Append($"Score for word:{word.Word}{Environment.NewLine}");
                    string scoreStr = "";
                    totalIndivididualScore = 0;
                    foreach (IndividualScore score in word.Score)
                    {
                        if (score.PremiumContent == "2L")            // For a special cell 2L, the score of the letter will be doubled.
                        {
                            currentLetterScore = score.Score * 2;
                            scoreStr = $"({score.Letter}({score.Score}x{score.PremiumContent})={currentLetterScore})";   // E.g.: A(1x2L)=2
                        }
                        else if (score.PremiumContent == "3L")       // For a special cell 3L, the score of the letter will be tripled.
                        {
                            currentLetterScore = score.Score * 3;
                            scoreStr = $"{score.Letter}({score.Score}x({score.PremiumContent})={currentLetterScore})";   // E.g.: A(1x3L)=3
                        }
                        else if (score.PremiumContent == "2W")       // For a special cell 2W, the score of the whole word will be doubled.
                        {
                            currentLetterScore = score.Score;
                            scoreStr = $"{score.Letter}({score.Score}({score.PremiumContent})={currentLetterScore})";    // E.g.: A(1(2W)=1)
                        }
                        else if (score.PremiumContent == "3W")       // For a special cell 3W, the score of the whole word will be tripled.
                        {
                            currentLetterScore = score.Score;
                            scoreStr = $"{score.Letter}({score.Score}({score.PremiumContent})={currentLetterScore})";    // E.g.: A(1(3W)=1)
                        }
                        else
                        {
                            currentLetterScore = score.Score;
                            scoreStr = $"{score.Letter}({currentLetterScore})";
                        }
                        str.Append($"{scoreStr} + ");
                        totalIndivididualScore += currentLetterScore;
                    }                    
                    str.Remove(str.ToString().LastIndexOf('+') - 1, 2);
                    str.Append($"= {totalIndivididualScore}");
                    string wordAugmenter = "";

                    // There might be more than one 2W, 3W premium cells in the current word.
                    foreach (IndividualScore score in word.Score)
                    {
                        if (score.PremiumContent == "2W")
                        {
                            totalIndivididualScore *= 2;
                            wordAugmenter += score.PremiumContent + ", ";
                        }
                        else if (score.PremiumContent == "3W")
                        {
                            totalIndivididualScore *= 3;
                            wordAugmenter += score.PremiumContent + ", ";
                        }
                    }

                    if (!string.IsNullOrEmpty(wordAugmenter))
                    {
                        wordAugmenter = wordAugmenter.Remove(wordAugmenter.LastIndexOf(", "), 2);   // Remove the last comma.
                        str.Append($", multiplied by word augmenters {wordAugmenter} = {totalIndivididualScore}");
                    }
                    if (starCheckNeeded)       // For the first turn only, the score should be doubled.
                    {
                        totalIndivididualScore *= 2;
                        str.Append($" + first word bonus (x2) = {totalIndivididualScore}");
                    }
                    if (bingo)
                    {
                        totalIndivididualScore += BINGO_SCORE;
                        str.Append($" + BINGO bonus (+ {BINGO_SCORE}) = {totalIndivididualScore}");
                    }
                    grandTotal += totalIndivididualScore;
                    str.Append($"{Environment.NewLine}{Environment.NewLine}");
                }
                MessageBox.Show($"Valid words:{Environment.NewLine}{string.Join(", ", validWords.Select(x => x.Word))}" +
                                $"{Environment.NewLine}{Environment.NewLine}" +
                                $"Score details:" +
                                $"{Environment.NewLine}{Environment.NewLine}" +
                                str.ToString(), Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                Players[currentPlayer].TotalScore += totalIndivididualScore;                         // Cumulative total.
                Players[currentPlayer].ScoreDetails.Add(new TurnsWithScores(currentTurn, str.ToString(), validWords));
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the DisplayAndRecordScoreDetails() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// This function checks if a word that is already to the left of the word rightfully passes through the word to be placed.
        /// </summary>
        /// <param name="x">Intended x-position of the word to be placed</param>
        /// <param name="y">Intended y-position of the word to be placed</param>
        /// <param name="direction">The direction to search for from the (x, y)</param>
        private void CheckValidity(int x, int y, WordDirectionEnum direction, char[,] matrix,
            List<ValidWordWithScore> validWords, List<string> invalidWords, Stack<BoardCell> boardCellList)
        {
            char[] chars = new char[MAX_WORD_LENGTH];
            int startX, startY;
            List<IndividualScore> score = new List<IndividualScore>();
            try
            {
                switch (direction)
                {
                    case WordDirectionEnum.Horizontal:
                        while (--x >= 0)
                            if (matrix[x, y] == '\0') break;                    // First walk towards the left until you reach the beginning of the word that is already on the board.
                        
                        startX = ++x;                                           // Keep a track of where (x, y) this word started on the board.
                        startY = y;

                        string premium;
                        bool blankTile;
                        for (int i = 0; x < GRID_SIZE; x++, i++)                // Now walk towards right until you reach the end of the word that is already on the board.
                        {
                            if (matrix[x, y] == '\0') break;
                            chars[i] = matrix[x, y];
                            premium = GetPreimumContent(x, y, boardCellList);
                            blankTile = CheckIfBlankTile(x, y);
                            score.Add(new IndividualScore(blankTile ? ' ' : chars[i], premium, new Point(x, y)));
                        }

                        string str = new string(chars);
                        str = str.Trim('\0');
                        if (WordFound(str.ToLower(), x, y, out bool existingWord))
                        {
                            validWords.Add(new ValidWordWithScore(str, score, new Point(x, y))); // Add it to the valid list.
                            return;
                        }
                        if (!existingWord)
                            invalidWords.Add(str);                              // Else, add it to the invalid list.
                        break;
                    case WordDirectionEnum.Vertical:
                        while (--y >= 0)
                            if (matrix[x, y] == '\0') break;                    // First walk upwards until you reach the beginning of the word that is already on the board.

                        startX = x;                                             // Keep a track of where (x, y) this word started on the board.
                        startY = ++y;

                        for (int i = 0; y < GRID_SIZE; y++, i++)                // Now walk downwards until you reach the end of the word that is already on the board.
                        {
                            if (matrix[x, y] == '\0') break;
                            chars[i] = matrix[x, y];
                            premium = GetPreimumContent(x, y, boardCellList);
                            blankTile = CheckIfBlankTile(x, y);
                            score.Add(new IndividualScore(blankTile ? ' ' : chars[i], premium, new Point(x, y)));
                        }

                        str = new string(chars);
                        str = str.Trim('\0');
                        if (WordFound(str.ToLower(), x, y, out existingWord))
                        {
                            validWords.Add(new ValidWordWithScore(str, score, new Point(x, y)));    // Add it to the valid list.
                            return;
                        }
                        if (!existingWord)
                            invalidWords.Add(str);                                                  // Else, add it to the invalid list.
                        break;
                    case WordDirectionEnum.None:
                        // Direction - none is the case when the player placed a single letter on the board.
                        str = boardCellList.FirstOrDefault(a => a.X == x && a.Y == y).AfterDragCellContent;
                        if (WordFound(str.ToLower(), x, y, out existingWord))
                        {
                            validWords.Add(new ValidWordWithScore(str, score, new Point(x, y)));    // Add it to the valid list.
                            return;
                        }
                        if (!existingWord)
                            invalidWords.Add(str);                                                  // Else, add it to the invalid list.
                        break;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the CheckIfValidWord() method of the 'GameEngine' class.\n\n" +                                
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Checks if the current cell contains a (historical) blank tile.
        /// </summary>
        /// <param name="x">X-axis of the cell.</param>
        /// <param name="y">Y-axis of the cell.</param>
        /// <returns>True if the current cell contains a (historical) blank tile.</returns>
        private bool CheckIfBlankTile(int x, int y)
        {
            try
            {
                foreach (Point p in BlankTileRackLocations)     // There can be two blank tiles at best, hence two loops at best.
                    if (p.X == x && p.Y == y)
                        return true;
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the CheckIfBlankTile() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Returns special cell content if any.
        /// </summary>
        /// <param name="x">X-axis of the cell.</param>
        /// <param name="y">Y-axis of the cell.</param>
        /// <param name="boardCellList">The stack of cells where the player placed letters.</param>
        /// <returns>2W, 3W, 2L, 3L or blank.</returns>
        private string GetPreimumContent(int x, int y, Stack<BoardCell> boardCellList)
        {
            try
            {
                BoardCell cell = boardCellList.FirstOrDefault(a => a.X == x && a.Y == y);   // The cell at (x, y).
                return cell == null ? "" : cell.PremiumContent;                             // Return the premium content of the cell if any.
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the GetPreimumContent() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "";
            }
        }

        /// <summary>
        /// Checks for each word if that was already coined by another player in the same position.
        /// Same position check is needed as same word might be coined more than once.
        /// However, the current words check for the current player must not be that of an existing player.
        /// For example, say TOPAZ is the current word coined by the current player which crosses with MOST.
        /// Now we have to check if MOST was already coined by another player at the same position.
        /// If it was coined by an existing player on the same position, then MOST is not awarded to the current player. S/he will only have TOPAZ.
        ///         M
        ///       T O P A Z
        ///         S
        ///         T
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool WordFound(string str, int x, int y, out bool existingWord)
        {
            existingWord = false;
            try
            {                
                if (Words.IndexOf(str) == -1)   // If the word is not found in the dictionary,
                    return false;               // then return negative.
                foreach (PlayerDetails p in Players)
                {
                    foreach (TurnsWithScores t in p.ScoreDetails)
                        if (t.ValidWords != null)
                            foreach (ValidWordWithScore v in t.ValidWords)
                                if (v.Word == str.ToUpper() && v.Axis.X == x && v.Axis.Y == y)
                                {
                                    existingWord = true;
                                    return false;
                                }
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the WordFound() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public void LoadLetterBag()
        {
            try
            {
                for (int letter = 65, i = 0; letter < 91; letter++)
                    switch (letter)
                    {
                        case 65:    // Should be 9 'A'
                            for (int count = 0; count < 9; count++)
                                LetterBag[i++] = (char)letter;
                            break;
                        case 66:    // Should be 2 'B'
                        case 67:    // Should be 2 'C'
                        case 70:    // Should be 2 'F'
                        case 72:    // Should be 2 'H'
                        case 77:    // Should be 2 'M'
                        case 80:    // Should be 2 'P'
                        case 86:    // Should be 2 'V'
                        case 87:    // Should be 2 'W'
                        case 89:    // Should be 2 'Y'
                            for (int count = 0; count < 2; count++)
                                LetterBag[i++] = (char)letter;
                            break;
                        case 68:    // Should be 4 'D'
                        case 76:    // Should be 4 'L'
                        case 83:    // Should be 4 'S'
                        case 85:    // Should be 4 'U'
                            for (int count = 0; count < 4; count++)
                                LetterBag[i++] = (char)letter;
                            break;
                        case 69:    // Should be 12 'E'
                            for (int count = 0; count < 12; count++)
                                LetterBag[i++] = (char)letter;
                            break;
                        case 71:    // Should be 3 'G'
                            for (int count = 0; count < 3; count++)
                                LetterBag[i++] = (char)letter;
                            break;
                        case 73:    // Should be 9 'I'
                            for (int count = 0; count < 9; count++)
                                LetterBag[i++] = (char)letter;
                            break;
                        case 74:    // Should be 1 'J'
                        case 75:    // Should be 1 'K'
                        case 81:    // Should be 1 'Q'
                        case 88:    // Should be 1 'X'
                        case 90:    // Should be 1 'Z'
                            LetterBag[i++] = (char)letter;
                            break;
                        case 78:    // Should be 6 'N'
                        case 82:    // Should be 6 'R'
                        case 84:    // Should be 6 'T'
                            for (int count = 0; count < 6; count++)
                                LetterBag[i++] = (char)letter;
                            break;
                        case 79:    // Should be 8 'O'
                            for (int count = 0; count < 8; count++)
                                LetterBag[i++] = (char)letter;
                            break;
                    }
                LetterBag[98] = LetterBag[99] = ' ';        // Two blank tiles.
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred in the LoadLetterBag() method of the 'GameEngine' class.\n\n" +
                                $"\n\nError msg: {e.Message}", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Checks if the first word passed through the central star tile.
        /// </summary>
        /// <param name="boardCellList">The list of cells where the player placed letters.</param>
        /// <returns>True if the word passed through the central star tile, false otherwise.</returns>
        internal bool FirstWordThroughCentralStarTile(Stack<BoardCell> boardCellList)
        {
            try
            {
                foreach (BoardCell c in boardCellList)          // Loop through all the cells.
                    if (c.X == 7 && c.Y == 7)                   // If any of them contains (7, 7) - the central tile, then return true.
                        return true;
                MessageBox.Show("The first word should pass through the central star tile. Use CTRL+Z to return the letters, then place through the star."
                    , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;                                   // If none of them contains (7, 7), then return false.
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred while determining if the first word passes through the central star tile. Error msg: {e.Message}"
                    , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Checks if the current word crosses or touches any existing word on the board.
        /// </summary>
        /// <param name="letters">The cells on the board where the current player dropped letters.</param>
        /// <param name="matrix">The character matrix.</param>
        /// <returns></returns>
        internal bool CurrentWordCrossedThroughExistingWord(List<BoardCell> letters, char[,] matrix, int currentPlayer, int currentTurn)
        {
            try
            {
                int x, y;
                switch (CurrentWordDirection)
                {
                    // For a horizontal word, we need to check the following square-marked positions.
                    //      _ _ _
                    //    _|_|_|_|_
                    //   |_|C A T|_|
                    //     |_|_|_|
                    //
                    case WordDirectionEnum.Horizontal:
                        letters = letters.Select(a => a).OrderBy(a => a.X).ToList(); // Take a clone of the stack and order from left to right.

                        // ************************************************************************************************
                        // ************************* Check if the left cell has a letter. *********************************
                        //    __|_|
                        //   |x_|C|
                        //      |_|
                        // ************************************************************************************************
                        x = letters.First().X - 1;              // Check left of the first letter.
                        y = letters.First().Y;
                        if (x >= 0)                             // If not falling out of grid.
                            if (matrix[x, y] != '\0')           // If there is a non-NULL character in the cell.
                                return true;                    // Then return true.

                        // ************************************************************************************************
                        // ************************* Check if the right cell has a letter. ********************************
                        //   ______
                        //   |T|_x|
                        // ************************************************************************************************
                        x = letters.Last().X + 1;               // Check right of the last letter.
                        y = letters.Last().Y;
                        if (x < GRID_SIZE)                      // If not falling out of grid.
                            if (matrix[x, y] != '\0')           // If there is a non-NULL character in the cell.
                                return true;                    // Then return true.

                        // ************************************************************************************************
                        // ****** Now traverse along from left to right and see if there is a letter on top or bottom. ****
                        //   __________
                        //   |x_|x_|x_|
                        //   |_C|_A|_T|     // Suppose A was already on the board, and the current player placed C and T.
                        //   |x_|x_|x_|     // We have to check if there is any letter on top or bottom of C or T.
                        //
                        // ************************************************************************************************
                        foreach (BoardCell c in letters)
                        {
                            x = c.X;
                            y = c.Y - 1;                        // Check top.
                            if (y >= 0)                         // If not falling out of grid.
                                if (matrix[x, y] != '\0')       // If there is a non-NULL character in the cell.
                                    return true;                // Then return true.

                            x = c.X;
                            y = c.Y + 1;                        // Check bottom.
                            if (y < GRID_SIZE)                  // If not falling out of grid.
                                if (matrix[x, y] != '\0')       // If there is a non-NULL character in the cell.
                                    return true;                // Then return true.
                        }

                        // ************************************************************************************************
                        // ****** Now traverse along all cells in between the letters from left to right
                        // ****** and confirm if there is a letter in between.
                        //   __________
                        //   |x_|x_|x_|
                        //   |_C|_A|_T|     // Suppose 'A' was already on the board, and the current player placed C and T.
                        //   |x_|x_|x_|     // We have to check if there is any letter in between C and T.
                        //
                        // ************************************************************************************************
                        for (x = letters.First().X + 1, y = letters.First().Y; x < letters.Last().X; x++)
                            if (letters.FirstOrDefault(a => a.X == x && a.Y == y) == null)  // If this is not already in the current player's cell list.
                                if (matrix[x, y] != '\0')       // If there is a non-NULL character in the cell.
                                    return true;                // Then return true.
                        break;

                    // For a vertical word, we need to check the following square-marked positions.
                    //      _ 
                    //    _|_|_
                    //   |_|C|_|
                    //   |_|A|_|
                    //   |_|T|_|
                    //     |_|
                    case WordDirectionEnum.Vertical:
                        letters = letters.Select(a => a).OrderBy(a => a.Y).ToList(); // Take a clone of the stack and order from top to bottom.
                        // ************************************************************************************************
                        // ************************* Check if the top cell has a letter. **********************************
                        //     ____
                        //    _|x_|_
                        //   |_| C|_|
                        // ************************************************************************************************
                        x = letters.First().X;
                        y = letters.First().Y - 1;              // Check top of the first letter.
                        if (y >= 0)                             // If not falling out of grid.
                            if (matrix[x, y] != '\0')           // If there is a non-NULL character in the cell.
                                return true;                    // Then return true.

                        // ************************************************************************************************
                        // ************************* Check if the bottom cell has a letter. *******************************
                        //   ________
                        //   |_|T_|_|
                        //     |_x|
                        // ************************************************************************************************
                        x = letters.Last().X;
                        y = letters.Last().Y + 1;               // Check bottom of the last letter.
                        if (y < GRID_SIZE)                      // If not falling out of grid.
                            if (matrix[x, y] != '\0')           // If there is a non-NULL character in the cell.
                                return true;                    // Then return true.

                        // ************************************************************************************************
                        // ****** Now traverse along from top to bottom and see if there is a letter on left or right. ****
                        //   |x_|C|_x|      // Suppose A was already on the board, and the current player placed C and T.
                        //   |x_|A|_x|      // We have to check if there is any letter to the left or right of C or T.
                        //   |x_|T|_x|
                        // ************************************************************************************************
                        foreach (BoardCell c in letters)
                        {
                            x = c.X - 1;                        // Check left.
                            y = c.Y;
                            if (x >= 0)                         // If not falling out of grid.
                                if (matrix[x, y] != '\0')       // If there is a non-NULL character in the cell.
                                    return true;                // Then return true.

                            x = c.X + 1;                        // Check right.
                            y = c.Y;
                            if (x < GRID_SIZE)                  // If not falling out of grid.
                                if (matrix[x, y] != '\0')       // If there is a non-NULL character in the cell.
                                    return true;                // Then return true.
                        }

                        // ************************************************************************************************
                        // ****** Now traverse along all cels in between the letters from top to bottom
                        // ****** and confirm if there is a letter in between.
                        //   |x_|C|_x|      // Suppose 'A' was already on the board, and the current player placed C and T.
                        //   |x_|A|_x|      // We have to check if there is any letter in between C and T.
                        //   |x_|T|_x|
                        // ************************************************************************************************
                        for (x = letters.First().X, y = letters.First().Y + 1; y < letters.Last().Y; y++)
                            if (letters.FirstOrDefault(a => a.X == x && a.Y == y) == null)  // If this is not already in the current player's cell list.
                                if (matrix[x, y] != '\0')       // If there is a non-NULL character in the cell.
                                    return true;                // Then return true.
                        break;
                }
                MessageBox.Show("The word didn't cross through, or touched another word. Not a valid placement. Press CTRL+Z to return the letters and try again."
                    , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                //Players[currentPlayer].ScoreDetails.Add(new TurnsWithScores(currentTurn, $"Disjoint word coined.", null));
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show($"An error occurred while determining if the current word crosses existing word(S). Error msg: {e.Message}"
                    , Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}