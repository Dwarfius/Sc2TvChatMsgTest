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
using System.Timers;
using System.Collections.ObjectModel;

namespace Sc2TvChatMsgTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string baseUrl = "http://sc2tv.ru/";
        const string gate = "http://chat.sc2tv.ru/gate.php";
        const string chatTargetUri = "http://chat.sc2tv.ru/memfs/channel-157781.json";

        CookieCollection cookies;
        UserData userdata;
        int lastParsedId = 0;
        WebClient client = new WebClient();
        ObservableCollection<Rule> rules = new ObservableCollection<Rule>();
        Timer checkTimer;

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("Rules.txt"))
            {
                string[] lines = File.ReadAllLines("Rules.txt");
                foreach (string line in lines)
                {
                    string[] splitLine = line.Split('\t');
                    rules.Add(new Rule() { Command = splitLine[0], Reply = splitLine[1] });
                }
            }
            rulesGrid.ItemsSource = rules;

            cookies = App.Current.Properties["Cookies"] as CookieCollection;
            userdata = App.Current.Properties["UserData"] as UserData;

            loginBlock.Inlines.Add(new Bold(new Run(userdata.name)));

            checkTimer = new Timer(500f);
            checkTimer.AutoReset = true;
            checkTimer.Elapsed += OnTimedEvent;
        }
        
        //Checking for new Messages
        void OnTimedEvent(Object source, ElapsedEventArgs args)
        {
            if (client.IsBusy)
                return; //stopping early, since won't be able to use it anyway

            string s;
            try
            {
                s = client.DownloadString(chatTargetUri);
            }
            catch { return; } //can't do anything without a new snapshot

            List<Message> msgToParse = new List<Message>();
            JsonChatRecords newChat = JsonConvert.DeserializeObject<JsonChatRecords>(s);
            if (lastParsedId == 0)
            {
                lastParsedId = newChat.messages[0].GetId();
                return; //ignoring previous messages to avoid spam
            }

            for (int i = 0; i < newChat.messages.Count && newChat.messages[i].GetId() > lastParsedId; i++)
                msgToParse.Add(newChat.messages[i]);

            for (int i = msgToParse.Count - 1; i >= 0; i--) //doing it reverse since I keep track of lastAnsweredId
                ParseMsg(msgToParse[i]);

            if(msgToParse.Count > 0)
                lastParsedId = msgToParse[0].GetId();
        }

        void ParseMsg(Message msg)
        {
            if (msg.message[0] == '#') //if it's a command
            {
                string[] messageSplit = msg.message.Split(' '); //break it, might contain arguments
                foreach (Rule rule in rules)
                {
                    string[] commandSplit = rule.Command.Split(' ');
                    if (messageSplit[0] == commandSplit[0] && messageSplit.Length == commandSplit.Length) //found the matching command, which has enough arguments
                    {
                        string reply = rule.GetPreParsedReply(msg.name); //getting a reply which already has #Name and #Time replaced

                        for (int i = 1; i < commandSplit.Length; i++) //if it has parameters - use them
                            reply = reply.Replace(commandSplit[i], messageSplit[i]); //replace the variable in reply to a value received with command

                        Send(reply);
                        return;
                    }
                }
            }
        }

        void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            while (rulesGrid.SelectedItems.Count > 0)
                rules.Remove((Rule)rulesGrid.SelectedItems[0]);
        }

        void Send(string message)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(gate);
            request.Method = "POST";
            request.AllowAutoRedirect = false;
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies["SESS4a29996287c6a61196a9cfc443f0fdb3"]);

            request.ContentType = "application/x-www-form-urlencoded";
            string msg = message.Replace(' ', '+');
            string data = "task=WriteMessage&message=" + msg + "&channel_id=157781&token=" + userdata.token;
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
                MessageBox.Show(userdata.error);
        }

        void toggleStartBtn_Click(object sender, RoutedEventArgs e)
        {
            checkTimer.Enabled = !checkTimer.Enabled;
            toggleStartBtn.Content = checkTimer.Enabled ? "Stop" : "Start";
            rulesGrid.IsEnabled = !checkTimer.Enabled;
        }

        void logoutBtn_Click(object sender, RoutedEventArgs e)
        {
            File.Delete("Credentials.txt");
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }

        void saveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (rules.Count == 0)
            {
                File.Delete("Rules.txt");
                return;
            }

            string s = "";
            foreach (Rule rule in rules)
                s += rule.Command + '\t' + rule.Reply + "\n";
            s.Substring(0, s.Length - 2); //removing the last \n symbol
            File.WriteAllText("Rules.txt", s);
        }

        void deleteAllBtn_Click(object sender, RoutedEventArgs e)
        {
            rules.Clear();
            File.Delete("Rules.txt");
        }

        void infoBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This program uses a logged user as a chat bot, and sends replies to commands red from chat.\n\n" + 
                            "On your left is a datagrid, which determines on which commands should it reply, and what to reply. " + 
                            "Command is what triggers the responce from the bot. It must start with a #. " +
                            "Reply contains a string which is sent back to a requested command. \n" + 
                            "This bot has basic variable support, meaning that you can add arguments to commands:\n" + 
                            "\tCommand: #throw #at\n" +
                            "\tReply: #at get's a snowball to the face.\n" + 
                            "When a user writes in chat \"#throw Dog\" the bot replies with \"Dog get's a snowball to the face\".\n\n" +
                            "At the moment there are 2 predefined variables: #Name and #Time, where #Name is the name of the command sender" + 
                            "and #Time is the current local time. \n" + 
                            "Note: using #Name and #Time as command argument names won't lead to desired results, since they'll be swapped to built-in values.");
        }
    }
}
