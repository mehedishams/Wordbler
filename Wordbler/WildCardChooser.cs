using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using Wordbler.Classes;
using static Wordbler.Classes.Globals;

namespace Wordbler
{
    public partial class WildCardChooser : Form
    {
        Wordbler RoverForm;
        WindowScaler scaler;
        int scale, calibration;

        public WildCardChooser(Wordbler rover)
        {
            InitializeComponent();
            RoverForm = rover;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtLetter.Text)) return;
            RoverForm.WildCard = txtLetter.Text.ToCharArray()[0];
            Close();
        }

        /// <summary>
        /// checks for alpha only. No numeric, no space or any other character.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtLetter_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar > (char)64 && e.KeyChar < (char)91) ||                       // If any letter between 'A' and 'Z' are pressed,
                (e.KeyChar > (char)96 && e.KeyChar < (char)123))                        // or if any letter between 'a' and 'z' are pressed,
                txtLetter.Text = e.KeyChar.ToString().ToUpper();                        // then convert the letter to uppercase and put in the text box.
            else if (e.KeyChar == (char)13 && !string.IsNullOrEmpty(txtLetter.Text))    // Else if ENTER key is pressed and the textbox is not empty,
                btnOk_Click(sender, e);                                                 // then send message to the Ok button click handler to close the form.
            else e.Handled = true;                                                      // Else don't process this keystroke - it is not alpha.
        }

        /// <summary>
        /// For closing the search window by pressing ESC key.
        /// Reference: https://stackoverflow.com/questions/2290959/escape-button-to-close-windows-forms-form-in-c-sharp
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
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