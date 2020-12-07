/*
 DemoButtonsForm.cs

 Controlling program for the DemoButtons program which allow user to input a signature on an STU
 and reproduces it on a Window on the PC

 Copyright (c) 2015 Wacom GmbH. All rights reserved.

*/
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoButtons
{
    public partial class FrmMain : Form
    {
        private string loginToken;
        string HotelId;
       
        public bool Islogin { get; private set; }
        public static string _GuestName;

        private readonly string ENDPOINTURL = "https://4001.hoteladvisor.net";
        string startupPath = Environment.CurrentDirectory + @"\Credentials.txt";
        public string _User = "";
        public string _Password = "";
        public string PortNumber = "";
        static System.IO.MemoryStream ms;
        static Bitmap bitmap;
        public string DeviceId = "";

        public void print(string txt)
        {
            txtDisplay.Text += txt + "\r\n";
            txtDisplay.SelectionStart = txtDisplay.Text.Length; // scroll to end
            txtDisplay.ScrollToCaret();

        }

        public FrmMain()
        {
            InitializeComponent();
            radHID.Checked = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WacomConnect();  
        }
        public bool WacomConnect()
        {
            if (/*radHID.Checked*/true)
            {
               return captureSignatureHID();
            }
            else
            {
                return false;//  captureSignatureSerial();
            }
        }

        private bool captureSignatureHID()
        {
            print("Get device...");
            wgssSTU.SerialInterface serialInterface = new wgssSTU.SerialInterface();
            print("Get device 1 ...");
            wgssSTU.UsbDevices usbDevices = new wgssSTU.UsbDevices();


            print("Device Count: " + usbDevices.Count.ToString());
            if (usbDevices.Count != 0)
            {
                try
                {
                     
                    wgssSTU.IUsbDevice usbDevice = usbDevices[0]; // select a device
                    
                    SignatureForm demo = new SignatureForm(this, usbDevice, serialInterface, txtCOM.Text, txtBaudRate.Text, true,_GuestName);
                   
                    DialogResult res = demo.ShowDialog();
                    //print("SignatureForm returned: " + res.ToString());
                    if (res == DialogResult.OK)
                    {
                        DisplaySignature(demo);
                    }
                
                    demo.Dispose();
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message +" SOURCE: "+ex.Source+"  help link: "+ex.HelpLink );
                    return false;
                }
            }
            else
            {
                MessageBox.Show("No STU devices attached");
                return false;
            }
        }

        private void captureSignatureSerial()
        {
            int baudRate;
            string fileNameCOMPort;
            wgssSTU.IUsbDevice usbDevice;
            wgssSTU.SerialInterface serialInterface;

            usbDevice = null;
            serialInterface = new wgssSTU.SerialInterface();

            fileNameCOMPort = txtCOM.Text;
            baudRate = int.Parse(txtBaudRate.Text);

            try
            {
                SignatureForm demo = new SignatureForm(this, usbDevice, serialInterface, txtCOM.Text, txtBaudRate.Text, false);
                DialogResult res = demo.ShowDialog();
                print("SignatureForm returned: " + res.ToString());
                if (res == DialogResult.OK)
                {
                    DisplaySignature(demo);
                }
                demo.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No STU devices attached");
            }
        }

        private void radHID_CheckedChanged(object sender, EventArgs e)
        {
            radioSelectionCheck();
        }

        private void radSerial_CheckedChanged(object sender, EventArgs e)
        {
            radioSelectionCheck();
        }

        private void radioSelectionCheck()
        {
            if (radHID.Checked == true)
            {
                txtCOM.Enabled = false;
                txtBaudRate.Enabled = false;
            }
            else
            {
                txtCOM.Enabled = true;
                txtBaudRate.Enabled = true;
                txtBaudRate.Text = "128000";
                txtCOM.Focus();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void DisplaySignature(SignatureForm demo)
        {
            //Bitmap bitmap;
             
            bitmap = demo.GetSigImage();
            
            // resize the image to fit the screen if needed
            int scale = 1;       
            if (bitmap.Width > 400)
                scale = 4;
            pictureBox1.Size = new Size(bitmap.Width / scale, bitmap.Height / scale);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Image = bitmap;
            pictureBox1.Parent = this;
            //centre the image in the panel
            int x, y;
            x = panel1.Location.X + ((panel1.Width - pictureBox1.Width) / 2);
            y = panel1.Location.Y + ((panel1.Height - pictureBox1.Height) / 2);
            this.pictureBox1.Location = new Point(x, y);
            pictureBox1.BringToFront();
            ms = new System.IO.MemoryStream();
            Image _Img = (Image)bitmap.Clone();

            pictureBox1.Image = _Img;

            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
             
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        
        private void DemoButtonsForm_Load(object sender, EventArgs e)
        {
            this.Hide();
            FrmLogin frmLogin = new FrmLogin();
            frmLogin.ShowDialog();

            if (frmLogin.t == true)
            {
                login(frmLogin.Tenant, frmLogin.user, frmLogin.password);
                HotelId = frmLogin.Tenant;

                this.Show();

            }
            else
            {
                this.Close();
                Application.Exit();
                return;
            }
        }
        public string post(string requestBody)
        {
            
            string responseBody = null;

            if (ENDPOINTURL.StartsWith("https"))
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)768 | (SecurityProtocolType)192;
                //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;//TLS1.2;
                ServicePointManager.DefaultConnectionLimit = 9999;
                System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }

            /*
                May Throw Exception
             */
            var newRequest = WebRequest.Create(ENDPOINTURL);
            HttpWebRequest request = (HttpWebRequest)newRequest;
            //2017-08-07:OGUZ
            request.Host = ENDPOINTURL.Replace("https://", "").Replace("http://", "").Split('/')[0]; //.NET FRAMEWORK BUG, see https://referencesource.microsoft.com/#System/net/System/Net/HttpWebRequest.cs,15b3b0da6dac6d63
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.AllowAutoRedirect = true;
            request.MaximumAutomaticRedirections = Int32.MaxValue;
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Timeout = 10000;//Int32.MaxValue;
            request.ReadWriteTimeout = Int32.MaxValue;
            

            byte[] requestData = Encoding.UTF8.GetBytes(requestBody);
            var outStream = request.GetRequestStream();
            outStream.Write(requestData, 0, requestData.Length);
            outStream.Close();


            Encoding encode = System.Text.Encoding.GetEncoding("utf-8");

            //Try To Retrieve The Result
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                responseBody = new System.IO.StreamReader(response.GetResponseStream(), encode).ReadToEnd();
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return "401";
                }
                response.Close();
            }
            catch (WebException exc)
            {
                if (exc.Response==null)
                {
                    return null;
                }
                using (var response = exc.Response)
                using (var data = response.GetResponseStream())
                using (var reader = new System.IO.StreamReader(data))
                    responseBody = reader.ReadToEnd();
            }
            // MessageBox.Show("Rsponose body = " + responseBody);
            return responseBody;
        }


        public bool login(string _hotelid, string user, string password)
        {
            var loginResp = post(@"{ ""Action"":""Login"", ""Tenant"":""" + _hotelid + @""", ""Usercode"":""" + user + @""", ""Password"":""" + password + @""" }");
            var loginRespJson = JObject.Parse(loginResp);

            if ((bool)loginRespJson["Success"] == true)
            {
                loginToken = (string)loginRespJson["LoginToken"];
                HotelId = (_hotelid);
                Islogin = true;
                return true;
            }
            else
            {               
                return false;
            }
        }

        private void DemoButtonsForm_Shown(object sender, EventArgs e)
        {

            this.Hide();
            FrmLogin frmLogin = new FrmLogin();
            frmLogin.ShowDialog();

            if (frmLogin.t == true)
            {
                login(frmLogin.Tenant, frmLogin.user, frmLogin.password);
                _User = frmLogin.user;
                _Password = frmLogin.password;
                HotelId = frmLogin.Tenant;
                DeviceId = frmLogin.deviceId;

                this.Show();

            }
            else
            {
                this.Close();
                Application.Exit();
                return;
            }

            Task t = new Task(
                () =>
                {
                    ImzaTara();
                }

                );
            t.Start();

        }
        public void ImzaTara()
        {
            string Result = "";
            string WaitMode = "";
            string DeviceIdKosul = "";
            if (DeviceId != "")
            {
                DeviceIdKosul = @",{ ""Column"":""INTERFACENAME"", ""Operator"":""="" , ""Value"": " + @"""" + DeviceId + @"""" + @" }";
            }
            
            print("DeviceId: " + DeviceId);
            while (true)
            {
                
                print("Getting data please wait...");

                // Result= @"{""DataTypes"":null,""TotalCount"":1,""ResultSets"":[[{""ID"":887,""HOTELID"":18892,""INTERFACE_TYPE"":null,""INTERFACE_BRAND"":null,""CMDDATE"":""2020-05-19 11:27:09.207"",""RESID"":3621543,""ROOMNO"":null,""LOCKNO"":null,""GUESTNAME"":""Birsen Nane"",""CIN"":null,""COUT"":null,""KEYCOUNT"":null,""INTERFACENAME"":"""",""CMDTYPE"":""SIGNATURE"",""CMD"":null,""CMDSTATE"":null,""SUCCESS"":false,""RESPONSE"":null,""GUESTID"":5727368}]],""SQL"":""SELECT R.[ID] AS [ID],R.[HOTELID] AS [HOTELID],R.[INTERFACE_TYPE] AS [INTERFACE_TYPE],R.[INTERFACE_BRAND] AS [INTERFACE_BRAND],R.[CMDDATE] AS [CMDDATE],R.[RESID] AS [RESID],R.[ROOMNO] AS [ROOMNO],R.[LOCKNO] AS [LOCKNO],R.[GUESTNAME] AS [GUESTNAME],R.[CIN] AS [CIN],R.[COUT] AS [COUT],R.[KEYCOUNT] AS [KEYCOUNT],R.[INTERFACENAME] AS [INTERFACENAME],R.[CMDTYPE] AS [CMDTYPE],R.[CMD] AS [CMD],R.[CMDSTATE] AS [CMDSTATE],R.[SUCCESS] AS [SUCCESS],R.[RESPONSE] AS [RESPONSE],R.[GUESTID] AS [GUESTID] FROM [HOTEL_INTERFACE_CMD] as R WITH(NOLOCK) WHERE R.[HOTELID] = @p0 AND R.[SUCCESS] = @p1 AND R.[CMDTYPE] = @p2 ORDER BY R.[ID] ASC OFFSET @_OFFSET ROWS FETCH NEXT @_NEXT ROWS ONLY;SELECT COUNT(1) AS [COUNT] FROM [HOTEL_INTERFACE_CMD] as R WITH(NOLOCK) WHERE R.[HOTELID] = @p0 AND R.[SUCCESS] = @p1 AND R.[CMDTYPE] = @p2""}";

               

                // if (Result==null)
                if (WaitMode == "")
                { // İlk önce imzası atılmamış kayıtlar varsa onları getirsin. Daha sonra wait mode ile dinlemeye alsın.
                    Result = post(@"{
                          ""Action"":""Select"",
                          ""Object"":""HOTEL_INTERFACE_CMD"",
                          ""Select"":[""ID"",""HOTELID"",""INTERFACE_TYPE"",""INTERFACE_BRAND"",""CMDDATE"",""RESID"",""ROOMNO"",""LOCKNO"",""GUESTNAME"",""CIN"",""COUT"",""KEYCOUNT"",""INTERFACENAME"",""CMDTYPE"",""CMD"",""CMDSTATE"",""SUCCESS"",""RESPONSE"",""GUESTID""],                                  
                          ""Where"": [{ ""Column"":""HOTELID"", ""Operator"":""="" , ""Value"": " + @"""" + HotelId.ToString() + @"""" + @" },{ ""Column"":""SUCCESS"", ""Operator"":""="" , ""Value"": " + @"""" + 0.ToString() + @"""" + @" },{ ""Column"":""CMDTYPE"", ""Operator"":""="" , ""Value"": " + @"""" + "SIGNATURE" + @"""" + @" }" + DeviceIdKosul + @"],         
                          ""Paging"":{""Current"":1,""ItemsPerPage"":9999},
                          ""TotalCount"": true,                          
                         ""LoginToken"":""" + loginToken + @"""}");

                }
                else
                {
                    Result = post(@"{
                          ""Action"":""Select"",
                          ""Object"":""HOTEL_INTERFACE_CMD"",
                          ""Select"":[""ID"",""HOTELID"",""INTERFACE_TYPE"",""INTERFACE_BRAND"",""CMDDATE"",""RESID"",""ROOMNO"",""LOCKNO"",""GUESTNAME"",""CIN"",""COUT"",""KEYCOUNT"",""INTERFACENAME"",""CMDTYPE"",""CMD"",""CMDSTATE"",""SUCCESS"",""RESPONSE"",""GUESTID""],
                          ""Where"": [{ ""Column"":""HOTELID"", ""Operator"":""="" , ""Value"": " + @"""" + HotelId.ToString() + @"""" + @" },{ ""Column"":""SUCCESS"", ""Operator"":""="" , ""Value"": " + @"""" + 0.ToString() + @"""" + @" },{ ""Column"":""CMDTYPE"", ""Operator"":""="" , ""Value"": " + @"""" + "SIGNATURE" + @"""" + @" }" + DeviceIdKosul + @"],         
                          ""WaitMode"":""" + WaitMode + @""",                          
                          ""Paging"":{""Current"":1,""ItemsPerPage"":9999},
                          ""TotalCount"": true,                          
                         ""LoginToken"":""" + loginToken + @"""}");
                }
                WaitMode = "4";
                if (Result == null)
                {
                    continue;
                }
               bool LoginOl = (Result == "401") || (Result.Contains("not found") || Result.Contains("LoginToken")) || (Result.StartsWith("{") == false || Result.EndsWith("}") == false || (Result == "LoginToken is invalid"));
                if (LoginOl)
                {
                    login(HotelId, _User, _Password);
                    System.Threading.Thread.Sleep(1000);
                    Result = post(@"{
                          ""Action"":""Select"",
                          ""Object"":""HOTEL_INTERFACE_CMD"",
                          ""Select"":[""ID"",""HOTELID"",""INTERFACE_TYPE"",""INTERFACE_BRAND"",""CMDDATE"",""RESID"",""ROOMNO"",""LOCKNO"",""GUESTNAME"",""CIN"",""COUT"",""KEYCOUNT"",""INTERFACENAME"",""CMDTYPE"",""CMD"",""CMDSTATE"",""SUCCESS"",""RESPONSE"",""GUESTID""],
                          ""Where"": [{ ""Column"":""HOTELID"", ""Operator"":""="" , ""Value"": " + @"""" + HotelId.ToString() + @"""" + @" },{ ""Column"":""SUCCESS"", ""Operator"":""="" , ""Value"": " + @"""" + 0.ToString() + @"""" + @" },{ ""Column"":""CMDTYPE"", ""Operator"":""="" , ""Value"": " + @"""" + "SIGNATURE" + @"""" + @" }],         
                          ""WaitMode"":""" + WaitMode + @""",                          
                          ""Paging"":{""Current"":1,""ItemsPerPage"":9999},
                          ""TotalCount"": true,                          
                         ""LoginToken"":""" + loginToken + @"""}");

                }

                var obj = JObject.Parse(Result);
               
                int TotalCount = (int)obj["TotalCount"];

                string ID = "", Message = "OK",GuestName;
                if (TotalCount!=0)
                {
                    
                    this.Show();
                    string GuestId = ""; 
                    for (int i = 0; i < TotalCount; i++)
                    {
                        GuestName = obj["ResultSets"][0][i]["GUESTNAME"].ToString();
                       _GuestName = GuestName+" ("+ (i+1).ToString()+"/"+ TotalCount.ToString() + ") ";
                        GuestId = obj["ResultSets"][0][i]["GUESTID"].ToString();
                        print(GuestName + " kişisi için işlem yapılıyor...");
                        print("Please Wait Processing...");
                       var con =  WacomConnect();//GND 
                        if (con==false)
                        {
                            print("Bağlantı sağlanılamadı.");
                            return;
                        }
                        ID = obj["ResultSets"][0][i]["ID"].ToString();
                        if (ms!=null)
                        {
                            byte[] byteImage = ms.ToArray();
                            string UploadImage = Convert.ToBase64String(byteImage);
                            if (!SignatureControl(GuestId, UploadImage))
                            {
                                bool I = FileUpload(GuestId, UploadImage);
                                if (I)
                                {
                                    print(GuestName + " kişisi için imza yüklendi");
                                }
                            }
                            if (UploadImage != null)
                            {
                                ms.Close();
                                pictureBox1.Image = null;
                                bitmap = null;
                                Hotel_Interface_Cmd_Update(ID, 1.ToString(), HotelId, Message, loginToken);

                            }
                        }
                        else
                        {
                            Hotel_Interface_Cmd_Update(ID, 1.ToString(), HotelId, "Kullanıcı imza atmadı.", loginToken);
                             
                        }
                        
                            
                    }
                }
                else
                {
                    
                }
                
            }
                 
        }

        public bool FileUpload(string ResId,string File)
        {
            File = "data:image/png;base64," + File;
           string res = post(@"{
                           ""Action"":""Insert"",
                           ""Object"":""DOCARCHIVE"",
                           ""Row"":{""SOURCETABLE"":""" + "RES_NAME" + @""", ""SOURCETABLEID"": """ + ResId + @""",""HOTELID"": """ + HotelId + @""",""BINARYDATA"": """ + File + @""",""TITLE"": """ + "Self-Checkin Signature" + @"""},
                           ""SelectAfterInsert"": [""HOTELID""],                       
                           ""LoginToken"":""" + loginToken + @"""}"
             );
            var ParseJson = JObject.Parse(res);

            return (bool)ParseJson["Success"];

        }
        public bool SignatureControl(string Id,string File)
        {
            string Res = post(@"{
                          ""Action"":""Select"",
                          ""Object"":""DOCARCHIVE"",
                          ""Select"":[""ID"",""HOTELID"",""SOURCETABLEID""],                                  
                          ""Where"": [{ ""Column"":""HOTELID"", ""Operator"":""="" , ""Value"": " + @"""" + HotelId.ToString() + @"""" + @" },{ ""Column"":""SOURCETABLEID"", ""Operator"":""="" , ""Value"": " + @"""" + Id + @"""" + @" },{ ""Column"":""SOURCETABLE"", ""Operator"":""="" , ""Value"": " + @"""" + "RES_NAME" + @"""" + @" }],         
                          ""Paging"":{""Current"":1,""ItemsPerPage"":9999},
                          ""TotalCount"": true,                      
                         ""LoginToken"":""" + loginToken + @"""}");

            var ParseJson = JObject.Parse(Res);

            var Count = (int)ParseJson["TotalCount"];
            if (Count!=0)
            {
                string DocId = ParseJson["ResultSets"][0][0]["ID"].ToString();
                //
                File = "data:image/png;base64," + File;
                Res = post(@"{
                           ""Action"":""Update"",
                           ""Object"":""DOCARCHIVE"",
                           ""Row"":{""ID"":""" + DocId + @""", ""BINARYDATA"": """ + File + @""",""HOTELID"": """ + HotelId + @""",""TITLE"": """ + "Self-Checkin Signature" + @"""},
                           ""SelectAfterUpdate"": [""HOTELID""],                       
                           ""LoginToken"":""" + loginToken + @"""}"
            );
                ParseJson = JObject.Parse(Res);

                if ((bool)ParseJson["Success"])
                {
                    print(DocId + " nolu docid güncellendi.");
                    return true;
                }
                else
                {
                    print(DocId + " nolu docid güncellenirken hata çıktı. " + ParseJson["Message"].ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }

            
        }

        public string Hotel_Interface_Cmd_Update(string Id, string success, string HotelId, string message, string LoginToken)
        {
            return post(@"{
                           ""Action"":""Update"",
                           ""Object"":""HOTEL_INTERFACE_CMD"",
                           ""Row"":{""ID"":""" + Id + @""", ""SUCCESS"": """ + success + @""",""HOTELID"": """ + HotelId + @""",""RESPONSE"": """ + /*"Kart Hazırlandı"*/ message + @"""},
                           ""SelectAfterUpdate"": [""HOTELID""],                       
                           ""LoginToken"":""" + LoginToken + @"""}"
             );
        }
         
        public string ReadExeIni(string HOTELID, string PROGNAME, string SECTIONSTR, string KEYSTR = null, string DEFAULTVALUE = null /*string Section,string Key,string D*/)
        {

            string json = post(@"{
                ""Action"":""Execute"",
                ""Object"":""SP_READHOTELCONFIG"",                                                                                                                      
                ""Parameters"": { ""HOTELID"": """ + HOTELID + @""", ""PROGNAME"": """ + PROGNAME + @""",  ""SECTIONSTR"": """ + SECTIONSTR + @""", ""KEYSTR"": """ + KEYSTR + @""",  ""DEFAULTVALUE"": """ + DEFAULTVALUE + @""",  ""CURRENTVALUE"": """ + "" + @""" ,  ""SELECT"": """ + "1" + @""" }, 
                ""Paging"":{""Current"":1,""ItemsPerPage"":10},
                ""LoginToken"":""" + loginToken + @"""}");
            // MessageBox.Show(json);

            var obj = JArray.Parse(json);


            if (obj[0][0]["CURRENTVALUE"].ToString() == "")
            {
                return DEFAULTVALUE;
            }

            return (obj[0][0]["CURRENTVALUE"].ToString());
        }
    }
}
