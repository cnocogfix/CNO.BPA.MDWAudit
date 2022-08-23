using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CNO.BPA.MDWAudit
{
    public partial class CustomParameterEditor1 : Form
    {
        CustomParameters _parmsCustom;
        string _processName;

        public CustomParameterEditor1(ref CustomParameters ParmsCustom, string ProcessName)
        {
            InitializeComponent();
            _parmsCustom = ParmsCustom;
            _processName = ProcessName;

            //assume that if the dsn is blank there is nothing we need to decrypt either
            if (_parmsCustom.DSN.Length > 0)
            {
                txtDSN.Text = _parmsCustom.DSN;
                Framework.Cryptography crypto = new Framework.Cryptography();
                txtUserID.Text = crypto.Decrypt(_parmsCustom.UserName);
                txtPassword.Text = crypto.Decrypt(_parmsCustom.Password);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Framework.Cryptography crypto = new Framework.Cryptography();
            _parmsCustom.DSN = txtDSN.Text;
            _parmsCustom.UserName = crypto.Encrypt(txtUserID.Text);
            _parmsCustom.Password = crypto.Encrypt(txtPassword.Text);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            Framework.Cryptography crypto = new Framework.Cryptography();
            _parmsCustom.DSN = txtDSN.Text;
            _parmsCustom.UserName = crypto.Encrypt(txtUserID.Text);
            _parmsCustom.Password = crypto.Encrypt(txtPassword.Text);
        }

    }
}
