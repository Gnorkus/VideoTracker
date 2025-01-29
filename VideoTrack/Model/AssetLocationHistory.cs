using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoTrack.Model
{
    public class AssetLocationHistory : CoreDatabase
    {
        [PrimaryIdentityKey] public int AssetLocationHistoryPK { get; }   // unique key
        public int AssetFK { get; set; }                  // 
        public int AssetType { get; set; }
        public int CameraFK { get; set; }
        public double PosX { get; set; }
        public double PosY { get; set; }
        public double PosZ { get; set; }
        public DateTime Date { get; set; }
        public bool IsStale { get; set; }
        public int LocationStateFK { get; set; }
    }
}