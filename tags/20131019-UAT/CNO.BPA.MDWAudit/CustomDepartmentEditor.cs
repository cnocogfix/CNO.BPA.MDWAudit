using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CNO.BPA.ScanPop
{
    public partial class CustomDepartmentEditor : Form
    {
        public CustomDepartmentEditor()
        {
            InitializeComponent();
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            lstSelectedDepartments.Items.Add(lstAvailableDeptartments.SelectedItem.ToString());
            lstAvailableDeptartments.Items.RemoveAt(lstAvailableDeptartments.SelectedIndex);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            lstAvailableDeptartments.Items.Add(lstSelectedDepartments.SelectedItem.ToString());
            lstSelectedDepartments.Items.RemoveAt(lstSelectedDepartments.SelectedIndex);
        }
    }
}
