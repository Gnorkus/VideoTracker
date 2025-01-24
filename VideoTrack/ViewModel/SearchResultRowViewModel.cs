using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoTrack.Controls;

namespace VideoTrack
{
    public class SearchResultRowViewModel : INotifyPropertyChanged
    {
        public SearchResultRowViewModel() { }

        private MarkerPair m_Marker;

        public MarkerPair Marker
        {
            get
            {
                return m_Marker;
            }
            set
            {
                m_Marker = value;
                ItemTag = m_Marker.PairID().ToString("D6");
                FoundCamera = m_Marker.PairDescription;
            }
        }


        private string m_ItemTag;

        public string ItemTag
        {
            get 
            { 
                return m_ItemTag; 
            }
            set 
            {
                m_ItemTag = value;
                NotifyPropertyChanged("ItemTag");
            }
        }


        private string m_FoundCamera;
        public string FoundCamera 
        {
            get 
            {
                return m_FoundCamera;
            }

            set 
            { 
                m_FoundCamera = value;
                NotifyPropertyChanged("FoundCamera");
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SearchResultRowViewModel otherRow)
            {
                return this.m_ItemTag == otherRow.m_ItemTag
                    && this.m_FoundCamera == otherRow.m_FoundCamera;
            }
            else
            if (obj is MarkerPair otherMarker)
            {
                return m_Marker.PairDescription == otherMarker.PairDescription
                    && m_Marker.PairID() == otherMarker.PairID();
            }
            return false;
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string Obj)
        {
            if (PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(Obj));
            }
        }
    }
}
