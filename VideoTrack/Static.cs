using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace VideoTrack
{
    // All dimensions are in inches
    public static class Static
    {
        static Static()
        {

        }

        public static void Initialize()
        {
            // Force the static constructor to be called
            defaultConnString = ConfigurationHelper.GetConnectionString("DefaultConnection");
        }


        public static bool bDetectMarkers = false;
        public static bool bCopyToPictureBox = true;

        public static double m_dPositionTolerance = 6.0;

        public static string defaultConnString; 
    }
}
