namespace Wordbler
{
    partial class DisplayScoreDetails
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblPlayer = new System.Windows.Forms.Label();
            this.txtScoreDetails = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblPlayer
            // 
            this.lblPlayer.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPlayer.Location = new System.Drawing.Point(-5, 9);
            this.lblPlayer.Name = "lblPlayer";
            this.lblPlayer.Size = new System.Drawing.Size(810, 33);
            this.lblPlayer.TabIndex = 0;
            this.lblPlayer.Text = "lblPlayer";
            this.lblPlayer.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtScoreDetails
            // 
            this.txtScoreDetails.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtScoreDetails.Location = new System.Drawing.Point(29, 45);
            this.txtScoreDetails.Multiline = true;
            this.txtScoreDetails.Name = "txtScoreDetails";
            this.txtScoreDetails.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtScoreDetails.Size = new System.Drawing.Size(757, 645);
            this.txtScoreDetails.TabIndex = 1;
            // 
            // DisplayScoreDetails
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(817, 747);
            this.Controls.Add(this.txtScoreDetails);
            this.Controls.Add(this.lblPlayer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DisplayScoreDetails";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Display Score Details";
            this.Load += new System.EventHandler(this.DisplayScoreDetails_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblPlayer;
        private System.Windows.Forms.TextBox txtScoreDetails;
    }
}