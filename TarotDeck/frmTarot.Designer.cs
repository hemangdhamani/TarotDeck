namespace TarotCardsDeck
{
    partial class frmTarot
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
            this.pcbCard01 = new System.Windows.Forms.PictureBox();
            this.pcbCard02 = new System.Windows.Forms.PictureBox();
            this.pcbCard03 = new System.Windows.Forms.PictureBox();
            this.btnDrawCards = new System.Windows.Forms.Button();
            this.btnShufCards = new System.Windows.Forms.Button();
            this.flpMain = new System.Windows.Forms.FlowLayoutPanel();
            this.flpCardFirst = new System.Windows.Forms.FlowLayoutPanel();
            this.pcbCardFirst = new System.Windows.Forms.PictureBox();
            this.flpGoBack = new System.Windows.Forms.FlowLayoutPanel();
            this.btnGoBack = new System.Windows.Forms.Button();
            this.flpCard01 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.flpCard02 = new System.Windows.Forms.FlowLayoutPanel();
            this.flpCard03 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flpGoFowd = new System.Windows.Forms.FlowLayoutPanel();
            this.btnGoFowd = new System.Windows.Forms.Button();
            this.flpCardLast = new System.Windows.Forms.FlowLayoutPanel();
            this.pcbCardLast = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pcbCard01)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcbCard02)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcbCard03)).BeginInit();
            this.flpMain.SuspendLayout();
            this.flpCardFirst.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pcbCardFirst)).BeginInit();
            this.flpGoBack.SuspendLayout();
            this.flpCard01.SuspendLayout();
            this.flowLayoutPanel3.SuspendLayout();
            this.flpCard02.SuspendLayout();
            this.flpCard03.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flpGoFowd.SuspendLayout();
            this.flpCardLast.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pcbCardLast)).BeginInit();
            this.SuspendLayout();
            // 
            // pcbCard01
            // 
            this.pcbCard01.Location = new System.Drawing.Point(33, 3);
            this.pcbCard01.Name = "pcbCard01";
            this.pcbCard01.Size = new System.Drawing.Size(146, 250);
            this.pcbCard01.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcbCard01.TabIndex = 0;
            this.pcbCard01.TabStop = false;
            // 
            // pcbCard02
            // 
            this.pcbCard02.Location = new System.Drawing.Point(33, 3);
            this.pcbCard02.Name = "pcbCard02";
            this.pcbCard02.Size = new System.Drawing.Size(146, 250);
            this.pcbCard02.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcbCard02.TabIndex = 1;
            this.pcbCard02.TabStop = false;
            // 
            // pcbCard03
            // 
            this.pcbCard03.Location = new System.Drawing.Point(33, 3);
            this.pcbCard03.Name = "pcbCard03";
            this.pcbCard03.Size = new System.Drawing.Size(146, 250);
            this.pcbCard03.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcbCard03.TabIndex = 2;
            this.pcbCard03.TabStop = false;
            // 
            // btnDrawCards
            // 
            this.btnDrawCards.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDrawCards.Location = new System.Drawing.Point(23, 13);
            this.btnDrawCards.Name = "btnDrawCards";
            this.btnDrawCards.Size = new System.Drawing.Size(89, 23);
            this.btnDrawCards.TabIndex = 3;
            this.btnDrawCards.Text = "Draw Cards";
            this.btnDrawCards.UseVisualStyleBackColor = true;
            this.btnDrawCards.Click += new System.EventHandler(this.btnDrawCards_Click);
            // 
            // btnShufCards
            // 
            this.btnShufCards.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnShufCards.Location = new System.Drawing.Point(29, 13);
            this.btnShufCards.Name = "btnShufCards";
            this.btnShufCards.Size = new System.Drawing.Size(89, 23);
            this.btnShufCards.TabIndex = 4;
            this.btnShufCards.Text = "Shuffle Cards";
            this.btnShufCards.UseVisualStyleBackColor = true;
            this.btnShufCards.Click += new System.EventHandler(this.btnShufCards_Click);
            // 
            // flpMain
            // 
            this.flpMain.AutoSize = true;
            this.flpMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpMain.Controls.Add(this.flpCardFirst);
            this.flpMain.Controls.Add(this.flpGoBack);
            this.flpMain.Controls.Add(this.flpCard01);
            this.flpMain.Controls.Add(this.flpCard02);
            this.flpMain.Controls.Add(this.flpCard03);
            this.flpMain.Controls.Add(this.flpGoFowd);
            this.flpMain.Controls.Add(this.flpCardLast);
            this.flpMain.Location = new System.Drawing.Point(117, 477);
            this.flpMain.Margin = new System.Windows.Forms.Padding(0);
            this.flpMain.Name = "flpMain";
            this.flpMain.Size = new System.Drawing.Size(1351, 317);
            this.flpMain.TabIndex = 5;
            // 
            // flpCardFirst
            // 
            this.flpCardFirst.AutoSize = true;
            this.flpCardFirst.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpCardFirst.Controls.Add(this.pcbCardFirst);
            this.flpCardFirst.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpCardFirst.Location = new System.Drawing.Point(3, 3);
            this.flpCardFirst.Name = "flpCardFirst";
            this.flpCardFirst.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.flpCardFirst.Size = new System.Drawing.Size(241, 311);
            this.flpCardFirst.TabIndex = 9;
            // 
            // pcbCardFirst
            // 
            this.pcbCardFirst.Location = new System.Drawing.Point(33, 3);
            this.pcbCardFirst.Name = "pcbCardFirst";
            this.pcbCardFirst.Size = new System.Drawing.Size(175, 300);
            this.pcbCardFirst.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcbCardFirst.TabIndex = 0;
            this.pcbCardFirst.TabStop = false;
            // 
            // flpGoBack
            // 
            this.flpGoBack.AutoSize = true;
            this.flpGoBack.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpGoBack.Controls.Add(this.btnGoBack);
            this.flpGoBack.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpGoBack.FlowDirection = System.Windows.Forms.FlowDirection.BottomUp;
            this.flpGoBack.Location = new System.Drawing.Point(250, 3);
            this.flpGoBack.Name = "flpGoBack";
            this.flpGoBack.Padding = new System.Windows.Forms.Padding(0, 0, 0, 13);
            this.flpGoBack.Size = new System.Drawing.Size(95, 311);
            this.flpGoBack.TabIndex = 8;
            // 
            // btnGoBack
            // 
            this.btnGoBack.Enabled = false;
            this.btnGoBack.Location = new System.Drawing.Point(3, 272);
            this.btnGoBack.Name = "btnGoBack";
            this.btnGoBack.Size = new System.Drawing.Size(89, 23);
            this.btnGoBack.TabIndex = 6;
            this.btnGoBack.Text = "<<<";
            this.btnGoBack.UseVisualStyleBackColor = true;
            this.btnGoBack.Click += new System.EventHandler(this.btnGoBack_Click);
            // 
            // flpCard01
            // 
            this.flpCard01.AutoSize = true;
            this.flpCard01.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpCard01.Controls.Add(this.pcbCard01);
            this.flpCard01.Controls.Add(this.flowLayoutPanel3);
            this.flpCard01.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpCard01.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpCard01.Location = new System.Drawing.Point(351, 3);
            this.flpCard01.Name = "flpCard01";
            this.flpCard01.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.flpCard01.Size = new System.Drawing.Size(213, 311);
            this.flpCard01.TabIndex = 6;
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoSize = true;
            this.flowLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel3.Controls.Add(this.btnShufCards);
            this.flowLayoutPanel3.Location = new System.Drawing.Point(33, 259);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Padding = new System.Windows.Forms.Padding(26, 10, 26, 10);
            this.flowLayoutPanel3.Size = new System.Drawing.Size(147, 49);
            this.flowLayoutPanel3.TabIndex = 9;
            // 
            // flpCard02
            // 
            this.flpCard02.AutoSize = true;
            this.flpCard02.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpCard02.Controls.Add(this.pcbCard02);
            this.flpCard02.Location = new System.Drawing.Point(570, 3);
            this.flpCard02.Name = "flpCard02";
            this.flpCard02.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.flpCard02.Size = new System.Drawing.Size(212, 256);
            this.flpCard02.TabIndex = 7;
            // 
            // flpCard03
            // 
            this.flpCard03.AutoSize = true;
            this.flpCard03.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpCard03.Controls.Add(this.pcbCard03);
            this.flpCard03.Controls.Add(this.flowLayoutPanel1);
            this.flpCard03.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpCard03.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpCard03.Location = new System.Drawing.Point(788, 3);
            this.flpCard03.Name = "flpCard03";
            this.flpCard03.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.flpCard03.Size = new System.Drawing.Size(212, 311);
            this.flpCard03.TabIndex = 8;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.btnDrawCards);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(33, 259);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(20, 10, 26, 10);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(141, 49);
            this.flowLayoutPanel1.TabIndex = 7;
            // 
            // flpGoFowd
            // 
            this.flpGoFowd.AutoSize = true;
            this.flpGoFowd.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpGoFowd.Controls.Add(this.btnGoFowd);
            this.flpGoFowd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpGoFowd.FlowDirection = System.Windows.Forms.FlowDirection.BottomUp;
            this.flpGoFowd.Location = new System.Drawing.Point(1006, 3);
            this.flpGoFowd.Name = "flpGoFowd";
            this.flpGoFowd.Padding = new System.Windows.Forms.Padding(0, 0, 0, 13);
            this.flpGoFowd.Size = new System.Drawing.Size(95, 311);
            this.flpGoFowd.TabIndex = 9;
            // 
            // btnGoFowd
            // 
            this.btnGoFowd.Enabled = false;
            this.btnGoFowd.Location = new System.Drawing.Point(3, 272);
            this.btnGoFowd.Name = "btnGoFowd";
            this.btnGoFowd.Size = new System.Drawing.Size(89, 23);
            this.btnGoFowd.TabIndex = 6;
            this.btnGoFowd.Text = ">>>";
            this.btnGoFowd.UseVisualStyleBackColor = true;
            this.btnGoFowd.Click += new System.EventHandler(this.btnGoFowd_Click);
            // 
            // flpCardLast
            // 
            this.flpCardLast.AutoSize = true;
            this.flpCardLast.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpCardLast.Controls.Add(this.pcbCardLast);
            this.flpCardLast.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpCardLast.Location = new System.Drawing.Point(1107, 3);
            this.flpCardLast.Name = "flpCardLast";
            this.flpCardLast.Padding = new System.Windows.Forms.Padding(30, 0, 30, 0);
            this.flpCardLast.Size = new System.Drawing.Size(241, 311);
            this.flpCardLast.TabIndex = 8;
            // 
            // pcbCardLast
            // 
            this.pcbCardLast.Location = new System.Drawing.Point(33, 3);
            this.pcbCardLast.Name = "pcbCardLast";
            this.pcbCardLast.Size = new System.Drawing.Size(175, 300);
            this.pcbCardLast.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pcbCardLast.TabIndex = 0;
            this.pcbCardLast.TabStop = false;
            // 
            // frmTarot
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1584, 862);
            this.Controls.Add(this.flpMain);
            this.MaximizeBox = false;
            this.Name = "frmTarot";
            this.Text = "Tarot";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmTarot_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pcbCard01)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcbCard02)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pcbCard03)).EndInit();
            this.flpMain.ResumeLayout(false);
            this.flpMain.PerformLayout();
            this.flpCardFirst.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pcbCardFirst)).EndInit();
            this.flpGoBack.ResumeLayout(false);
            this.flpCard01.ResumeLayout(false);
            this.flpCard01.PerformLayout();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.flpCard02.ResumeLayout(false);
            this.flpCard03.ResumeLayout(false);
            this.flpCard03.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flpGoFowd.ResumeLayout(false);
            this.flpCardLast.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pcbCardLast)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pcbCard01;
        private System.Windows.Forms.PictureBox pcbCard02;
        private System.Windows.Forms.PictureBox pcbCard03;
        private System.Windows.Forms.Button btnDrawCards;
        private System.Windows.Forms.Button btnShufCards;
        private System.Windows.Forms.FlowLayoutPanel flpMain;
        private System.Windows.Forms.FlowLayoutPanel flpCard01;
        private System.Windows.Forms.FlowLayoutPanel flpCard02;
        private System.Windows.Forms.FlowLayoutPanel flpCard03;
        private System.Windows.Forms.FlowLayoutPanel flpCardLast;
        private System.Windows.Forms.PictureBox pcbCardLast;
        private System.Windows.Forms.FlowLayoutPanel flpGoBack;
        private System.Windows.Forms.Button btnGoBack;
        private System.Windows.Forms.FlowLayoutPanel flpGoFowd;
        private System.Windows.Forms.Button btnGoFowd;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flpCardFirst;
        private System.Windows.Forms.PictureBox pcbCardFirst;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
    }
}

