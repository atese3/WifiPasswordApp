using Microsoft.Expression.Interactivity.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WifiPasswordApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker bgw;
        public bool Visible
        {
            get;
            set;
        }
        public Dictionary<string, string> Wifis
        {
            get;
            set;
        }
        public MainWindow()
        {
            InitializeComponent();
            Wifis = new Dictionary<string, string>();
            this.DataContext = this;

        }

        private List<string> GetWifiNames()
        {
            // netsh wlan show profile
            Process processWifi = new Process();
            processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processWifi.StartInfo.FileName = "netsh";
            processWifi.StartInfo.Arguments = "wlan show profile";
            //processWifi.StartInfo.WorkingDirectory = Path.GetDirectoryName(YourApplicationPath);

            processWifi.StartInfo.UseShellExecute = false;
            processWifi.StartInfo.RedirectStandardError = true;
            processWifi.StartInfo.RedirectStandardInput = true;
            processWifi.StartInfo.RedirectStandardOutput = true;
            processWifi.StartInfo.CreateNoWindow = true;
            processWifi.Start();
            //* Read the output (or the error)
            string output = processWifi.StandardOutput.ReadToEnd();
            // Show output commands
            string err = processWifi.StandardError.ReadToEnd();
            // show error commands
            processWifi.WaitForExit();
            //output = output.Replace("    <None>\r\n\r\nUser profiles\r\n-------------\r\n    All User Profile     : ", "").Replace("\r", "").Replace("\n", "").Replace("<None>", "").Replace("Profiles on interface Wi-Fi 2:Group policy profiles (read only)---------------------------------", "").Replace("    All User Profile     : ", ";");
            string output2 = string.Empty;

            using (StringReader reader = new StringReader(output))
            {
                // Loop over the lines in the string.
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regex1 = new Regex(@"All User Profile * : (?<after>.*)"); // Wifi Names
                    Match match1 = regex1.Match(line); // Wifi Names
                    output2 += match1.ToString().Replace("All User Profile     : ", ";");
                }
            }
            return output2.Remove(0, 1).Split(';').ToList();
        }

        private string GetWifiPassword(string wifiname)
        {
            string get_password = wifipassword(wifiname); // Get the chunk from console that returns the wifi password           
            using (StringReader reader = new StringReader(get_password))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regex2 = new Regex(@"Key Content * : (?<after>.*)"); // Passwords
                    Match match2 = regex2.Match(line);

                    if (match2.Success)
                    {
                        string current_password = match2.Groups["after"].Value;
                        return current_password;
                    }
                }
            }
            return "Open Network";
        }

        private string wifipassword(string wifiname)
        {
            // netsh wlan show profile name=* key=clear
            string argument = "wlan show profile name=\"" + wifiname + "\" key=clear";
            Process processWifi = new Process();
            processWifi.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processWifi.StartInfo.FileName = "netsh";
            processWifi.StartInfo.Arguments = argument;
            //processWifi.StartInfo.WorkingDirectory = Path.GetDirectoryName(YourApplicationPath);

            processWifi.StartInfo.UseShellExecute = false;
            processWifi.StartInfo.RedirectStandardError = true;
            processWifi.StartInfo.RedirectStandardInput = true;
            processWifi.StartInfo.RedirectStandardOutput = true;
            processWifi.StartInfo.CreateNoWindow = true;
            processWifi.Start();
            //* Read the output (or the error)
            string output = processWifi.StandardOutput.ReadToEnd();
            // Show output commands
            string err = processWifi.StandardError.ReadToEnd();
            // show error commands
            processWifi.WaitForExit();
            return output;
        }

        private void FinishLoadingAnimation()
        {
            ExtendedVisualStateManager.GoToElementState(this.Loading.grid, "LoadOffState", true);
            this.Loading.Visibility = Visibility.Collapsed;
        }
        private void StartLoadingAnimation()
        {
            data.ItemsSource = null;
            Wifis.Clear();
            this.Loading.Visibility = Visibility.Visible;
            ExtendedVisualStateManager.GoToElementState(this.Loading.grid, "LoadOnState", true);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StartLoadingAnimation();
            bgw = new BackgroundWorker();
            bgw.DoWork += bgw_DoWork;
            bgw.RunWorkerCompleted += bgw_RunWorkerCompleted;
            bgw.RunWorkerAsync();
        }

        void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            FinishLoadingAnimation();
            data.ItemsSource = Wifis;
        }

        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            List<string> wifiNames = GetWifiNames();
            List<string> wifiPasswords = new List<string>();
            foreach (string item in wifiNames)
            {
                wifiPasswords.Add(GetWifiPassword(item));
            }

            for (int i = 0; i < wifiNames.Count(); i++)
            {
                Wifis.Add(wifiNames[i], wifiPasswords[i]);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string filePath = AppDomain.CurrentDomain.BaseDirectory + @"\WifiPasswords.csv";
            StringBuilder sb = new StringBuilder();
            foreach (var item in Wifis)
	        {
		        string newLine = String.Format("{0};{1}", item.Key, item.Value);
                sb.AppendLine(newLine);
	        }
            File.WriteAllText(filePath, sb.ToString());
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as CheckBox).IsChecked == true)
            {
                Visible = true;
            }
            else
            {
                Visible = false;
            }
            data.ItemsSource = null;
            data.ItemsSource = Wifis;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            data.Height = this.Window.ActualHeight - 100;
            data.Width = this.Window.ActualWidth - 30;

            Loading.Height = this.Window.ActualHeight;
            Loading.Width = this.Window.ActualWidth;
        }
    }
}
