namespace CNO.BPA.ScanPop
{
    partial class CustomDepartmentEditor
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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.lblSelectedDepartments = new System.Windows.Forms.Label();
            this.lblAvailableDepartments = new System.Windows.Forms.Label();
            this.lstSelectedDepartments = new System.Windows.Forms.ListBox();
            this.lstAvailableDeptartments = new System.Windows.Forms.ListBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.btnCancel, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnOK, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(254, 269);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(165, 29);
            this.tableLayoutPanel1.TabIndex = 15;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(85, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(3, 3);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // lblSelectedDepartments
            // 
            this.lblSelectedDepartments.AutoSize = true;
            this.lblSelectedDepartments.Location = new System.Drawing.Point(244, 12);
            this.lblSelectedDepartments.Name = "lblSelectedDepartments";
            this.lblSelectedDepartments.Size = new System.Drawing.Size(112, 13);
            this.lblSelectedDepartments.TabIndex = 12;
            this.lblSelectedDepartments.Text = "Selected Departments";
            // 
            // lblAvailableDepartments
            // 
            this.lblAvailableDepartments.AutoSize = true;
            this.lblAvailableDepartments.Location = new System.Drawing.Point(17, 8);
            this.lblAvailableDepartments.Name = "lblAvailableDepartments";
            this.lblAvailableDepartments.Size = new System.Drawing.Size(113, 13);
            this.lblAvailableDepartments.TabIndex = 11;
            this.lblAvailableDepartments.Text = "Available Departments";
            // 
            // lstSelectedDepartments
            // 
            this.lstSelectedDepartments.FormattingEnabled = true;
            this.lstSelectedDepartments.Location = new System.Drawing.Point(242, 28);
            this.lstSelectedDepartments.Name = "lstSelectedDepartments";
            this.lstSelectedDepartments.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.lstSelectedDepartments.Size = new System.Drawing.Size(177, 225);
            this.lstSelectedDepartments.TabIndex = 4;
            // 
            // lstAvailableDeptartments
            // 
            this.lstAvailableDeptartments.FormattingEnabled = true;
            this.lstAvailableDeptartments.Location = new System.Drawing.Point(12, 28);
            this.lstAvailableDeptartments.Name = "lstAvailableDeptartments";
            this.lstAvailableDeptartments.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.lstAvailableDeptartments.Size = new System.Drawing.Size(177, 225);
            this.lstAvailableDeptartments.TabIndex = 1;
            // 
            // btnAdd
            // 
            this.btnAdd.Image = global::CNO.BPA.ScanPop.Properties.Resources.RightArrow;
            this.btnAdd.Location = new System.Drawing.Point(201, 65);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(35, 31);
            this.btnAdd.TabIndex = 2;
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Image = global::CNO.BPA.ScanPop.Properties.Resources.LeftArrow;
            this.btnRemove.Location = new System.Drawing.Point(201, 102);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(35, 31);
            this.btnRemove.TabIndex = 3;
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // CustomDepartmentEditor
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(431, 310);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.lblSelectedDepartments);
            this.Controls.Add(this.lblAvailableDepartments);
            this.Controls.Add(this.lstSelectedDepartments);
            this.Controls.Add(this.lstAvailableDeptartments);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.Name = "CustomDepartmentEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Department Editor";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Label lblSelectedDepartments;
        private System.Windows.Forms.Label lblAvailableDepartments;
        internal System.Windows.Forms.ListBox lstSelectedDepartments;
        internal System.Windows.Forms.ListBox lstAvailableDeptartments;
    }
}