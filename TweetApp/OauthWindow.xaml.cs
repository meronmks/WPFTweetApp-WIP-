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
using System.Windows.Shapes;
using CoreTweet;
using TweetApp.CoreClass;

namespace TweetApp
{
    /// <summary>
    /// OauthWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class OauthWindow : Window
    {
        private OAuth.OAuthSession Session { get; set; }
        private Tokens tokens = TwitterProperties.tokens;

        public OauthWindow()
        {
            InitializeComponent();

            Session = OAuth.Authorize(TwitterProperties.APIKey, TwitterProperties.APISecret);
            WebBrowser.Source = Session.AuthorizeUri;
        }

        private void AuthorizeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                tokens = Session.GetTokens(PinBox.Text);
                Properties.Settings.Default.AccessToken = tokens.AccessToken;
                Properties.Settings.Default.AccessTokenSecret = tokens.AccessTokenSecret;
                Properties.Settings.Default.Save();
                var result = MessageBox.Show("認証成功！", "Success", MessageBoxButton.OK);
                if (result == MessageBoxResult.OK)
                {
                    var MainWindow = new MainWindow();
                    MainWindow.Show();
                    Close();
                }

            }
            catch (Exception)
            {
                MessageBox.Show("認証失敗。PINを再入力してください。", "Error", MessageBoxButton.OK);
            }
        }
    }
}
