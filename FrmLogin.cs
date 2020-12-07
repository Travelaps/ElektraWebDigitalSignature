using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Linq;

namespace DemoButtons
{
    public partial class FrmLogin : Form
    {
        public FrmLogin()
        {
            InitializeComponent();
        }
        public bool t { get; set; }
        public string Tenant, user, password, deviceId; 
        string CREDENTIALSPATH = Environment.CurrentDirectory + @"\Credentials.txt";
        private void FrmLogin_Shown(object sender, EventArgs e)
        {
            AutoLogin();
        }
        public void AutoLogin()
        {
            if (File.Exists(CREDENTIALSPATH))
            {
                var lines = System.IO.File.ReadAllText(CREDENTIALSPATH).Split('\n');
                txtHotelCode.Text = (lines.FirstOrDefault() ?? "").Replace("\n", "").Replace("\r", "").Replace(" ", "");
                txtUserCode.Text = (lines.Skip(1).FirstOrDefault() ?? "").Replace("\n", "").Replace("\r", "").Replace(" ", "");
                txtPassword.Text = (lines.Skip(2).FirstOrDefault() ?? "").Replace("\n", "").Replace("\r", "").Replace(" ", "");
                deviceId = (lines.Skip(3).FirstOrDefault() ?? "").Replace("\n", "").Replace("\r", "").Replace(" ", "");

                t = false;
                Tenant = txtHotelCode.Text;
                user = txtUserCode.Text;
                password = txtPassword.Text;

                btnLogin.PerformClick();
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            t = false;
            Tenant = txtHotelCode.Text;
            user = txtUserCode.Text;
            password = txtPassword.Text;
            

            FrmMain frm = new FrmMain();
            // t = true;
            // this.Hide();
            // return;
            if (!frm.login(Tenant, user, password))
            {
                MessageBox.Show("Login bilgileri hatalı");
                return;
            }
            else
            {
                t = true;
                this.Hide();
            }
        }
    }
}
