namespace CNC
{
    partial class formTest
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
            this.cmdEnvoyerSignal = new System.Windows.Forms.Button();
            this.tBFreq = new System.Windows.Forms.TextBox();
            this.cmdActiverRelai = new System.Windows.Forms.Button();
            this.cmdEnable = new System.Windows.Forms.Button();
            this.cmdSens = new System.Windows.Forms.Button();
            this.cBAxeX = new System.Windows.Forms.CheckBox();
            this.cBAxeY = new System.Windows.Forms.CheckBox();
            this.tBNbrPas = new System.Windows.Forms.TextBox();
            this.cmdNbrPas = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // cmdEnvoyerSignal
            // 
            this.cmdEnvoyerSignal.Location = new System.Drawing.Point(111, 24);
            this.cmdEnvoyerSignal.Name = "cmdEnvoyerSignal";
            this.cmdEnvoyerSignal.Size = new System.Drawing.Size(127, 23);
            this.cmdEnvoyerSignal.TabIndex = 0;
            this.cmdEnvoyerSignal.Text = "Envoyer signal";
            this.cmdEnvoyerSignal.UseVisualStyleBackColor = true;
            this.cmdEnvoyerSignal.Click += new System.EventHandler(this.e_cmdEnvoyerSignal_Click);
            // 
            // tBFreq
            // 
            this.tBFreq.Location = new System.Drawing.Point(12, 26);
            this.tBFreq.Name = "tBFreq";
            this.tBFreq.Size = new System.Drawing.Size(76, 20);
            this.tBFreq.TabIndex = 1;
            this.tBFreq.Text = "1000";
            // 
            // cmdActiverRelai
            // 
            this.cmdActiverRelai.Location = new System.Drawing.Point(111, 74);
            this.cmdActiverRelai.Name = "cmdActiverRelai";
            this.cmdActiverRelai.Size = new System.Drawing.Size(127, 23);
            this.cmdActiverRelai.TabIndex = 2;
            this.cmdActiverRelai.Text = "Activer Relai";
            this.cmdActiverRelai.UseVisualStyleBackColor = true;
            this.cmdActiverRelai.Click += new System.EventHandler(this.e_cmdActiverRelai_Click);
            // 
            // cmdEnable
            // 
            this.cmdEnable.Location = new System.Drawing.Point(111, 125);
            this.cmdEnable.Name = "cmdEnable";
            this.cmdEnable.Size = new System.Drawing.Size(127, 23);
            this.cmdEnable.TabIndex = 3;
            this.cmdEnable.Text = "Enable";
            this.cmdEnable.UseVisualStyleBackColor = true;
            this.cmdEnable.Click += new System.EventHandler(this.e_cmdEnable_Click);
            // 
            // cmdSens
            // 
            this.cmdSens.Location = new System.Drawing.Point(111, 174);
            this.cmdSens.Name = "cmdSens";
            this.cmdSens.Size = new System.Drawing.Size(127, 23);
            this.cmdSens.TabIndex = 4;
            this.cmdSens.Text = "Sens";
            this.cmdSens.UseVisualStyleBackColor = true;
            this.cmdSens.Click += new System.EventHandler(this.e_cmdSens_Click);
            // 
            // cBAxeX
            // 
            this.cBAxeX.AutoSize = true;
            this.cBAxeX.Location = new System.Drawing.Point(12, 164);
            this.cBAxeX.Name = "cBAxeX";
            this.cBAxeX.Size = new System.Drawing.Size(51, 17);
            this.cBAxeX.TabIndex = 5;
            this.cBAxeX.Text = "AxeX";
            this.cBAxeX.UseVisualStyleBackColor = true;
            // 
            // cBAxeY
            // 
            this.cBAxeY.AutoSize = true;
            this.cBAxeY.Location = new System.Drawing.Point(12, 187);
            this.cBAxeY.Name = "cBAxeY";
            this.cBAxeY.Size = new System.Drawing.Size(51, 17);
            this.cBAxeY.TabIndex = 6;
            this.cBAxeY.Text = "AxeY";
            this.cBAxeY.UseVisualStyleBackColor = true;
            // 
            // tBNbrPas
            // 
            this.tBNbrPas.Location = new System.Drawing.Point(119, 229);
            this.tBNbrPas.Name = "tBNbrPas";
            this.tBNbrPas.Size = new System.Drawing.Size(119, 20);
            this.tBNbrPas.TabIndex = 7;
            // 
            // cmdNbrPas
            // 
            this.cmdNbrPas.Location = new System.Drawing.Point(12, 227);
            this.cmdNbrPas.Name = "cmdNbrPas";
            this.cmdNbrPas.Size = new System.Drawing.Size(101, 23);
            this.cmdNbrPas.TabIndex = 8;
            this.cmdNbrPas.Text = "Nbr Pas Executés";
            this.cmdNbrPas.UseVisualStyleBackColor = true;
            this.cmdNbrPas.Click += new System.EventHandler(this.e_cmdNbrPas_Click);
            // 
            // formTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(258, 269);
            this.Controls.Add(this.cmdNbrPas);
            this.Controls.Add(this.tBNbrPas);
            this.Controls.Add(this.cBAxeY);
            this.Controls.Add(this.cBAxeX);
            this.Controls.Add(this.cmdSens);
            this.Controls.Add(this.cmdEnable);
            this.Controls.Add(this.cmdActiverRelai);
            this.Controls.Add(this.tBFreq);
            this.Controls.Add(this.cmdEnvoyerSignal);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "formTest";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "formTest";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cmdEnvoyerSignal;
        private System.Windows.Forms.TextBox tBFreq;
        private System.Windows.Forms.Button cmdActiverRelai;
        private System.Windows.Forms.Button cmdEnable;
        private System.Windows.Forms.Button cmdSens;
        private System.Windows.Forms.CheckBox cBAxeX;
        private System.Windows.Forms.CheckBox cBAxeY;
        private System.Windows.Forms.TextBox tBNbrPas;
        private System.Windows.Forms.Button cmdNbrPas;
    }
}