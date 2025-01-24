using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MillerMetal.VideoTrack.Commands
{
    public static class VideoTrackCommands
    {
        public static readonly RoutedCommand OpenConfig = new RoutedCommand();
        public static readonly RoutedCommand SaveConfig = new RoutedCommand();
        public static readonly RoutedCommand StartAllCalib = new RoutedCommand();
        public static readonly RoutedCommand StartCurCalib = new RoutedCommand();
        public static readonly RoutedCommand StopCalib = new RoutedCommand();
        public static readonly RoutedCommand PrintTargets = new RoutedCommand();
        public static readonly RoutedCommand NewMapWindow = new RoutedCommand();
    }
}
        