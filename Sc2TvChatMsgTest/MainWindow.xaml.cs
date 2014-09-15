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

        List<Cookie> cookies = new List<Cookie>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void sendBtn_Click(object sender, RoutedEventArgs e)
        {
            WebRequest request = WebRequest.Create(gate + "?task=GetUserInfo&ref=" + referrer);
            string[] cookies = cookiesBox.Text.Replace(" ", "").Split(';');
            (request as HttpWebRequest).CookieContainer = new CookieContainer();
            foreach (string cookie in cookies)
            {
                string[] cookieParts = cookie.Split('=');
                Cookie c = new Cookie(cookieParts[0], cookieParts[1]) { Domain = "chat.sc2tv.ru" }; 
                (request as HttpWebRequest).CookieContainer.Add(c);
            }
            WebResponse resp = request.GetResponse();
            StreamReader streamReader = new StreamReader(resp.GetResponseStream());
            string data = streamReader.ReadToEnd();
            streamReader.Close();
            resp.Close();
            MessageBox.Show(data);
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
            string cookiesStr = resp.Headers["Set-Cookie"];


        }
    }
}
