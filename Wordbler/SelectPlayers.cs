using Wordbler.Classes;
using System;
using System.Drawing;
using System.Windows.Forms;
using static Wordbler.Classes.Globals;

namespace Wordbler
{
    public partial class SelectPlayers : Form
    {
        WindowScaler scaler;
        int scale, calibration;
        PictureBox mascot;

        public SelectPlayers()
        {
            InitializeComponent();
        }        
        
        #region Image selector panel's mouse-down events
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox1;
            pictureBox1.DoDragDrop(pictureBox1.Image, DragDropEffects.Copy);            
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox2;
            pictureBox2.DoDragDrop(pictureBox2.Image, DragDropEffects.Copy);            
        }

        private void pictureBox3_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox3;
            pictureBox3.DoDragDrop(pictureBox3.Image, DragDropEffects.Copy);            
        }

        private void pictureBox4_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox4;
            pictureBox4.DoDragDrop(pictureBox4.Image, DragDropEffects.Copy);            
        }

        private void pictureBox5_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox5;
            pictureBox5.DoDragDrop(pictureBox5.Image, DragDropEffects.Copy);            
        }

        private void pictureBox6_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox6;
            pictureBox6.DoDragDrop(pictureBox6.Image, DragDropEffects.Copy);            
        }

        private void pictureBox7_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox7;
            pictureBox7.DoDragDrop(pictureBox7.Image, DragDropEffects.Copy);            
        }

        private void pictureBox8_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox8;
            pictureBox8.DoDragDrop(pictureBox8.Image, DragDropEffects.Copy);            
        }

        private void pictureBox9_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox9;
            pictureBox9.DoDragDrop(pictureBox9.Image, DragDropEffects.Copy);            
        }

        private void pictureBox10_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox10;
            pictureBox10.DoDragDrop(pictureBox10.Image, DragDropEffects.Copy);
        }

        private void pictureBox11_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox11;
            pictureBox11.DoDragDrop(pictureBox11.Image, DragDropEffects.Copy);
        }

        private void pictureBox12_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox12;
            pictureBox12.DoDragDrop(pictureBox12.Image, DragDropEffects.Copy);
        }

        private void pictureBox15_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox15;
            pictureBox15.DoDragDrop(pictureBox15.Image, DragDropEffects.Copy);
        }

        private void pictureBox14_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox14;
            pictureBox14.DoDragDrop(pictureBox14.Image, DragDropEffects.Copy);
        }

        private void pictureBox13_MouseDown(object sender, MouseEventArgs e)
        {
            mascot = pictureBox13;
            pictureBox13.DoDragDrop(pictureBox13.Image, DragDropEffects.Copy);
        }
        #endregion

        private void cmbPlayers_SelectedIndexChanged(object sender, EventArgs e)
        {
            panelImagesCollection.Visible = lblInstructions.Visible = btnOk.Visible = true;
            if (cmbPlayers.Text == "2")
            {
                ShowOrHidePlayerDetails(1, true);
                ShowOrHidePlayerDetails(2, true);
                ShowOrHidePlayerDetails(3, false);
                ShowOrHidePlayerDetails(4, false);
            }
            else if (cmbPlayers.Text == "3")
            {
                ShowOrHidePlayerDetails(1, true);
                ShowOrHidePlayerDetails(2, true);
                ShowOrHidePlayerDetails(3, true);
                ShowOrHidePlayerDetails(4, false);
            }
            else if (cmbPlayers.Text == "4")
            {
                ShowOrHidePlayerDetails(1, true);
                ShowOrHidePlayerDetails(2, true);
                ShowOrHidePlayerDetails(3, true);
                ShowOrHidePlayerDetails(4, true);
            }
        }

        private void ShowOrHidePlayerDetails(int playerNumber, bool visibility)
        {
            Controls.Find($"lblPlayer{playerNumber}", true)[0].Visible = visibility;
            Controls.Find($"lblP{playerNumber}Instruction", true)[0].Visible = visibility;
            Controls.Find($"txtPlayer{playerNumber}", true)[0].Visible = visibility;
            Controls.Find($"panelPlayer{playerNumber}Mascot", true)[0].Visible = visibility;
        }

        #region Players' mascot boxes drop-enter events
        private void panelPlayer1Mascot_DragDrop(object sender, DragEventArgs e)
        {
            panelPlayer1Mascot.BackgroundImage = (Image)e.Data.GetData(DataFormats.Bitmap);
            mascot.Visible = panelPlayer1Mascot.Enabled = false;            
        }

        private void panelPlayer1Mascot_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void panelPlayer2Mascot_DragDrop(object sender, DragEventArgs e)
        {
            panelPlayer2Mascot.BackgroundImage = (Image)e.Data.GetData(DataFormats.Bitmap);
            mascot.Visible = panelPlayer2Mascot.Enabled = false;
        }

        private void panelPlayer2Mascot_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void panelPlayer3Mascot_DragDrop(object sender, DragEventArgs e)
        {
            panelPlayer3Mascot.BackgroundImage = (Image)e.Data.GetData(DataFormats.Bitmap);
            mascot.Visible = panelPlayer3Mascot.Enabled = false;
        }

        private void panelPlayer3Mascot_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void panelPlayer4Mascot_DragDrop(object sender, DragEventArgs e)
        {
            panelPlayer4Mascot.BackgroundImage = (Image)e.Data.GetData(DataFormats.Bitmap);
            mascot.Visible = panelPlayer4Mascot.Enabled = false;
        }

        private void panelPlayer4Mascot_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }
        #endregion

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (lblPlayer1.Visible)
                if (!InputsOkay(1))
                    return;

            if (lblPlayer2.Visible)
                if (!InputsOkay(2))
                    return;

            if (lblPlayer3.Visible)
                if (!InputsOkay(3))
                    return;

            if (lblPlayer4.Visible)
                if (!InputsOkay(4))
                    return;

            if (lblPlayer1.Visible)
                Players.Add(new PlayerDetails(txtPlayer1.Text, panelPlayer1Mascot.BackgroundImage));
            if (lblPlayer2.Visible)
                Players.Add(new PlayerDetails(txtPlayer2.Text, panelPlayer2Mascot.BackgroundImage));
            if (lblPlayer3.Visible)
                Players.Add(new PlayerDetails(txtPlayer3.Text, panelPlayer3Mascot.BackgroundImage));
            if (lblPlayer4.Visible)
                Players.Add(new PlayerDetails(txtPlayer4.Text, panelPlayer4Mascot.BackgroundImage));

            Hide();
            Wordbler rover = new Wordbler();
            rover.ShowDialog();
        }

        private void SelectPlayers_Load(object sender, EventArgs e)
        {
            ScaleBoardIfNecessary(out scaler, Screen.PrimaryScreen.Bounds, out scale, out calibration);
            Width = scaler.GetMetrics(Width, "Width");                  // Form width and height set up.
            Height = scaler.GetMetrics(Height, "Height");
            Left = Screen.GetBounds(this).Width / 2 - Width / 2;        // Form centering.
            Top = Screen.GetBounds(this).Height / 2 - Height / 2 - 30;  // 30 is a calibration factor.                        
            ScaleControls(Controls);
        }

        private void ScaleControls(Control.ControlCollection controls)
        {
            foreach (Control ctl in controls)
            {
                Scale(ctl);
                if (ctl is Panel)   // Recursive call if it is a panel (which has more controls inside it).
                    ScaleControls(ctl.Controls);
            }
        }

        private void Scale(Control ctl)
        {
            ctl.Font = new Font(FontFamily.GenericSansSerif, scaler.GetMetrics((int)ctl.Font.Size), FontStyle.Regular);
            ctl.Width = scaler.GetMetrics(ctl.Width, "Width");
            ctl.Height = scaler.GetMetrics(ctl.Height, "Height");
            ctl.Top = scaler.GetMetrics(ctl.Top, "Top");
            ctl.Left = scaler.GetMetrics(ctl.Left, "Left");
        }       

        private bool InputsOkay(int playerNumber)
        {
            Control playerName = Controls.Find($"txtPlayer{playerNumber}", true)[0];
            Control playerMascot = Controls.Find($"panelPlayer{playerNumber}Mascot", true)[0];

            if (string.IsNullOrEmpty(playerName.Text))
            {
                MessageBox.Show($"Select a name for player {playerNumber}.", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            else if (playerMascot.BackgroundImage == null)
            {
                MessageBox.Show($"Select (drag) a mascot for '{playerName.Text}'.", Properties.Resources.APP_TITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }
    }
}