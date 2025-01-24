using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Npgsql;

namespace VideoTrack
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public string szFileOpenFromCmdLine = "";


        // Connect to the PostgreSQL server


        App()
        {
            Static.Initialize();
        }



        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Access command line parameters
            string[] commandLineArgs = e.Args;

            // We're looking for a startup file path...
            // If we don't get a useful one, then just
            // start up normally.

            // We will populate the app variable szFileOpenFromCmdLine with the 
            // 
            if (commandLineArgs.Length > 0 )
                szFileOpenFromCmdLine = commandLineArgs[0];
        }

    }

}
