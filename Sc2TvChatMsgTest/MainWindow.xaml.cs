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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.IO;
using System.Collections;
using Newtonsoft.Json;

namespace Sc2TvChatMsgTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string baseUrl = "http://sc2tv.ru/";
        const string gate = "http://chat.sc2tv.ru/gate.php";
        const string referrer = "http://sc2tv.ru/channel/czt";

        CookieCollection cookies;
        UserData userdata;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            //getting basic cookies and fetching form for login
            HttpWebRequest baseReq = (HttpWebRequest)WebRequest.Create(baseUrl);
            baseReq.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:32.0) Gecko/20100101 Firefox/32.0";
            baseReq.CookieContainer = new CookieContainer();
            HttpWebResponse resp = (HttpWebResponse)baseReq.GetResponse();
            StreamReader reader = new StreamReader(resp.GetResponseStream());
            string data = reader.ReadToEnd();
            reader.Close();
            resp.Close();

            //getting a valid form id for login
            string searchStr = "<input type=\"hidden\" name=\"form_build_id\" id=\"";
            int ind = data.IndexOf(searchStr);
            string formId = data.Substring(ind + searchStr.Length, 37);
            
            //login - attempt to recieve cookies
            string name = nameBox.Text;
            string pass = passBox.Text;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(baseUrl + "all?destination=node");
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
            data = "name=" + name + "&pass=" + pass + "&op=%D0%92%D1%85%D0%BE%D0%B4&form_build_id=" + formId + "&form_id=user_login_block";
            byte[] dataBytes = Encoding.Default.GetBytes(data);
            req.ContentLength = dataBytes.Length;
            Stream stream = req.GetRequestStream();
            stream.Write(dataBytes, 0, dataBytes.Length);
            stream.Close();

            //looking for required cookie (SESS4a29996287c6a61196a9cfc443f0fdb3)
            resp = (HttpWebResponse)req.GetResponse();
            resp.Close();
            cookies = resp.Cookies;

            SendChatLogin();
        }

        void SendChatLogin()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(gate + "?task=GetUserInfo&ref=" + referrer);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies["SESS4a29996287c6a61196a9cfc443f0fdb3"]);
            WebResponse resp = request.GetResponse();
            StreamReader streamReader = new StreamReader(resp.GetResponseStream());
            string data = streamReader.ReadToEnd();
            streamReader.Close();
            resp.Close();
            userdata = JsonConvert.DeserializeObject<UserData>(data);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(gate);
            request.Method = "POST";
            request.AllowAutoRedirect = false;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies["SESS4a29996287c6a61196a9cfc443f0fdb3"]);

            request.ContentType = "application/x-www-form-urlencoded";
            string msg = msgBox.Text.Replace(' ', '+');
            string data = "task=WriteMessage&message=" + msg + "&channel_id=0&token=" + userdata.token;
            byte[] dataBytes = Encoding.Default.GetBytes(data);
            request.ContentLength = dataBytes.Length;
            Stream stream = request.GetRequestStream();
            stream.Write(dataBytes, 0, dataBytes.Length);
            stream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream());
            data = reader.ReadToEnd();
            reader.Close();
            response.Close();
            userdata = JsonConvert.DeserializeObject<UserData>(data);
            if (userdata.error != "")
                MessageBox.Show("Error");
        }
    }
}
