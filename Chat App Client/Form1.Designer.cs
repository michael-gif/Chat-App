namespace Chat_App_Client
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            TreeNode treeNode1 = new TreeNode("Node1");
            TreeNode treeNode2 = new TreeNode("Node2");
            TreeNode treeNode3 = new TreeNode("Node3");
            TreeNode treeNode4 = new TreeNode("Node4");
            TreeNode treeNode5 = new TreeNode("Channels", new TreeNode[] { treeNode1, treeNode2, treeNode3, treeNode4 });
            DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
            channelTreeView = new TreeView();
            serverNameLabel = new Label();
            textBox1 = new TextBox();
            messageHistoryGridView = new DataGridView();
            UsernameColumn = new DataGridViewTextBoxColumn();
            MessageColumn = new DataGridViewTextBoxColumn();
            onlineUsersListView = new ListView();
            columnHeader1 = new ColumnHeader();
            sendMessageButton = new Button();
            connectToServerButton = new Button();
            ((System.ComponentModel.ISupportInitialize)messageHistoryGridView).BeginInit();
            SuspendLayout();
            // 
            // channelTreeView
            // 
            channelTreeView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            channelTreeView.Location = new Point(12, 27);
            channelTreeView.Name = "channelTreeView";
            treeNode1.Name = "Node1";
            treeNode1.Text = "Node1";
            treeNode2.Name = "Node2";
            treeNode2.Text = "Node2";
            treeNode3.Name = "Node3";
            treeNode3.Text = "Node3";
            treeNode4.Name = "Node4";
            treeNode4.Text = "Node4";
            treeNode5.Name = "Node0";
            treeNode5.Text = "Channels";
            channelTreeView.Nodes.AddRange(new TreeNode[] { treeNode5 });
            channelTreeView.ShowPlusMinus = false;
            channelTreeView.ShowRootLines = false;
            channelTreeView.Size = new Size(121, 411);
            channelTreeView.TabIndex = 0;
            channelTreeView.BeforeCollapse += channelTreeView_BeforeCollapse;
            channelTreeView.BeforeExpand += channelTreeView_BeforeExpand;
            channelTreeView.BeforeSelect += channelTreeView_BeforeSelect;
            // 
            // serverNameLabel
            // 
            serverNameLabel.AutoSize = true;
            serverNameLabel.Location = new Point(12, 9);
            serverNameLabel.Name = "serverNameLabel";
            serverNameLabel.Size = new Size(71, 15);
            serverNameLabel.TabIndex = 1;
            serverNameLabel.Text = "ServerName";
            // 
            // textBox1
            // 
            textBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBox1.Location = new Point(139, 418);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.PlaceholderText = "Enter message";
            textBox1.Size = new Size(404, 20);
            textBox1.TabIndex = 3;
            textBox1.TextChanged += UpdateTextBoxSize;
            textBox1.KeyDown += textBox1_KeyDown;
            // 
            // messageHistoryGridView
            // 
            messageHistoryGridView.AllowUserToAddRows = false;
            messageHistoryGridView.AllowUserToDeleteRows = false;
            messageHistoryGridView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            messageHistoryGridView.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            messageHistoryGridView.BackgroundColor = SystemColors.Window;
            messageHistoryGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            messageHistoryGridView.ColumnHeadersVisible = false;
            messageHistoryGridView.Columns.AddRange(new DataGridViewColumn[] { UsernameColumn, MessageColumn });
            dataGridViewCellStyle1.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = SystemColors.Window;
            dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            dataGridViewCellStyle1.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = DataGridViewTriState.True;
            messageHistoryGridView.DefaultCellStyle = dataGridViewCellStyle1;
            messageHistoryGridView.Location = new Point(139, 27);
            messageHistoryGridView.Name = "messageHistoryGridView";
            messageHistoryGridView.ReadOnly = true;
            messageHistoryGridView.RowHeadersVisible = false;
            messageHistoryGridView.RowTemplate.Height = 25;
            messageHistoryGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            messageHistoryGridView.Size = new Size(485, 385);
            messageHistoryGridView.TabIndex = 4;
            messageHistoryGridView.CellMouseEnter += messageHistoryGridView_CellMouseEnter;
            messageHistoryGridView.CellMouseLeave += messageHistoryGridView_CellMouseLeave;
            messageHistoryGridView.SelectionChanged += messageHistoryGridView_SelectionChanged;
            // 
            // UsernameColumn
            // 
            UsernameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            UsernameColumn.FillWeight = 20F;
            UsernameColumn.HeaderText = "Username";
            UsernameColumn.Name = "UsernameColumn";
            UsernameColumn.ReadOnly = true;
            // 
            // MessageColumn
            // 
            MessageColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            MessageColumn.FillWeight = 80F;
            MessageColumn.HeaderText = "Message";
            MessageColumn.Name = "MessageColumn";
            MessageColumn.ReadOnly = true;
            // 
            // onlineUsersListView
            // 
            onlineUsersListView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            onlineUsersListView.Columns.AddRange(new ColumnHeader[] { columnHeader1 });
            onlineUsersListView.FullRowSelect = true;
            onlineUsersListView.Location = new Point(630, 27);
            onlineUsersListView.Name = "onlineUsersListView";
            onlineUsersListView.Size = new Size(158, 411);
            onlineUsersListView.TabIndex = 5;
            onlineUsersListView.UseCompatibleStateImageBehavior = false;
            onlineUsersListView.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Online";
            columnHeader1.Width = 154;
            // 
            // sendMessageButton
            // 
            sendMessageButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            sendMessageButton.Location = new Point(549, 417);
            sendMessageButton.Name = "sendMessageButton";
            sendMessageButton.Size = new Size(75, 22);
            sendMessageButton.TabIndex = 6;
            sendMessageButton.Text = "Send";
            sendMessageButton.UseVisualStyleBackColor = true;
            sendMessageButton.Click += sendMessageButton_Click;
            // 
            // connectToServerButton
            // 
            connectToServerButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            connectToServerButton.Location = new Point(630, 5);
            connectToServerButton.Name = "connectToServerButton";
            connectToServerButton.Size = new Size(158, 23);
            connectToServerButton.TabIndex = 7;
            connectToServerButton.Text = "Connect to server";
            connectToServerButton.UseVisualStyleBackColor = true;
            connectToServerButton.Click += connectToServerButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(connectToServerButton);
            Controls.Add(sendMessageButton);
            Controls.Add(onlineUsersListView);
            Controls.Add(messageHistoryGridView);
            Controls.Add(textBox1);
            Controls.Add(serverNameLabel);
            Controls.Add(channelTreeView);
            Name = "Form1";
            Text = "Chat App";
            FormClosing += Form1_FormClosing;
            ((System.ComponentModel.ISupportInitialize)messageHistoryGridView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TreeView channelTreeView;
        private Label serverNameLabel;
        private TextBox textBox1;
        private DataGridView messageHistoryGridView;
        private DataGridViewTextBoxColumn UsernameColumn;
        private DataGridViewTextBoxColumn MessageColumn;
        private ListView onlineUsersListView;
        private ColumnHeader columnHeader1;
        private Button sendMessageButton;
        private Button connectToServerButton;
    }
}