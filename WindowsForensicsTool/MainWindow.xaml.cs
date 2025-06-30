using System.Windows;

namespace WindowsForensicsTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ActivityHistory _activityHistory;
        private ExtensionMismatch _extensionMismatch;
        private KeywordSearch _keywordSearch;

        public MainWindow()
        {
            _activityHistory = new ActivityHistory(this);
            _extensionMismatch = new ExtensionMismatch(this);
            _keywordSearch = new KeywordSearch(this);
            
            InitializeComponent();
        }

        private void ActivityHistorySearchButton_Click(object sender, RoutedEventArgs e)
        {
            _activityHistory.ActivityHistorySearchButton_Click();
        }

        private void ActivityHistoryRadioButtons_Checked(object sender, RoutedEventArgs e)
        {
            _activityHistory.ActivityHistoryRadioButtons_Checked();
        }

        private void ExtensionMismatchSearchButton_Click(object sender, RoutedEventArgs e)
        {
            _extensionMismatch.ExtensionMismatchSearchButton_Click();
        }

        private void KeywordSearchSearchButton_Click(object sender, RoutedEventArgs e)
        {
            _keywordSearch.KeywordSearchSearchButton_Click();
        }
    }
}
