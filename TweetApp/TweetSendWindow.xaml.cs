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

namespace TweetApp
{
    /// <summary>
    /// TweetSendWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetSendWindow : Window
    {

        private Tokens tokens { get; set; }

        public TweetSendWindow(Tokens _tokens)
        {
            InitializeComponent();
            tokens = _tokens;
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Enter:
                    ModifierKeys modifierKeys = Keyboard.Modifiers;
                    if ((modifierKeys & ModifierKeys.Control) != ModifierKeys.None)
                    {
                        tokens.Statuses.Update(status => TweetTextBox.Text);
                        Close();
                    }
                    break;

            }
        }
    }
}
