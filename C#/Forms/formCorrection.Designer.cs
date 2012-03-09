namespace CNC
{
    partial class formCorrection
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
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tBX = new System.Windows.Forms.TextBox();
            this.tBY = new System.Windows.Forms.TextBox();
            this.cmdEnvoyer = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(94, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Valeur lue pour X :";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 37);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Valeur lue pour Y :";
            // 
            // tBX
            // 
            this.tBX.Location = new System.Drawing.Point(112, 6);
            this.tBX.Name = "tBX";
            this.tBX.Size = new System.Drawing.Size(100, 20);
            this.tBX.TabIndex = 2;
            // 
            // tBY
            // 
            this.tBY.Location = new System.Drawing.Point(112, 34);
            this.tBY.Name = "tBY";
            this.tBY.Size = new System.Drawing.Size(100, 20);
            this.tBY.TabIndex = 3;
            // 
            // cmdEnvoyer
            // 
            this.cmdEnvoyer.Location = new System.Drawing.Point(61, 100);
            this.cmdEnvoyer.Name = "cmdEnvoyer";
            this.cmdEnvoyer.Size = new System.Drawing.Size(102, 28);
            this.cmdEnvoyer.TabIndex = 4;
            this.cmdEnvoyer.Text = "Envoyer";
            this.cmdEnvoyer.UseVisualStyleBackColor = true;
            this.cmdEnvoyer.Click += new System.EventHandler(this.e_cmdEnvoyer_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(188, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Saisir les valeurs en entiers de millieme";
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(61, 103);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(19, 23);
            this.cmdCancel.TabIndex = 6;
            this.cmdCancel.Text = "button1";
            this.cmdCancel.UseVisualStyleBackColor = true;
            this.cmdCancel.Click += new System.EventHandler(this.e_cmdCancel_Click);
            // 
            // formCorrection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cmdCancel;
            this.ClientSize = new System.Drawing.Size(225, 142);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmdEnvoyer);
            this.Controls.Add(this.tBY);
            this.Controls.Add(this.tBX);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmdCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "formCorrection";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Correction";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tBX;
        private System.Windows.Forms.TextBox tBY;
        private System.Windows.Forms.Button cmdEnvoyer;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button cmdCancel;
    }
}