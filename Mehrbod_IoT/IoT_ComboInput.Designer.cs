namespace Mehrbod_IoT
{
    partial class IoT_ComboInput
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
            this.pictureBox_Pic = new System.Windows.Forms.PictureBox();
            this.label_PromptText = new System.Windows.Forms.Label();
            this.comboBox_Options = new System.Windows.Forms.ComboBox();
            this.button_OK = new System.Windows.Forms.Button();
            this.button_Cancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Pic)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox_Pic
            // 
            this.pictureBox_Pic.Image = global::Mehrbod_IoT.Properties.Resources.img_Internet1;
            this.pictureBox_Pic.Location = new System.Drawing.Point(12, 12);
            this.pictureBox_Pic.Name = "pictureBox_Pic";
            this.pictureBox_Pic.Size = new System.Drawing.Size(128, 128);
            this.pictureBox_Pic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox_Pic.TabIndex = 0;
            this.pictureBox_Pic.TabStop = false;
            // 
            // label_PromptText
            // 
            this.label_PromptText.AutoEllipsis = true;
            this.label_PromptText.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.label_PromptText.Location = new System.Drawing.Point(146, 12);
            this.label_PromptText.Name = "label_PromptText";
            this.label_PromptText.Size = new System.Drawing.Size(360, 86);
            this.label_PromptText.TabIndex = 0;
            this.label_PromptText.Text = "متن پیام";
            // 
            // comboBox_Options
            // 
            this.comboBox_Options.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.comboBox_Options.Cursor = System.Windows.Forms.Cursors.Hand;
            this.comboBox_Options.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.comboBox_Options.FormattingEnabled = true;
            this.comboBox_Options.Location = new System.Drawing.Point(146, 101);
            this.comboBox_Options.Name = "comboBox_Options";
            this.comboBox_Options.Size = new System.Drawing.Size(360, 39);
            this.comboBox_Options.Sorted = true;
            this.comboBox_Options.TabIndex = 1;
            // 
            // button_OK
            // 
            this.button_OK.BackColor = System.Drawing.Color.PaleGreen;
            this.button_OK.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button_OK.Image = global::Mehrbod_IoT.Properties.Resources.ico_OK_30x30;
            this.button_OK.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button_OK.Location = new System.Drawing.Point(378, 155);
            this.button_OK.Name = "button_OK";
            this.button_OK.Size = new System.Drawing.Size(128, 36);
            this.button_OK.TabIndex = 2;
            this.button_OK.Text = "تأیید";
            this.button_OK.UseVisualStyleBackColor = false;
            // 
            // button_Cancel
            // 
            this.button_Cancel.BackColor = System.Drawing.Color.NavajoWhite;
            this.button_Cancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_Cancel.Image = global::Mehrbod_IoT.Properties.Resources.ico_NO_30x30;
            this.button_Cancel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.button_Cancel.Location = new System.Drawing.Point(244, 155);
            this.button_Cancel.Name = "button_Cancel";
            this.button_Cancel.Size = new System.Drawing.Size(128, 36);
            this.button_Cancel.TabIndex = 3;
            this.button_Cancel.Text = "لغو";
            this.button_Cancel.UseVisualStyleBackColor = false;
            // 
            // IoT_ComboInput
            // 
            this.AcceptButton = this.button_OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button_Cancel;
            this.ClientSize = new System.Drawing.Size(518, 203);
            this.Controls.Add(this.button_Cancel);
            this.Controls.Add(this.button_OK);
            this.Controls.Add(this.comboBox_Options);
            this.Controls.Add(this.label_PromptText);
            this.Controls.Add(this.pictureBox_Pic);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "IoT_ComboInput";
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "عنوان پنجره";
            this.Load += new System.EventHandler(this.IoT_ComboInput_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Pic)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        public ComboBox comboBox_Options;
        private Button button_OK;
        private Button button_Cancel;
        public PictureBox pictureBox_Pic;
        public Label label_PromptText;
    }
}