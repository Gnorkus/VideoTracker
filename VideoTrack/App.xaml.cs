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

            // Enable detailed tracing for data binding errors
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
            PresentationTraceSources.DataBindingSource.Listeners.Add(new CustomTraceListener());

        }

        // https://stackoverflow.com/questions/47391020/cannot-find-source-for-binding-with-reference-relativesource-findancestor
        public class CustomTraceListener : TraceListener
        {
            public override void Write(string message)
            {
                // Optional: Handle if needed
            }

            public override void WriteLine(string message)
            {
                if (message.Contains("System.Windows.Data Error: 4"))
                {
                    Debugger.Break(); // Trigger a breakpoint
                }
            }
        }
    }

}
