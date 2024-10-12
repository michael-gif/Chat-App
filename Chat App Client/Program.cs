namespace Chat_App_Client
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();
            string username;
            ApplicationConfiguration.Initialize();
            if (args.Length > 1)
            {
                username = args[1];
            } else
            {
                username = UsernameInputBox();
                if (string.IsNullOrEmpty(username))
                {
                    MessageBox.Show("You must provide a username.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            Application.Run(new Form1(username));
        }

        static string UsernameInputBox()
        {
            Size size = new Size(275, 70);
            Form inputBox = new Form();
            inputBox.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputBox.ClientSize = size;
            inputBox.Text = "Pick a username";
            inputBox.Location = new Point(-1, -1);
            inputBox.MaximizeBox = false;
            inputBox.StartPosition = FormStartPosition.CenterScreen;

            TextBox textBox = new TextBox();
            textBox.Size = new Size(size.Width - 10, 23);
            textBox.Location = new Point(5, 5);
            textBox.PlaceholderText = "Username";
            inputBox.Controls.Add(textBox);

            Button okButton = new Button();
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            okButton.Name = "okButton";
            okButton.Size = new System.Drawing.Size(100, 23);
            okButton.Text = "&OK";
            okButton.Location = new System.Drawing.Point(150 - 105, 39);
            inputBox.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new System.Drawing.Size(100, 23);
            cancelButton.Text = "&Cancel";
            cancelButton.Location = new System.Drawing.Point(150 + 5, 39);
            inputBox.Controls.Add(cancelButton);

            inputBox.AcceptButton = okButton;
            inputBox.CancelButton = cancelButton;
            inputBox.ShowDialog();
            return textBox.Text;
        }
    }
}