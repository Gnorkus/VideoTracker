using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoTrack.Model
{
    public class CameraCalibParams : CoreDatabase
    {
        public string szRTSPStreamURL { get; set; }
        public string szCameraMatrix { get; set; }
        public string szDistortionMatrix { get; set; }  
        public string szFloorMarkerLocations { get; set; }

    }
}
