namespace Dummy_Socket
{
    partial class frmSocket
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.serverPort = new System.Windows.Forms.NumericUpDown();
            this.serverLog = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.startServer = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.serverIP = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.clientPort = new System.Windows.Forms.NumericUpDown();
            this.clientMsg = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.sendMsg = new System.Windows.Forms.Button();
            this.clientName = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.receivedMsgs = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.clientLog = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.clientConnect = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.clientIP = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.serverPort)).BeginInit();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clientPort)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(460, 437);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.serverPort);
            this.tabPage1.Controls.Add(this.serverLog);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.startServer);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.serverIP);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(452, 411);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Servidor";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // serverPort
            // 
            this.serverPort.Location = new System.Drawing.Point(53, 32);
            this.serverPort.Maximum = new decimal(new int[] {
            65536,
            0,
            0,
            0});
            this.serverPort.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.serverPort.Name = "serverPort";
            this.serverPort.Size = new System.Drawing.Size(283, 20);
            this.serverPort.TabIndex = 7;
            this.serverPort.Value = new decimal(new int[] {
            30000,
            0,
            0,
            0});
            // 
            // serverLog
            // 
            this.serverLog.Location = new System.Drawing.Point(9, 77);
            this.serverLog.Multiline = true;
            this.serverLog.Name = "serverLog";
            this.serverLog.ReadOnly = true;
            this.serverLog.Size = new System.Drawing.Size(437, 328);
            this.serverLog.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 61);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Log del servidor:";
            // 
            // startServer
            // 
            this.startServer.Location = new System.Drawing.Point(342, 17);
            this.startServer.Name = "startServer";
            this.startServer.Size = new System.Drawing.Size(104, 23);
            this.startServer.TabIndex = 4;
            this.startServer.Text = "Arrancar servidor";
            this.startServer.UseVisualStyleBackColor = true;
            this.startServer.Click += new System.EventHandler(this.startServer_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(41, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Puerto:";
            // 
            // serverIP
            // 
            this.serverIP.Location = new System.Drawing.Point(53, 6);
            this.serverIP.Name = "serverIP";
            this.serverIP.Size = new System.Drawing.Size(283, 20);
            this.serverIP.TabIndex = 1;
            this.serverIP.Text = "127.0.0.1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(20, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "IP:";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.clientPort);
            this.tabPage2.Controls.Add(this.clientMsg);
            this.tabPage2.Controls.Add(this.label9);
            this.tabPage2.Controls.Add(this.sendMsg);
            this.tabPage2.Controls.Add(this.clientName);
            this.tabPage2.Controls.Add(this.label8);
            this.tabPage2.Controls.Add(this.receivedMsgs);
            this.tabPage2.Controls.Add(this.label7);
            this.tabPage2.Controls.Add(this.clientLog);
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.clientConnect);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.clientIP);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(452, 411);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Cliente";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // clientPort
            // 
            this.clientPort.Location = new System.Drawing.Point(62, 32);
            this.clientPort.Maximum = new decimal(new int[] {
            65536,
            0,
            0,
            0});
            this.clientPort.Minimum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.clientPort.Name = "clientPort";
            this.clientPort.Size = new System.Drawing.Size(274, 20);
            this.clientPort.TabIndex = 21;
            this.clientPort.Value = new decimal(new int[] {
            30000,
            0,
            0,
            0});
            // 
            // clientMsg
            // 
            this.clientMsg.Location = new System.Drawing.Point(62, 84);
            this.clientMsg.Name = "clientMsg";
            this.clientMsg.Size = new System.Drawing.Size(274, 20);
            this.clientMsg.TabIndex = 20;
            this.clientMsg.KeyDown += new System.Windows.Forms.KeyEventHandler(this.clientMsg_KeyDown);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(6, 87);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(50, 13);
            this.label9.TabIndex = 19;
            this.label9.Text = "Mensaje:";
            // 
            // sendMsg
            // 
            this.sendMsg.Location = new System.Drawing.Point(342, 82);
            this.sendMsg.Name = "sendMsg";
            this.sendMsg.Size = new System.Drawing.Size(104, 23);
            this.sendMsg.TabIndex = 18;
            this.sendMsg.Text = "Enviar mensaje";
            this.sendMsg.UseVisualStyleBackColor = true;
            this.sendMsg.Click += new System.EventHandler(this.sendMsg_Click);
            // 
            // clientName
            // 
            this.clientName.Location = new System.Drawing.Point(62, 58);
            this.clientName.Name = "clientName";
            this.clientName.Size = new System.Drawing.Size(274, 20);
            this.clientName.TabIndex = 17;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(9, 61);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(47, 13);
            this.label8.TabIndex = 16;
            this.label8.Text = "Nombre:";
            // 
            // receivedMsgs
            // 
            this.receivedMsgs.Location = new System.Drawing.Point(9, 129);
            this.receivedMsgs.Multiline = true;
            this.receivedMsgs.Name = "receivedMsgs";
            this.receivedMsgs.ReadOnly = true;
            this.receivedMsgs.Size = new System.Drawing.Size(437, 129);
            this.receivedMsgs.TabIndex = 15;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 113);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 13);
            this.label7.TabIndex = 14;
            this.label7.Text = "Mensajes recibidos:";
            // 
            // clientLog
            // 
            this.clientLog.Location = new System.Drawing.Point(9, 277);
            this.clientLog.Multiline = true;
            this.clientLog.Name = "clientLog";
            this.clientLog.ReadOnly = true;
            this.clientLog.Size = new System.Drawing.Size(437, 128);
            this.clientLog.TabIndex = 13;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 261);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(129, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Log de errores del cliente:";
            // 
            // clientConnect
            // 
            this.clientConnect.Location = new System.Drawing.Point(342, 30);
            this.clientConnect.Name = "clientConnect";
            this.clientConnect.Size = new System.Drawing.Size(104, 23);
            this.clientConnect.TabIndex = 11;
            this.clientConnect.Text = "Conectarse";
            this.clientConnect.UseVisualStyleBackColor = true;
            this.clientConnect.Click += new System.EventHandler(this.clientConnect_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(15, 35);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(41, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Puerto:";
            // 
            // clientIP
            // 
            this.clientIP.Location = new System.Drawing.Point(62, 6);
            this.clientIP.Name = "clientIP";
            this.clientIP.Size = new System.Drawing.Size(274, 20);
            this.clientIP.TabIndex = 8;
            this.clientIP.Text = "127.0.0.1";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(36, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(20, 13);
            this.label6.TabIndex = 7;
            this.label6.Text = "IP:";
            // 
            // frmSocket
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 461);
            this.Controls.Add(this.tabControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frmSocket";
            this.Text = "Dummy Sockets - Conexiones";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmSocket_Closing);
            this.Load += new System.EventHandler(this.frmSocket_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.frmSocket_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.frmSocket_KeyPress);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.serverPort)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clientPort)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox serverIP;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button startServer;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox serverLog;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox clientLog;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button clientConnect;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox clientIP;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox clientName;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox receivedMsgs;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button sendMsg;
        private System.Windows.Forms.TextBox clientMsg;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown serverPort;
        private System.Windows.Forms.NumericUpDown clientPort;
    }
}