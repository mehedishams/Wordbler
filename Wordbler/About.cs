using Wordbler.Classes;
using System;
using System.Drawing;
using System.Windows.Forms;
using static Wordbler.Classes.Globals;

namespace Wordbler
{
    public partial class About : Form
    {
        WindowScaler scaler;
        int scale, calibration;

        public About()
        {
            InitializeComponent();
        }

        private void About_Load(object sender, EventArgs e)
        {
            ScaleBoardIfNecessary(out scaler, Screen.PrimaryScreen.Bounds, out scale, out calibration);
            ScaleControls(Controls);
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