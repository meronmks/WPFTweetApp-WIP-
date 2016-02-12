using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using CoreTweet;
using TweetApp.CoreClass;
using UniRx;

namespace TweetApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        public OAuth.OAuthSession Session { get; set; }
        private Tokens tokens { get; set; }
        public List<Status> status { get; set; }

        

        public MainWindow()
        {
            InitializeComponent();
            status = new List<Status>();
            if (Properties.Settings.Default.AccessToken != "" &&
                Properties.Settings.Default.AccessTokenSecret != "")
            {
                tokens = Tokens.Create(
                    TwitterProperties.APIKey,
                    TwitterProperties.APISecret,
                    Properties.Settings.Default.AccessToken,
                    Properties.Settings.Default.AccessTokenSecret);
            }
            else
            {
                var OauthWindow = new OauthWindow();
                OauthWindow.Show();
                Close();
            }
        }

        private async void ButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var tweet in await tokens.Statuses.HomeTimelineAsync())
            {
                status.Add(tweet);
            }

            listBox.ItemsSource = status;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.T:
                    var TweetSendWindow = new TweetSendWindow(tokens);
                    TweetSendWindow.ShowDialog();
                    break;
                    
            }
        }
    }
}
