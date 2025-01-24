using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Emgu.CV.Aruco;
using Emgu.CV.ML;
using static Emgu.CV.Dai.OpenVino;
using System.Web.UI.WebControls;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel;

namespace VideoTrack
{
    /// <summary>
    /// Interaction logic for PrintAprilTagDlg.xaml
    /// </summary>
    public partial class PrintAprilTagDlg : Window, INotifyPropertyChanged
    {
        Dictionary myDict = new Dictionary(Dictionary.PredefinedDictionaryName.DictAprilTag36h10);


        public double m_dEdgeWidth = 3.5;
        public double m_dMinColWidth = 0.25;
        public double m_dMinBorderTB = 0.5;
        public double m_dMinBorderLR = 0.5;
        public double EdgeWidth { get { return m_dEdgeWidth; } set { m_dEdgeWidth = value; } }
        public double MinColWidth { get { return m_dMinColWidth; } set { m_dMinColWidth = value; } }
        public double MinBorderTB { get { return m_dMinBorderTB; } set { m_dMinBorderTB = value; } }
        public double MinBorderLR { get { return m_dMinBorderLR; } set { m_dMinBorderLR = value; } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }






        public PrintAprilTagDlg()
        {
            InitializeComponent();
        }


        public string CreateTwoSquareSvg(double sideInches
                                      , int nLowMatInd
                                      , int nHighMatInd
                                      , string szLabel= ""
                                      , double topMargin = 0.3125
                                      , double leftMargin = 0.75
                                      , double rightMargin = 0.3125
                                      , double bottomMargin = 0.3125    
                                      , double docWidth=8.5
                                      , double docHeight=11.0
                                        )
        {
            int i, j ;

            Emgu.CV.Mat low = new Emgu.CV.Mat();
            Emgu.CV.Mat hi = new Emgu.CV.Mat();

            myDict.GenerateImageMarker(nLowMatInd, 8, low);
            myDict.GenerateImageMarker(nHighMatInd, 8, hi);

            double dPixSize = sideInches / (double)low.Cols;
            double dDivider = docWidth - 2.0*sideInches - leftMargin - rightMargin;
            double dTextX = leftMargin + sideInches + dPixSize / 2.0;
            double dTextY = topMargin + sideInches + 0.1875;

            if (dDivider < dPixSize*0.75)
            {
                System.Windows.Forms.MessageBox.Show("Please choose a different size for the target square.  "+
                                                     "A minimum column separation of one pattern bit can not be maintained.");
                return null;
            }

            string svgContent = "";

            double dRowInch = topMargin;
            double dColInchLeft = leftMargin;
            double dColInchRight = leftMargin;
            double dPosL, dPosR, dxL, dxR, dy;
            int nBit;

            dPosL = leftMargin;
            dPosR = leftMargin + dPixSize + sideInches;

            for (j = 0, dy=topMargin; j < low.Rows; j++,dy+=dPixSize)
            {
                for (i = 0, dxL = dPosL, dxR = dPosR; i < low.Cols; i++, dxL+=dPixSize, dxR+=dPixSize)
                {
                    nBit = (byte)(low.GetData().GetValue(j,i));

                    svgContent += "<rect width=\"" + dPixSize.ToString("F4") + "in\" "
                            + "height=\"" + dPixSize.ToString("F4") + "in\" "
                            + "x=\"" + dxL.ToString("F4") + "in\" "
                            + "y=\"" + dy.ToString("F4") + "in\" "
                            + "rx=\"0in\" ry=\"0in\" fill=\""
                            + ((nBit == 0) ? "black" : "white") + "\" />\r\n";

                    nBit = (byte)(hi.GetData().GetValue(j, i));

                    svgContent += "<rect width=\"" + dPixSize.ToString("F4") + "in\" "
                            + "height=\"" + dPixSize.ToString("F4") + "in\" "
                            + "x=\"" + dxR.ToString("F4") + "in\" "
                            + "y=\"" + dy.ToString("F4") + "in\" "
                            + "rx=\"0in\" ry=\"0in\" fill=\""
                            + ((nBit == 0) ? "black" : "white") + "\" />\r\n";
                }
            }

            if (szLabel.Length > 0)
                svgContent += "<text x=\"" + dTextX.ToString() 
                    + "in\" y=\"" + dTextY.ToString() 
                    + "in\" class=\"small\">" + szLabel + "</text>";

            return svgContent;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            string fileContents = InputTextBox.Text;
            char[] delimiterChars = { ',', '\t', '\n', '\r' };
            string[] numbers = fileContents.Split(delimiterChars);
            List<string> validnumbers = new List<string>();
            string szRootPath = Properties.Settings.Default.AprilTagSavePath;
            string szFilePath="";
            string svgFileName;
            string svgContent = "";
            int nLowMatInd, nHighMatInd;
            int j;

            // Figure out what really are numbers or not.
            // It could be that some have a zero length.
            for (j=0; j<numbers.Length; j++)
            {
                if (numbers[j].Length > 0)
                    validnumbers.Add(numbers[j].Trim());
            }



            if (SingleMarker.IsChecked == false) 
            {
                for (j = 0; j < validnumbers.Count; j +=  (bool)SingleMarker.IsChecked ? 1 : 3 )
                {
                    svgFileName = "";
                    svgContent = "<svg version = \"1.1\" width = \"8.5in\" height = \"11in\" viewbox = \"0in 0in 8.5in 11in\" xmlns = \"http://www.w3.org/2000/svg\">\r\n";
                    svgContent += "<style> .small { font: 0.125in sans-serif; text-anchor: middle; } </style>\r\n";

                    // Now that we have numbers, let's split them up into the top
                    // and bottom print patterns.
                    if (validnumbers.Count - j == 1)
                    {
                        svgFileName = szRootPath + "/" + validnumbers[j].Trim();
                    }
                    else
                    if (validnumbers.Count - j == 2)
                    {
                        svgFileName = szRootPath + "/" + validnumbers[j] + "-" + validnumbers[j + 1];
                    }
                    if (validnumbers.Count - j == 3)
                    {
                        svgFileName = szRootPath + "/" + validnumbers[j] + "-" + validnumbers[j + 1] + "-" + validnumbers[j + 2];
                    }


                    if (validnumbers.Count - j >= 1)
                    {
                        nLowMatInd = int.Parse(validnumbers[j]) % 1000;
                        nHighMatInd = int.Parse(validnumbers[j]) / 1000 + 1000;
                        svgContent += CreateTwoSquareSvg(3.25   // side in inches
                                            , nLowMatInd // lo
                                            , nHighMatInd // hi
                                            , validnumbers[j]
                                            , 0.3125
                                            );
                    }

                    if (validnumbers.Count - j >= 2)
                    {
                        nLowMatInd = int.Parse(validnumbers[j + 1]) % 1000;
                        nHighMatInd = int.Parse(validnumbers[j + 1]) / 1000 + 1000;
                        svgContent += CreateTwoSquareSvg(3.25   // side in inches
                                            , nLowMatInd // lo
                                            , nHighMatInd // hi
                                            , validnumbers[j + 1]
                                            , 0.3125 + 3.25 + 0.3125
                                            );
                    }

                    if (validnumbers.Count - j >= 3)
                    {
                        nLowMatInd = int.Parse(validnumbers[j + 2]) % 1000;
                        nHighMatInd = int.Parse(validnumbers[j + 2]) / 1000 + 1000;
                        svgContent += CreateTwoSquareSvg(3.25   // side in inches
                                            , nLowMatInd // lo
                                            , nHighMatInd // hi
                                            , validnumbers[j + 2]
                                            , 0.25 + 3.25 + 0.3125 + 3.25 + 0.3125
                                            );
                    }
                    svgContent += "</svg>";

                    System.IO.File.WriteAllText(svgFileName + ".svg", svgContent);
                    szFilePath = "cairosvg \"" + svgFileName + ".svg\" -o " + "\"" + svgFileName + ".pdf\"";

                    // Now that we created the svg file, let's
                    // convert it to a pdf
                    RunCommand(szFilePath);
                }

            }
            else
            {

            }

        }

        static void RunCommand(string command)
        {
            // Create a new ProcessStartInfo object to configure the process
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",           // Command prompt
                Arguments = $"/C {command}",    // "/C" runs the command and then closes cmd.exe
                RedirectStandardOutput = true,  // Capture standard output
                UseShellExecute = false,       // Do not use the shell to execute the command
                CreateNoWindow = true          // Do not show a command prompt window
            };

            try
            {
                // Start the process
                using (Process process = Process.Start(startInfo))
                {
                    // Read the output of the command
                    string output = process.StandardOutput.ReadToEnd();

                    // Wait for the process to exit
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., if the process couldn't be started)
                //Console.WriteLine($"An error occurred: {ex.Message}");
                Debug.WriteLine(ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Select Folder",
                CheckFileExists = false,
                ValidateNames = false,
                FileName = "Folder Selection",
                Filter = "Folders|*.dummy",
                InitialDirectory = Properties.Settings.Default.AprilTagSavePath
            };

            if (folderDialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)
            {
                // Get the selected folder path
                string selectedFolderPath = System.IO.Path.GetDirectoryName(folderDialog.FileName);
                SavePath.Text = selectedFolderPath;
                Properties.Settings.Default.AprilTagSavePath = selectedFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            // Create a new OpenFileDialog instance
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();

            // Set the file filter (optional)
            openFileDialog.Filter = "Text Files (*.txt,*.csv)|*.txt;*.csv|All Files (*.*)|*.*";

            // Show the dialog and check if the user selected a file
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                // Get the selected file path
                string filePath = openFileDialog.FileName;
                string fileContents = File.ReadAllText(filePath);
                InputTextBox.Text = fileContents;
            }
        }

        private void PrintAprilTagsDlg_Loaded(object sender, RoutedEventArgs e)
        {
            SavePath.Text=Properties.Settings.Default.AprilTagSavePath;
        }
    }
}
