using System;
using System.Collections.Generic;
using System.Diagnostics;
using VideoTrack.Controls;
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
using System.Windows.Threading;
using WPF.MDI;

namespace VideoTrack
{
    /// <summary>
    /// Interaction logic for SearchSpecificTagDlg.xaml
    /// </summary>
    public partial class SearchSpecificTagDlg : Window
    {
        private DispatcherTimer _timer;
        private SearchResultListViewModel m_SearchResults = new SearchResultListViewModel();
        Main myParent;
        bool bForceClose = false;

        public SearchSpecificTagDlg(Main otherParent)
        {
            InitializeComponent();


            this.DataContext = m_SearchResults;

            myParent = otherParent;

            // Initialize the DispatcherTimer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200) // Set interval to 1 second
            };

            // Subscribe to the Timer Tick event
            _timer.Tick += timer_Tick;

            // Start the timer
            _timer.Start();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // This starts the search and disables list entry
            ListToSearchFor.IsEnabled = false;

            // We store a sorted list of IDs
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // This stops the search and enables list entry
            ListToSearchFor.IsEnabled = true;
        }

        List<int> GetSearchResultsIDs()
        {
            List<int> rval = new List<int>();
            foreach (var p in m_SearchResults.Rows)
            {
                int nItemTag = int.Parse(p.ItemTag);
                if (rval.Contains(nItemTag)) {}
                else
                    rval.Add(nItemTag);
                rval.Sort();
            }
            return rval;
        }

        List<int> GetSortedListOfIDsToFind ()
        {
            List<int> rval = new List<int>();
            string values = ListToSearchFor.Text;
            char[] delimiterChars = { ',', '\t', '\n', '\r' };
            string[] numbers = values.Split(delimiterChars);
            for (int i=0; i<numbers.Length; i++)
            {
                try
                {
                    if (numbers[i].Length > 0)
                        rval.Add(int.Parse(numbers[i]));
                }
                catch (Exception e)
                {
                    
                }

            }

            rval.Sort();

            return rval;
        }


        void timer_Tick(object sender, EventArgs e)
        {
            List<int> listIDsToFind = GetSortedListOfIDsToFind();
            List<int> listSearchResultsIDs = GetSearchResultsIDs();
            List<MarkerPair> listFoundMarkers = new List<MarkerPair>();
            List<int> listFoundIDs = new List<int>();

            // Constantly refresh the list box.  If
            // an item exists in a specific camera,
            // and does not exist in the list box,
            // add it.  Otherwise, if the item
            // no longer exists in a specific camera
            // but is in the list box, remove it
            // from thee listbox.

            // First, get the list of ID pairs in the current list
            // control.  

            // Check all child windows to see if the
            // tags in our list exist.
            if (ListToSearchFor.IsEnabled)
            {
                if (myParent != null)
                {
                    // Find all the IDs in each individual window.
                    // The window name will be stored in the p.Value
                    // (MarkerPair)
                    foreach (MdiChild c in myParent.Container.Children)
                    {
                        if (c.Content is VideoUserControl)
                        {
                            VideoUserControl vu = c.Content as VideoUserControl;
                            List<int> listSortedPairIDs = new List<int>();

                            foreach (var p in vu.m_listCurrentMarker)
                            {
                                if (listIDsToFind.Contains(p.Value.PairID))
                                {
                                    listFoundMarkers.Add(p.Value);
                                    listFoundIDs.Add(p.Value.PairID);
                                }
                            }
                        }
                    }

                    // When we're done, we will have all the ids that
                    // the cameras are seeing.  The next thing to do
                    // is to see if the PairID / Description in the
                    // row is the same or not.
                    List<SearchResultRowViewModel> listRowsToRemove = new List<SearchResultRowViewModel>();
                    foreach (var p in m_SearchResults.Rows)
                    {
                        // If we didn't find this Row's ID in a "camera found list"
                        // that means it's no longer there, so remove it from
                        // the search results.
                        if (!listFoundMarkers.Contains(p.Marker))
                            listRowsToRemove.Add(p);
                    }
                    foreach (var p in listRowsToRemove)
                        m_SearchResults.Rows.Remove(p);

                    foreach (MarkerPair m in listFoundMarkers)
                    {
                        if (!m_SearchResults.Contains(m))
                            m_SearchResults.Add(m);
                    }
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }

        public void ForceCloseWindow()
        {
            bForceClose = true;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (bForceClose == true)
            {
                e.Cancel = false;
                return;
            }
            else
            {
                e.Cancel = true;
                this.Visibility = Visibility.Hidden;
            }
        }
    }
}
