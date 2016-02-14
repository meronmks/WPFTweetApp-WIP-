using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
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
        public List<Status> Status { get; set; }
        private IDisposable StreamingDisposable { get; set; }
        private ICollectionView View;

        public MainWindow()
        {
            InitializeComponent();
            Status = new List<Status>();
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

                StreamingDisposable = Tokens.Streaming.UserAsObservable()
                    .Where((StreamingMessage m) => m.Type == MessageType.Create)
                    .Cast<StatusMessage>()
                    .Select((StatusMessage m) => m.Status)
                    .Subscribe((Status s) =>
                    {   
                        App.Current.Dispatcher.Invoke(
                            new Action(() =>
                            {
                                var selectIndex = listBox.SelectedIndex;
                                selectIndex++;
                                Status.Add(s);
                                View.Refresh();
                                listBox.SelectedIndex = selectIndex;
                                listBox.ScrollIntoView(listBox.SelectedItem);
                                //カーソルでの位置固定
                                var lbi = listBox.ItemContainerGenerator.ContainerFromIndex(selectIndex) as ListBoxItem;
                                lbi.Focus();
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

        private async void ButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var tweet in await Tokens.Statuses.HomeTimelineAsync())
            {
                Status.Add(tweet);
            }
            View.Refresh();
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.T:
                    var TweetSendWindow = new TweetSendWindow(Tokens);
                    TweetSendWindow.ShowDialog();
                    break;                   
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            StreamingDisposable.Dispose();
        }
    }
}
