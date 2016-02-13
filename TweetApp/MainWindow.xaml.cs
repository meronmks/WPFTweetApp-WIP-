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
        private Tokens tokens { get; set; }
        public List<Status> status { get; set; }
        private IDisposable disposable { get; set; }
        private ICollectionView view;

        public MainWindow()
        {
            InitializeComponent();
            status = new List<Status>();
            listBox.ItemsSource = status;

            view = CollectionViewSource.GetDefaultView(status);
            view.SortDescriptions.Add(new SortDescription("CreatedAt.LocalDateTime", ListSortDirection.Descending));

            var liveShaping = view as ICollectionViewLiveShaping;

            if (liveShaping != null && liveShaping.CanChangeLiveSorting)
            {
                liveShaping.LiveSortingProperties.Add("CreatedAt.LocalDateTime");
                liveShaping.IsLiveSorting = true;
            }

            if (Properties.Settings.Default.AccessToken != "" &&
                Properties.Settings.Default.AccessTokenSecret != "")
            {
                tokens = Tokens.Create(
                    TwitterProperties.APIKey,
                    TwitterProperties.APISecret,
                    Properties.Settings.Default.AccessToken,
                    Properties.Settings.Default.AccessTokenSecret);

                disposable = tokens.Streaming.UserAsObservable()
                    .Where((StreamingMessage m) => m.Type == MessageType.Create)
                    .Cast<StatusMessage>()
                    .Select((StatusMessage m) => m.Status)
                    .Subscribe((Status s) =>
                    {
                        status.Add(s);
                        App.Current.Dispatcher.Invoke(
                            new Action(() =>
                            {
                                view.Refresh();
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
            foreach (var tweet in await tokens.Statuses.HomeTimelineAsync())
            {
                status.Add(tweet);
            }
            view.Refresh();
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

        private void WindowClosed(object sender, EventArgs e)
        {
            disposable.Dispose();
        }
    }
}
