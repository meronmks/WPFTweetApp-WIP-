﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CoreTweet;
using CoreTweet.Streaming;
using TweetApp.CoreClass;

namespace TweetApp
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        public OAuth.OAuthSession Session { get; set; }
        private Tokens Tokens { get; set; }
        public HashSet<Status> Status { get; set; }
        private IDisposable StreamingDisposable { get; set; }
        private ICollectionView View;
        private bool isShowMenu = false;
        private ModifierKeys modifierKeys;
        private bool LoadLock = false;

        public MainWindow()
        {
            InitializeComponent();
            Status = new HashSet<Status>();
            listBox.ItemsSource = Status;

            View = CollectionViewSource.GetDefaultView(Status);
            View.SortDescriptions.Add(new SortDescription("CreatedAt.LocalDateTime", ListSortDirection.Descending));

            var liveShaping = View as ICollectionViewLiveShaping;

            if (liveShaping != null && liveShaping.CanChangeLiveSorting)
            {
                liveShaping.LiveSortingProperties.Add("CreatedAt.LocalDateTime");
                liveShaping.IsLiveSorting = true;
            }

            if (Properties.Settings.Default.AccessToken != "" &&
                Properties.Settings.Default.AccessTokenSecret != "")
            {
                Tokens = Tokens.Create(
                    TwitterProperties.APIKey,
                    TwitterProperties.APISecret,
                    Properties.Settings.Default.AccessToken,
                    Properties.Settings.Default.AccessTokenSecret);

                GetHomeTimeLineAsync();

                StreamingDisposable = Tokens.Streaming.UserAsObservable()
                    .Where((StreamingMessage m) => m.Type == MessageType.Create)
                    .Cast<StatusMessage>()
                    .Select((StatusMessage m) => m.Status)
                    .Subscribe((Status tweet) =>
                    {
                        Application.Current.Dispatcher.Invoke(
                            new Action(() =>
                            {
                                if (Status.Contains(tweet)) return;
                                Status.Add(TweetProcessing(tweet));
                                var selectIndex = listBox.SelectedIndex;
                                if (selectIndex != 0) selectIndex++;
                                View.Refresh();
                                listBox.SelectedIndex = selectIndex;
                                listBox.ScrollIntoView(listBox.SelectedItem);
                                //カーソルでの位置固定
                                var lbi = listBox.ItemContainerGenerator.ContainerFromIndex(selectIndex) as ListBoxItem;
                                lbi?.Focus();
                            })
                        );
                        
                    });
            }
            else
            {
                var OauthWindow = new OauthWindow();
                OauthWindow.Show();
                Close();
            }
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            modifierKeys = Keyboard.Modifiers;
            var listItem = listBox.SelectedItem as Status;
            switch (e.Key)
            {
                case Key.N:               
                    if ((modifierKeys & ModifierKeys.Control) == ModifierKeys.None) break;
                    var TweetSendWindow = new TweetSendWindow(TweetSendStatus.Post);
                    TweetSendWindow.Owner = Window.GetWindow(this); //オーナー設定
                    TweetSendWindow.ShowDialog();
                    break;             
                case Key.Escape:
                    SwitchMenu();
                    break;
                case Key.R:
                    if (((modifierKeys & ModifierKeys.Control) == ModifierKeys.None) || listBox.SelectedItem == null) break;
                    var ReplySendWindow = new TweetSendWindow(TweetSendStatus.Reply);
                    ReplySendWindow.Owner = Window.GetWindow(this); //オーナー設定
                    ReplySendWindow.SetInReplyToStatus(listItem);
                    ReplySendWindow.ShowDialog();
                    break;
                case Key.F:
                    if (((modifierKeys & ModifierKeys.Control) == ModifierKeys.None) || listBox.SelectedItem == null) break;
                    var favResult = MessageBox.Show(listItem?.Text + "\r\nをお気に入りしてよろしいですか？", "確認", MessageBoxButton.YesNo);
                    if (favResult == MessageBoxResult.No) break;
                    FavTweetAsync(listItem?.Id);
                    break;
                case Key.F5:
                    GetHomeTimeLineAsync();
                    break;
                case Key.T:
                    if (((modifierKeys & ModifierKeys.Control) == ModifierKeys.None) || listBox.SelectedItem == null) break;
                    var rtResult = MessageBox.Show(listItem?.Text + "\r\nをRTしてよろしいですか？", "確認", MessageBoxButton.YesNo);
                    if (rtResult == MessageBoxResult.No) break;
                    ReTweetAsync(listItem?.Id);
                    break;
                case Key.Down:
                    var selectIndex = listBox.SelectedIndex;
                    if (listBox.Items.Count != selectIndex + 1 || LoadLock) break;
                    LoadLock = true;
                    GetHomeTimeLineAsync(listItem?.Id);
                    break;
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            StreamingDisposable.Dispose();
        }

        private void WindowActivated(object sender, EventArgs e)
        {
            listBox.Focus();
        }

        /// <summary>
        /// ツイート取得に関する奴
        /// </summary>
        /// <param name="maxID"></param>
        private async void GetHomeTimeLineAsync(long? maxID = null)
        {
            try
            {
                var flug = maxID.HasValue;
                var selectIndex = listBox.SelectedIndex;
                foreach (var tweet in await Tokens.Statuses.HomeTimelineAsync(null, null, maxID))
                {
                    if (Status.Contains(tweet) || flug)
                    {
                        flug = false;
                        continue;
                    }
                    Status.Add(TweetProcessing(tweet));
                }
                View.Refresh();
                LoadLock = false;
                if (maxID == null) return;
                listBox.SelectedIndex = selectIndex++;
                listBox.ScrollIntoView(listBox.SelectedItem);
                //カーソルでの位置固定
                var lbi = listBox.ItemContainerGenerator.ContainerFromIndex(selectIndex) as ListBoxItem;
                lbi?.Focus();

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error!");
            }

        }

        /// <summary>
        /// Listに入れる前にtweetStatusを加工するもの
        /// </summary>
        /// <param name="tweet"></param>
        /// <returns></returns>
        private Status TweetProcessing(Status tweet)
        {
            tweet.Source = SourceHTMLParser(tweet.Source);
            if (tweet.RetweetedStatus != null)
            {
                tweet.RetweetedStatus.Text = WebUtility.HtmlDecode(tweet.RetweetedStatus.Text);
                tweet.RetweetedStatus.Source = SourceHTMLParser(tweet.RetweetedStatus.Source);
            }
            tweet.Text = WebUtility.HtmlDecode(tweet.Text);
            return tweet;
        }

        /// <summary>
        /// RTの非同期処理
        /// </summary>
        /// <param name="tweetID"></param>
        private async void ReTweetAsync(long? tweetID)
        {
            if (!tweetID.HasValue) return;
            try
            {
                await Tokens.Statuses.RetweetAsync(id => tweetID);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error!");
            }
        }

        /// <summary>
        /// Favの非同期処理
        /// </summary>
        /// <param name="tweetID"></param>
        private async void FavTweetAsync(long? tweetID)
        {
            if (!tweetID.HasValue) return;
            try
            {
                await Tokens.Favorites.CreateAsync(id => tweetID);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Error!");
            }
        }
        /// <summary>
        /// StatusのSourceからVia名抜き出し
        /// </summary>
        /// <param name="html">Source</param>
        /// <returns></returns>
        private string SourceHTMLParser(string html)
        {
            var doc = new HtmlAgilityPack.HtmlDocument
            {
                OptionAutoCloseOnEnd = false,
                OptionCheckSyntax = false,
                OptionFixNestedTags = true
            };
            doc.LoadHtml(html);
            var nodes = doc.DocumentNode.SelectSingleNode("//a");
            return nodes.InnerText;
        }

        /// <summary>
        /// メニューの出し入れ操作
        /// </summary>
        private void SwitchMenu()
        {
            if (isShowMenu == false)
            {
                var s = TryFindResource("MainMenuLoadStoryBoard") as Storyboard;
                BeginStoryboard(s);
                isShowMenu = true;
            }
            else
            {
                var s = TryFindResource("MainMenuDeleteStoryBoard") as Storyboard;
                BeginStoryboard(s);
                isShowMenu = false;
            }
        }
    }
}
