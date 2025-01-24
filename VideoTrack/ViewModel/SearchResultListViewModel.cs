using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoTrack.Controls;
using static Emgu.CV.Dai.OpenVino;

namespace VideoTrack
{
    public class SearchResultListViewModel
    {
        public SearchResultListViewModel()
        {
            Rows = new ObservableCollection<SearchResultRowViewModel>();
        }

        private ObservableCollection<SearchResultRowViewModel> m_Rows;

        public ObservableCollection<SearchResultRowViewModel> Rows
        {
            get { return m_Rows; }
            set { m_Rows = value; }
        }


        public bool Contains(MarkerPair m)
        {
            SearchResultRowViewModel row = new SearchResultRowViewModel();
            row.Marker = m;

            if (Rows.Contains(row))
                return true;
            else
                return false;
        }


        public bool Contains(SearchResultRowViewModel test)
        {
            if (Rows.Contains(test))
                return true;
            else
                return false;
        }


        public void Add(MarkerPair markerPair)
        {
            SearchResultRowViewModel rowMightAdd = 
                new SearchResultRowViewModel
                {
                    ItemTag = markerPair.PairID().ToString("D6"),
                    FoundCamera = markerPair.PairDescription
                };

            if (!Contains(rowMightAdd))
                Rows.Add(rowMightAdd);
            else
                // If we already had it, delete the attempted add
                rowMightAdd = null;
        }

        public void RemoveItemsNotFound(ref List<int> listRowsToFind)
        {
            foreach(SearchResultRowViewModel row in Rows)
            {
                if (listRowsToFind.Contains(int.Parse(row.ItemTag)) == false)
                {
                    // If we couldn't find the desired tag
                    // in the list to find, then remove the 
                    // specified row.
                    Rows.Remove(row);
                }
            }
        }

        public void Remove(MarkerPair markerPair)
        {
            foreach (SearchResultRowViewModel row in Rows)
            {
                if (row.ItemTag == markerPair.PairID().ToString("D6"))
                {
                    // If we found a matching row ItemTag
                    // then remove the specified row.
                    Rows.Remove(row);
                }
            }
        }
    }
}
