namespace Chat_App_Client
{
    partial class ConnectToServerForm
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
            label1 = new Label();
            label2 = new Label();
            addressTextBox = new TextBox();
            okButton = new Button();
            cancelButton = new Button();
            portNumericUpDown = new NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)portNumericUpDown).BeginInit();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 9);
            label1.Name = "label1";
            label1.Size = new Size(122, 15);
            label1.TabIndex = 0;
            label1.Text = "Hostname/IP Address";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 38);
            label2.Name = "label2";
            label2.Size = new Size(29, 15);
            label2.TabIndex = 1;
            label2.Text = "Port";
            // 
            // addressTextBox
            // 
            addressTextBox.Location = new Point(140, 6);
            addressTextBox.Name = "addressTextBox";
            addressTextBox.Size = new Size(112, 23);
            addressTextBox.TabIndex = 2;
            addressTextBox.Text = "localhost";
            // 
            // okButton
            // 
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(12, 64);
            okButton.Name = "okButton";
            okButton.Size = new Size(118, 23);
            okButton.TabIndex = 4;
            okButton.Text = "OK";
            okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(134, 64);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(118, 23);
            cancelButton.TabIndex = 5;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // portNumericUpDown
            // 
            portNumericUpDown.Location = new Point(140, 35);
            portNumericUpDown.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            portNumericUpDown.Name = "portNumericUpDown";
            portNumericUpDown.Size = new Size(112, 23);
            portNumericUpDown.TabIndex = 6;
            portNumericUpDown.Value = new decimal(new int[] { 11000, 0, 0, 0 });
            // 
            // ConnectToServerForm
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(260, 96);
            Controls.Add(portNumericUpDown);
            Controls.Add(cancelButton);
            Controls.Add(okButton);
            Controls.Add(addressTextBox);
            Controls.Add(label2);
            Controls.Add(label1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConnectToServerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Connect to server";
            TopMost = true;
            ((System.ComponentModel.ISupportInitialize)portNumericUpDown).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private TextBox addressTextBox;
        private Button okButton;
        private Button cancelButton;
        private NumericUpDown portNumericUpDown;
    }
}