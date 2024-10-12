using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_App_Client
{
    public partial class ConnectToServerForm : Form
    {
        public string Address { get { return addressTextBox.Text; } }
        public int Port { get { return (int)portNumericUpDown.Value; } }
        public ConnectToServerForm()
        {
            InitializeComponent();
        }
    }
}
