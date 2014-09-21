using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace Sc2TvChatMsgTest
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            if (File.Exists("Credentials.txt"))
            {
                string[] credentials = File.ReadAllLines("Credentials.txt");
                loginBox.Text = credentials[0];
                passBox.Text = credentials[1];
                saveBox.IsChecked = true;
                loginBtn_Click(null, null);
            }
        }

        void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string formId = GetFormId();
                WebsiteLogin(formId);
                ChatLogin();
            }
            finally
            {
                if (App.Current.Properties["UserData"] != null)
                {
                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                    if ((bool)saveBox.IsChecked)
                        File.WriteAllText("Credentials.txt", loginBox.Text + "\n" + passBox.Text);
                    else
                        File.Delete("Credentials.txt");
                }
                else
                    MessageBox.Show("Login failed, try again");
            }
        }

        string GetFormId()
        {
            HttpWebRequest baseReq = (HttpWebRequest)WebRequest.Create("http://sc2tv.ru");
            baseReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:32.0) Gecko/20100101 Firefox/32.0"; //hiding ourself, just in case

            HttpWebResponse resp = (HttpWebResponse)baseReq.GetResponse();
            StreamReader reader = new StreamReader(resp.GetResponseStream());
            string data = reader.ReadToEnd();
            reader.Close();
            resp.Close();

            //getting a valid form id for login
            string searchStr = "<input type=\"hidden\" name=\"form_build_id\" id=\"";
            int ind = data.IndexOf(searchStr);
            return data.Substring(ind + searchStr.Length, 37); //37 - length of the formId
        }

        void WebsiteLogin(string formId)
        {
            //login - attempt to recieve cookies
            string name = loginBox.Text;
            string pass = passBox.Text;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://sc2tv.ru/all?destination=node");
            req.AllowAutoRedirect = false; //this is required to catch the response containing cookies, since the request is redirected to the main afterwards
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:32.0) Gecko/20100101 Firefox/32.0";
            req.KeepAlive = true;
            req.Headers = new WebHeaderCollection();
            req.Headers.Add("Accept-Language", "en-US,en;q=0.5");
            req.Headers.Add("Accept-Encoding", "gzip, deflate");
            req.Referer = "http://sc2tv.ru/";
            req.Method = "POST";
            req.CookieContainer = new CookieContainer();

            //adding request post form-data
            req.ContentType = "application/x-www-form-urlencoded";
            string data = "name=" + name + "&pass=" + pass + "&op=%D0%92%D1%85%D0%BE%D0%B4&form_build_id=" + formId + "&form_id=user_login_block";
            byte[] dataBytes = Encoding.Default.GetBytes(data);
            req.ContentLength = dataBytes.Length;
            Stream stream = req.GetRequestStream();
            stream.Write(dataBytes, 0, dataBytes.Length);
            stream.Close();

            //looking for required cookie (SESS4a29996287c6a61196a9cfc443f0fdb3)
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            resp.Close();

            App.Current.Properties["Cookies"] = resp.Cookies;
        }

        void ChatLogin()
        {
            CookieCollection cookies = App.Current.Properties["Cookies"] as CookieCollection;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://chat.sc2tv.ru/gate.php?task=GetUserInfo&ref=http://sc2tv.ru/channel/czt");
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies["SESS4a29996287c6a61196a9cfc443f0fdb3"]);

            WebResponse resp = request.GetResponse();
            StreamReader streamReader = new StreamReader(resp.GetResponseStream());
            string data = streamReader.ReadToEnd();
            streamReader.Close();
            resp.Close();

            App.Current.Properties["UserData"] = JsonConvert.DeserializeObject<UserData>(data);
        }
    }
}
