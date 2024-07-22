using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Wordbler.Classes;
using static Wordbler.Classes.Globals;

namespace Wordbler
{
    public partial class DisplayScoreDetails : Form
    {
        private PlayerDetails PlayerDetails { get; set; }
        WindowScaler scaler;
        int scale, calibration;

        public DisplayScoreDetails()
        {
            InitializeComponent();
        }
        public void AddPlayer(PlayerDetails playerDetails)
        {
            PlayerDetails = playerDetails;            
        }

        private void DisplayScoreDetails_Load(object sender, EventArgs e)
        {
            ScaleBoardIfNecessary(out scaler, Screen.PrimaryScreen.Bounds, out scale, out calibration);
            Width = scaler.GetMetrics(Width, "Width");                  // Form width and height set up.
            Height = scaler.GetMetrics(Height, "Height");
            Left = Screen.GetBounds(this).Width / 2 - Width / 2;        // Form centering.
            Top = Screen.GetBounds(this).Height / 2 - Height / 2 - 30;  // 30 is a calibration factor.
            ScaleControls(Controls);

            lblPlayer.Text = $"'{PlayerDetails.Name}' scores a total of {PlayerDetails.TotalScore}.";
            StringBuilder str = new StringBuilder();

            string validWords;
            foreach (TurnsWithScores s in PlayerDetails.ScoreDetails)
            {
                validWords = s.ValidWords == null || s.ValidWords.Count == 0 ? "None" : string.Join(", ", s.ValidWords.Select(a => a.Word));
                str.Append($"Turn: {s.Turn}{Environment.NewLine}Valid words: {validWords}{Environment.NewLine}{s.DetailedScore}{Environment.NewLine}");
            }

            txtScoreDetails.Text = str.ToString();
        }

        private void ScaleControls(Control.ControlCollection controls)
        {
            foreach (Control ctl in controls)
                Scale(ctl);
        }

        private void Scale(Control ctl)
        {
            ctl.Font = new Font(FontFamily.GenericSansSerif, scaler.GetMetrics((int)ctl.Font.Size), FontStyle.Regular);
            ctl.Width = scaler.GetMetrics(ctl.Width, "Width");
            ctl.Height = scaler.GetMetrics(ctl.Height, "Height");
            ctl.Top = scaler.GetMetrics(ctl.Top, "Top");
            ctl.Left = scaler.GetMetrics(ctl.Left, "Left");
        }
    }
}