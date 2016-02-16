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
    /// TweetSendWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetSendWindow : Window
    {

        private Tokens Tokens { get; set; }
        private readonly TweetSendStatus sendStatus;
        private long inReplyToStatusID;
        private string SendText;

        public TweetSendWindow(TweetSendStatus _sendStatus)
        {
            InitializeComponent();
            Tokens = Tokens.Create(
                TwitterProperties.APIKey,
                TwitterProperties.APISecret,
                Properties.Settings.Default.AccessToken,
                Properties.Settings.Default.AccessTokenSecret);
            sendStatus = _sendStatus;

            switch (_sendStatus)
            {
                case TweetSendStatus.Post:
                    Title = "新規ツイート";
                    break;
                case TweetSendStatus.Reply:
                    Title = "新規リプライ";
                    break;

            }
        }

        public void SetInReplyToStatus(Status _inReplyToStatus)
        {
            inReplyToStatusID = _inReplyToStatus.Id;
            TweetTextBox.Text = "@" + _inReplyToStatus.User.ScreenName + " ";
            TweetTextBox.Select(TweetTextBox.Text.Length, 0);
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            ModifierKeys modifierKeys = Keyboard.Modifiers;
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Enter:
                    if ((modifierKeys & ModifierKeys.Control) == ModifierKeys.None) break;
                    SendText = TweetTextBox.Text;
                    SendTweet();
                    break;
            }
        }

        private async void SendTweet()
        {
            StatusResponse result = new StatusResponse();

            switch (sendStatus)
            {
                case TweetSendStatus.Post:
                    result = await Tokens.Statuses
                        .UpdateAsync(status => SendText);
                    break;
                case TweetSendStatus.Reply:
                    result = await Tokens.Statuses
                        .UpdateAsync(status => SendText,
                        replyID => inReplyToStatusID);
                    break;
            }
            if (result.Text == null) return;
            Close();
        }
    }
}
