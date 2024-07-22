// ***************************************************************************************************************************************
// ****** Mehedi Shams Rony: Dec 2018*****************************************************************************************************
// ****** Purpose: Class that contains global variables and common methods. **************************************************************
// ***************************************************************************************************************************************
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace Wordbler.Classes
{
    //public enum Direction { Down = 1, Right, Left, Up, None };
    static class Globals
    {
        public const int GRID_SIZE = 15;
        public static Double MAX_ANIMATION_FRAMES = Convert.ToDouble(ConfigurationManager.AppSettings["MAX_ANIMATION_FRAMES"]);
        public static int MAX_WORD_LENGTH = Convert.ToInt16(ConfigurationManager.AppSettings["MAX_WORD_LENGTH"]);
        public static int MAX_SHAKE_PIXELS = Convert.ToInt16(ConfigurationManager.AppSettings["MAX_SHAKE_PIXELS"]);
        public static int MAX_SHAKE_COUNT = Convert.ToInt16(ConfigurationManager.AppSettings["MAX_SHAKE_COUNT"]);
        public static int MAX_FAIL_COUNT = Convert.ToInt16(ConfigurationManager.AppSettings["MAX_FAIL_COUNT"]);
        
        public static int THREAD_SLEEP = Convert.ToInt16(ConfigurationManager.AppSettings["THREAD_SLEEP"]); 
        public const int MAX_PLAYERS = 4;
        public const int BINGO_SCORE = 50;  // If a player is able to use all of the letters in the current turn, then s/he gets 50 points bonus called 'bingo'.

        private static readonly int MOUSE_X_CALIBRATION_PIXELS = Convert.ToInt16(ConfigurationManager.AppSettings["MOUSE_X_CALIBRATION_PIXELS"]);  // Calibration adjustment for the X-coordinate of the mouse, and also for placement of letters in the middle of boxes.
        public const int MAX_LETTERS = 7;
        public static int NUM_LETTERS_IN_BAG = 100;
        public enum WordDirectionEnum { Horizontal, Vertical, None };
        public static WordDirectionEnum CurrentWordDirection;
        public static List<PlayerDetails> Players = new List<PlayerDetails>();
        public static List<Point> BlankTileRackLocations = new List<Point>();
        public static char[] LetterBag = new char[NUM_LETTERS_IN_BAG];
        public static int CurrentPlayer;
        public static int CurrentTurn;

        public static void ScaleBoardIfNecessary(out WindowScaler scaler, Rectangle bounds, out int scaleFactor, out int calibrationFactor)
        {
            scaler = new WindowScaler(bounds);
            scaler.SetMultiplicationFactor();

            scaleFactor = scaler.GetMetrics(35);
            calibrationFactor = scaler.GetMetrics(MOUSE_X_CALIBRATION_PIXELS);
        }
                
        /// <summary>
        /// Processes the task queue.
        /// </summary>
        /// <param name="delayTime">How much time to delay and process - in milliseconds.</param>
        public static void DelayAndProcessWorks(int delayTime)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            while (stopWatch.Elapsed < TimeSpan.FromMilliseconds(delayTime))
                System.Windows.Forms.Application.DoEvents();
        }
    }
}
