using System;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using VideoTrack.Controls;
using WPF.MDI;
using System.Windows.Controls;
//using System.ComponentModel;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using System.Collections.Specialized;
using System.Windows.Forms;
using FileDialog = Microsoft.Win32.FileDialog;
using System.Windows.Input;
using Emgu.CV.Stitching;
using System.Diagnostics;
using VideoTrack.Model;
using Npgsql;

namespace VideoTrack
{
	/// <summary>
	/// Interaction logic for Main.xaml
	/// </summary>
	
	public partial class Main : Window
	{
        public SearchSpecificTagDlg dlgSearchSpecificTag;


        /// <summary>
        /// Initializes a new instance of the <see cref="Main"/> class.
        /// </summary>
        public Main()
		{
			InitializeComponent();
			_original_title = Title;
			Container.Children.CollectionChanged += (o, e) => Menu_RefreshWindows();
			Container.MdiChildTitleChanged += Container_MdiChildTitleChanged;

            dlgSearchSpecificTag = new SearchSpecificTagDlg(this);

            /*
			Container.Children.Add(new MdiChild
			{
				Title = "Empty Window Using Code",
				Icon = new BitmapImage(new Uri("OriginalLogo.png", UriKind.Relative))
			});

			*/
        }

        #region Mdi-like title

        string _original_title;

		void Container_MdiChildTitleChanged(object sender, RoutedEventArgs e)
		{
			if (Container.ActiveMdiChild != null && Container.ActiveMdiChild.WindowState == WindowState.Maximized)
				Title = _original_title + " - [" + Container.ActiveMdiChild.Title + "]";
			else
				Title = _original_title;
		}

		#endregion

		#region Theme Menu Events

		/// <summary>
		/// Handles the Click event of the Generic control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Generic_Click(object sender, RoutedEventArgs e)
		{
			Generic.IsChecked = true;
			Luna.IsChecked = false;
			Aero.IsChecked = false;

			Container.Theme = ThemeType.Generic;
		}

		/// <summary>
		/// Handles the Click event of the Luna control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Luna_Click(object sender, RoutedEventArgs e)
		{
			Generic.IsChecked = false;
			Luna.IsChecked = true;
			Aero.IsChecked = false;

			Container.Theme = ThemeType.Luna;
		}

		/// <summary>
		/// Handles the Click event of the Aero control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void Aero_Click(object sender, RoutedEventArgs e)
		{
			Generic.IsChecked = false;
			Luna.IsChecked = false;
			Aero.IsChecked = true;

			Container.Theme = ThemeType.Aero;
		}

		#endregion

		#region Menu Events

		int ooo = 1;

		/// <summary>
		/// Handles the Click event of the 'Normal window' menu item.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void AddWindow_Click(object sender, RoutedEventArgs e)
		{
            //Container.Children.Add();

            Container.Children.Add(new MdiChild { Content = new System.Windows.Controls.Label { Content = "Normal window" }, Title = "Window " + ooo++ });
		}

		/// <summary>
		/// Handles the Click event of the 'Fixed window' menu item.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void AddFixedWindow_Click(object sender, RoutedEventArgs e)
		{
			Container.Children.Add(new MdiChild { Content = new System.Windows.Controls.Label { Content = "Fixed width window" }, Title = "Window " + ooo++, Resizable = false });
		}

		/// <summary>
		/// Handles the Click event of the 'Scroll window' menu item.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void AddScrollWindow_Click(object sender, RoutedEventArgs e)
		{
			StackPanel sp = new StackPanel { Orientation = System.Windows.Controls.Orientation.Vertical };
			sp.Children.Add(new TextBlock { Text = "Window with scroll", Margin = new Thickness(5) });
			sp.Children.Add(new System.Windows.Controls.ComboBox { Margin = new Thickness(20), Width = 300 });
			ScrollViewer sv = new ScrollViewer { Content = sp, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };

			Container.Children.Add(new MdiChild { Content = sv, Title = "Window " + ooo++ });
		}


        /// <summary>
        /// Handles the Click event of the 'Video window' menu item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddMapWindow_Click(object sender, RoutedEventArgs e)
        {
            //Container.Children.Add();
            MdiChild mdiChild = new MdiChild();
            MapUserControl mapUserControl = new MapUserControl();
            mdiChild.Title = "Map of Tracked Items " + ooo++;
            mdiChild.Content = mapUserControl;
            mdiChild.Width = 256;
            mdiChild.Height = 256;
            mdiChild.Position = new Point(50 + ((ooo * 20) % 400), 80 + ((ooo * 20) % 400));
            mdiChild.Closing += mapUserControl.MdiChild_Closing;

            Container.Children.Add(mdiChild);
            //Container.Children.Add(new MdiChild { Content = new Label { Content = "Normal window" }, Title = "Window " + ooo++ });
        }


        /// <summary>
        /// Handles the Click event of the 'Video window' menu item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AddVideoWindow_Click(object sender, RoutedEventArgs e)
        {
            //Container.Children.Add();
			MdiChild mdiChild = new MdiChild();
			VideoUserControl videoUserControl = new VideoUserControl();
			mdiChild.Title = "Video Window" + ooo++;
            mdiChild.Content = videoUserControl;
			mdiChild.Width = 256;
			mdiChild.Height = 256;
            mdiChild.Position = new Point(50+((ooo*20)%400), 80 + ((ooo * 20) % 400));
			mdiChild.Closing += videoUserControl.MdiChild_Closing;

            Container.Children.Add(mdiChild);
            //Container.Children.Add(new MdiChild { Content = new Label { Content = "Normal window" }, Title = "Window " + ooo++ });
        }

        /// <summary>
        /// Refresh windows list
        /// </summary>
        void Menu_RefreshWindows()
		{
			int i;
			WindowsMenu.Items.Clear();
			System.Windows.Controls.MenuItem mi;
			for (i = 0; i < Container.Children.Count; i++)
			{
				MdiChild child = Container.Children[i];
				mi = new System.Windows.Controls.MenuItem { Header = child.Title };
				mi.Click += (o, e) => child.Focus();
				WindowsMenu.Items.Add(mi);
			}
			WindowsMenu.Items.Add(new Separator());
			WindowsMenu.Items.Add(mi = new System.Windows.Controls.MenuItem { Header = "Cascade" });
			mi.Click += (o, e) => Container.MdiLayout = WPF.MDI.MdiLayout.Cascade;
			WindowsMenu.Items.Add(mi = new System.Windows.Controls.MenuItem { Header = "Horizontally" });
			mi.Click += (o, e) => Container.MdiLayout = WPF.MDI.MdiLayout.TileHorizontal;
			WindowsMenu.Items.Add(mi = new System.Windows.Controls.MenuItem { Header = "Vertically" });
			mi.Click += (o, e) => Container.MdiLayout = WPF.MDI.MdiLayout.TileVertical;

			WindowsMenu.Items.Add(new Separator());
			WindowsMenu.Items.Add(mi = new System.Windows.Controls.MenuItem { Header = "Close all" });
			mi.Click += (o, e) => Container.Children.Clear();

            // Update the MRU Menu List while we're at it
            var MRU = GetMRU();
			i = 0;

            RecentConfigurations.Items.Clear();

            foreach (var item in MRU)
			{ 
				i++;

				// We add the '_' in front of the number so that it will be recognized as a shortcut
				RecentConfigurations.Items.Add(
					mi = new System.Windows.Controls.MenuItem { Header = "_" + i.ToString() + " " + item });
	
				mi.Click += (o, e) => MRU_Executed(o, e as ExecutedRoutedEventArgs);
            }
        }

        #endregion

        #region Content Button Events

        /*

		/// <summary>
		/// Handles the Click event of the DisableMinimize control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void DisableMinimize_Click(object sender, RoutedEventArgs e)
		{
			Window1.MinimizeBox = false;
		}

		/// <summary>
		/// Handles the Click event of the EnableMinimize control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void EnableMinimize_Click(object sender, RoutedEventArgs e)
		{
			Window1.MinimizeBox = true;
		}

		/// <summary>
		/// Handles the Click event of the DisableMaximize control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void DisableMaximize_Click(object sender, RoutedEventArgs e)
		{
			Window1.MaximizeBox = false;
		}

		/// <summary>
		/// Handles the Click event of the EnableMaximize control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void EnableMaximize_Click(object sender, RoutedEventArgs e)
		{
			Window1.MaximizeBox = true;
		}

        /// <summary>
        /// Handles the Click event of the DisableClose control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DisableClose_Click(object sender, RoutedEventArgs e)
        {
            Window1.CloseBox = false;
        }

        /// <summary>
        /// Handles the Click event of the EnableClose control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void EnableClose_Click(object sender, RoutedEventArgs e)
        {
            Window1.CloseBox = true;
        }

		/// <summary>
		/// Handles the Click event of the ShowIcon control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void ShowIcon_Click(object sender, RoutedEventArgs e)
		{
			Window1.ShowIcon = true;
		}

		/// <summary>
		/// Handles the Click event of the HideIcon control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
		private void HideIcon_Click(object sender, RoutedEventArgs e)
		{
			Window1.ShowIcon = false;
		}

		*/
        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			App myapp = (App)System.Windows.Application.Current;

			// If there's a string forwarded by the 
			if (myapp.szFileOpenFromCmdLine.Length > 0)
			{

			}

			Menu_RefreshWindows();
        }

		private System.Collections.Specialized.StringCollection GetMRU()
		{
            if (Properties.Settings.Default.MRU == null)
            {
                Properties.Settings.Default.MRU = new System.Collections.Specialized.StringCollection();
                Properties.Settings.Default.Save();
            }

            return Properties.Settings.Default.MRU;
        }

        /// <summary>
        /// Handles the Click event of the File 'Save' menu item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FileSaveConfig(object sender, RoutedEventArgs e)
        {
			// Loop through the child windows and save the parameters
			// only if there are actually children available.
			if (Container.Children.Count > 0)
			{
				Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();

				var MRU = GetMRU();

				// Populate the file dialog with the most recently 
				// accessed file name before displaying to the user.
				if (MRU.Count > 0)
					saveFileDialog.FileName = MRU[0];

                // Set the file extension filter (optional)
                saveFileDialog.Filter = "Json Files (*.json)|*.json|All Files (*.*)|*.*";

				// Show the dialog and check if a file was selected
				if (saveFileDialog.ShowDialog() == true)
				{
					// Get the path chosen by the user
					string filePath = saveFileDialog.FileName;
                    List<CameraCalibParams> listCameraParams = new List<CameraCalibParams>();

					// Loop through all of the current windows that are open
					// and get the camera parameters for each.
                    foreach (MdiChild mdiChild in Container.Children)
                    {
                        // Get the video control
                        VideoUserControl vc = (VideoUserControl)mdiChild.Content;
						// Add it's camera parameters to the list.  They might be empty
                        listCameraParams.Add(vc.cameraParams);
						vc.PauseStream.IsChecked = true;
                    }

                    // Finally, save the data
                    // Convert to a json string
                    string json = JsonSerializer.Serialize(listCameraParams, new JsonSerializerOptions { WriteIndented = true });

                    File.WriteAllText(filePath, json);

					// If this is our first time saving, we should add it to 
					// the MRU.  The MRU will know if we've already opened
					// this file.
					AddToMRU(filePath);
                }
            }
        }

		private bool TryToOpenFile(string filename)
		{
			string filePath = "";
			string json = "";

            List<CameraCalibParams> cameraParams = new List<CameraCalibParams>();
            try
            {
                filePath = filename;
                json = File.ReadAllText(filePath);
                cameraParams = JsonSerializer.Deserialize<List<CameraCalibParams>>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
            finally
            {
                if (filePath.Length > 0)
                {
                    AddToMRU(filePath);
                    Menu_RefreshWindows();
                }
                ooo = 0;
                foreach (CameraCalibParams param in cameraParams)
                {
                    VideoUserControl vc = new VideoUserControl();
                    vc.cameraParams = param;

                    MdiChild myChild = new MdiChild
                    {
                        Title = "Video Window" + ooo++,
                        Content = vc,
                        Width = 256,
                        Height = 256,
                        Position = new Point(300 + (ooo - 1) * 20, 80 + (ooo - 1) * 40)
                    };
                    Container.Children.Add(myChild);
                }

				Container.MdiLayout = WPF.MDI.MdiLayout.TileVertical;
            }
            return true;
		}

        /// <summary>
        /// Handles the Click event of the File 'Open' menu item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        /// 
        /// The file opened will be a text file that contains the calibration information and RTSP stream URLs.
        /// For each URL, we will have a window opened.  The file should automatically be saved each time a change is made.
        private void FileOpenConfig(object sender, RoutedEventArgs e)
		{
            List<CameraParams> cameraParams = new List<CameraParams>();

            if (Container.Children.Count > 0)
			{
				MessageBoxResult rval;

				rval = System.Windows.MessageBox.Show("You currently have videos running.  "
					+ "Opening a file will terminate that measuring session.  Are you sure "
					+ "you want to do this?"
					, "File Open"
					, MessageBoxButton.YesNo);
				if (rval == MessageBoxResult.No)
					return;
				else
				{
					Container.Children.Clear();
				}
			}

			Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
			//Microsoft.Win32.SaveFileDialog openFileDialog = new Microsoft.Win32.SaveFileDialog();

			var MRU = GetMRU();


			// Populate the file dialog with the most recently 
			// accessed file name before displaying to the user.
			
			if (MRU.Count > 0)
			{
				openFileDialog.InitialDirectory = Path.GetDirectoryName(MRU[0]);
                //openFileDialog.FileName = Path.GetFileName(MRU[0]);
				openFileDialog.CheckFileExists = true;
				openFileDialog.CheckPathExists = true;
				openFileDialog.Title = "Open Camera JSON File";
			}

			// Set the file extension filter (optional)
			openFileDialog.Filter = "Json Files (*.json)|*.json|All Files (*.*)|*.*";

			// Show the dialog and check if a file was selected
			if (openFileDialog.ShowDialog() == true)
			{
				TryToOpenFile(openFileDialog.FileName);
            }
		}


		/// <summary>
		/// Removes a file from the MRU list, if it exists in the list, and saves it to the settings
		/// </summary>
		/// <param name="filePath">The path of the item to be added</param>
		/// 
		private void RemoveFromMRU(string filePath)
		{
			var recentFiles = Properties.Settings.Default.MRU;

			// Is the file already in the list?
			if (recentFiles.Contains(filePath))
				recentFiles.Remove(filePath);

            Properties.Settings.Default.MRU = recentFiles;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Adds a new file to the MRU list and saves it
        /// </summary>
        /// <param name="filePath">The path of the item to be added</param>
        /// 
        private void AddToMRU(string filePath)
		{
            var recentFiles = Properties.Settings.Default.MRU;

			// Is the file already in the list?
            if (!recentFiles.Contains(filePath))
            {
				const int maxMRU = 10;

				// If the list exceeds the max number of entries,
				// remove the oldest item.
				if (recentFiles.Count >= maxMRU)
				{
					// Remove the last (oldest) item
					recentFiles.RemoveAt(recentFiles.Count - 1);
				}

				// Add the new file to the top
				recentFiles.Insert(0,filePath);

				// Save the updated MRU list to settings
				Properties.Settings.Default.MRU = recentFiles;
				Properties.Settings.Default.Save();
            }
			else
			{
				// Put us at the top, even if we've been found
				recentFiles.Remove(filePath);
				recentFiles.Insert(0, filePath);
            
				// Save the updated MRU list to settings
                Properties.Settings.Default.MRU = recentFiles;
                Properties.Settings.Default.Save();
            }
        }


		private void OpenConfig_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			try
			{
				FileOpenConfig(sender, e as RoutedEventArgs);
            }
            catch (Exception ex) 
			{ 
				Debug.WriteLine(ex.ToString());
			}

		}

        private void SaveConfig_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                FileSaveConfig(sender, e as RoutedEventArgs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

        }

		private void StartAllCalib_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			if (Container.Children.Count < 0)
			{
                System.Windows.MessageBox.Show("The current configuration " +
                    "has no cameras.  Select Add..Video Window " +
                    "and enter a valid RTSP stream, or open a new configuration.");
                return;
            }
			foreach (MdiChild child in Container.Children)
			{
				VideoUserControl userControl = child.Content as VideoUserControl;
				if (userControl != null) 
				{
					// Turn on all the calibration streams at once
					userControl.CalibrateStream.IsChecked = true;
				}
			}
        }

        private void StartCurCalib_Executed(object sender, ExecutedRoutedEventArgs e)
        {
			if (Container.ActiveMdiChild != null)
			{
				MdiChild child = Container.ActiveMdiChild as MdiChild;
				if (child != null)
				{
					VideoUserControl userControl = child.Content as VideoUserControl;
					if (userControl != null) 
					{
						userControl.CalibrateStream.IsChecked = true;
					}
				}
			}
        }

        private void StopCalib_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Container.Children.Count < 0)
            {
                System.Windows.MessageBox.Show("The current configuration " +
					"has no cameras.  Select Add..Video Window " +
					"and enter a valid RTSP stream, or open a new configuration.");
                return;
            }
            foreach (MdiChild child in Container.Children)
            {
                VideoUserControl userControl = child.Content as VideoUserControl;
                if (userControl != null)
                {
                    // Turn on all the calibration streams at once
                    userControl.CalibrateStream.IsChecked = false;
                }
            }
        }

        private void PrintTargets_Executed(object sender, ExecutedRoutedEventArgs e)
        {
			// Launch a child window to print the targets as svg files and convert them
			// directly to pdf.

			PrintAprilTagDlg dlg = new PrintAprilTagDlg();
			if (dlg.ShowDialog() == true)
			{
                // string userInput = dlg.UserInput;
                // System.Windows.MessageBox.Show("You entered: " + userInput);
            }
        }

        private void MRU_Executed(object sender, ExecutedRoutedEventArgs e)
        {
			string tstr = (sender as System.Windows.Controls.MenuItem).Header.ToString();

			// Find the first space and lop off the number
			int i = tstr.IndexOf(" ");
			if (i >0)
				tstr = tstr.Substring(i+1);
			if (TryToOpenFile(tstr) == false)
			{
                System.Windows.MessageBox.Show("The path "+tstr+" could not be opened.  It will be removed from the most recent files list.");
				RemoveFromMRU(tstr);
				Menu_RefreshWindows();
            }
        }

        private void NewMapWindow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
			// Launch a child window to print the targets as svg files and convert them
			// directly to pdf.
			AddMapWindow_Click(sender, e as RoutedEventArgs);
        }




        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
			foreach (MdiChild w in Container.Children)
			{
				if (w.Content is VideoUserControl)
				{
					VideoUserControl vuCtl = w.Content as VideoUserControl;
					vuCtl.PauseAndKillThreads();
				}
			}

			dlgSearchSpecificTag.ForceCloseWindow();
			dlgSearchSpecificTag = null;

            Container.Children.Clear();
            // Close all the windows and wait for them to shut down
            e.Cancel = false;
        }

        private void SearchSpecificTag_Click(object sender, RoutedEventArgs e)
        {
			// Launch a modeless search window to live locate a specific
			// id number
			dlgSearchSpecificTag.Show();
            dlgSearchSpecificTag.Owner = this;

            dlgSearchSpecificTag.Focus();
        }
    }
}