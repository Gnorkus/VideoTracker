using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Numerics;
using Emgu.CV.Aruco;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Web;
using VideoTrack.Model;
using WPF.MDI;
using Npgsql;

namespace VideoTrack.Controls
{
    /// <summary>
    /// Interaction logic for VideoUserControl.xaml
    /// </summary>
    public partial class VideoUserControl : System.Windows.Controls.UserControl
    {
        // Classwide Variables
        const int RINGBUFFER_NUM_ELEMENTS = 8;
        const double MAX_SECONDS_UNTIL_CALIB = 15;
        const int MAX_CORNERS_LIST_COUNT = 150;

        private System.Windows.Point _lastDragPoint;
        private double _scaleFactor = 1.0;
        private Double zoomMax = 5;
        private Double zoomMin = 0.5;
        private Double zoomSpeed = 0.001;
        private Double zoom = 1;
        private bool m_bCheckForCalib;

        VideoCapture m_capture;
        BitmapImage bitmap;
        Thread processingThread;
        Thread readingThread;
        CancellationTokenSource cancellationTokenSource;
        public CameraCalibParams cameraParams = new CameraCalibParams();
        bool bInCancel = false;

        private double m_dLastScrollH = -999999;
        private double m_dLastScrollV = -999999;
        System.Windows.Point m_ptAnchor;

        public System.Collections.Generic.Dictionary<int, MarkerPair> m_listCurrentMarker = new System.Collections.Generic.Dictionary<int, MarkerPair>();

        VectorOfPointF _checkerCorners;

        VectorOfVectorOfPointF myMarkerCorners = new VectorOfVectorOfPointF();
        VectorOfVectorOfPointF myRejects = new VectorOfVectorOfPointF();
        VectorOfInt myMarkerIds = new VectorOfInt();
        Dictionary myDict = new Dictionary(Dictionary.PredefinedDictionaryName.DictAprilTag36h10);

        Dictionary<int, MCvPoint3D32f> mappts = new Dictionary<int, MCvPoint3D32f>();
        DetectorParameters myDetectorParams = DetectorParameters.GetDefault();

        VectorOfFloat rotationVectors = new VectorOfFloat();
        VectorOfFloat translationVectors = new VectorOfFloat();

        List<VectorOfPointF> _checkerCornersList = new List<VectorOfPointF>();
        List<VectorOfPointF> _checkerCornersTempList = new List<VectorOfPointF>();
        MCvPoint3D32f[][] _cornersObjectList;
        PointF[][] _cornersPointsList;
        public Matrix<double> _cameraMatrix = new Matrix<double>(3, 3);
        public Matrix<double> _distortionMatrix = new Matrix<double>(5, 1);
        Mat[] _rotationVectors;
        Mat[] _translationVectors;
        //System.Numerics.Quaternion[] _quaternions;
        Mat _greyMat;
        Mat _colorMat;

        System.Drawing.Size _patternSize = new System.Drawing.Size(9, 7);  // Size(w,h)
        float _sqW = 0.9989f;
        float _sqH = 0.9945f;
        //float _sqW = 2.978f;
        //float _sqH = 2.973f;

        Mat[] m = new Mat[RINGBUFFER_NUM_ELEMENTS];
        int nCurMat = 0;
        int nCurDisplayMat = 0;
        bool _bCalibrationComplete = false;
        bool _bSnapshot = false;
        bool m_bIsPaused = false;
        string RTSPUrl = "";

        // Each time we instantiate a video window, we create
        // a new connection to the required databases.  This is
        // because Npgsql is not thread safe.
        //NpgsqlConnection m_Conn = new NpgsqlConnection(Static.defaultConnString);
        //Camera m_CameraDB = null;
        //AssetLocationHistory m_AssetDB = null;
        //LocationState m_LocationStateDB = null;

        // This loops through all of the positions of the objects we've found.
        // We'll be checking on the distance changed for the markers to see if it exceeds
        // a certain base tolerance.  If it does, then we will store the new position.
        // Otherwise, we will update just the time stamp of the last known position.
        void WritePositionsToDatabase()
        {
            try
            {
                //m_Conn.Open();

                // Check to see if the position database exists or not
                // If it doesn't then
                // Debug.WriteLine($"The PostgreSQL version: {conn.PostgreSqlVersion}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    Debug.WriteLine(ex.InnerException.Message);
            }
            finally
            {
                //m_Conn.Close();
            }
        }

        public VideoUserControl()
        {
            cancellationTokenSource = new CancellationTokenSource();
            InitializeComponent();

            // Create our own image buffer
            for (int i = 0; i < RINGBUFFER_NUM_ELEMENTS; i++)
                m[i] = new Mat(1616, 2880, DepthType.Cv8U, 3);

            Loaded += VideoUserControl_Loaded;
            Unloaded += VideoUserControl_Unloaded;
            LayoutUpdated += VideoUserControl_LayoutUpdated;

            // Attach the necessary event handlers
            RTSPStream.TextChanged += RichTextBox_TextChanged;
            RTSPStream.PreviewKeyDown += RichTextBox_PreviewKeyDown;
            RTSPStream.SelectionChanged += RichTextBox_SelectionChanged;

            // Due to limitations, we cannot simply initialize in the
            // constructor.  
           // m_CameraDB = new Camera();
           // m_CameraDB.Initialize(ref m_Conn);

           // m_AssetDB = new AssetLocationHistory();
           // m_AssetDB.Initialize(ref m_Conn);

           // m_LocationStateDB = new LocationState();
           // m_LocationStateDB.Initialize(ref m_Conn);

            // Kill the database table so we can create it again
            // to be compatible with MSSQL server
            //m_CameraDB.DropTable();
        }

        private void VideoUserControl_LayoutUpdated(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
            return;
        }

        public static DependencyObject GetParent(DependencyObject element)
        {
            // Get the parent element by traversing up the visual tree
            return VisualTreeHelper.GetParent(element);
        }
        public static T FindParent<T>(DependencyObject element) where T : DependencyObject
        {
            DependencyObject parent = GetParent(element);
            while (parent != null)
            {
                if (parent is T)
                {
                    return (T)parent;
                }
                parent = GetParent(parent);
            }
            return null;
        }


        // Instantiate the VideoCapture from OpenCV (it's really Emgu.CV),
        // initialize the ImageGrabbed event, and start the VideoCapture
        private void VideoUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            string tstr;

            _bCalibrationComplete = CheckCalibrationMatricesLoaded();

            this.BorderThickness = new System.Windows.Thickness(1.0);

            tstr = this.cameraParams.szRTSPStreamURL;
            ScaleTransform st = ViewBox_CanvasMain.LayoutTransform as ScaleTransform;
            if (st == null)
            {
                st = new ScaleTransform();
                ViewBox_CanvasMain.LayoutTransform = st;
            }

            if (ViewBox_CanvasMain.ActualWidth > 0 && ViewBox_CanvasMain.ActualHeight > 0)
            {
                double dHeightRatio = (double)ViewBox_CanvasMain.ActualHeight / 1616.0;
                double dWidthRatio = (double)ViewBox_CanvasMain.ActualWidth / 2880.0;
                if (dHeightRatio > dWidthRatio)
                {
                    dSmallestZoom = dWidthRatio;
                }
                else
                {
                    dSmallestZoom = dHeightRatio;
                }
            }
            else
            {
                dSmallestZoom = dSmallestZoom;
            }

            Transform rt = ViewBox_CanvasMain.RenderTransform;
            st.ScaleX = st.ScaleY = dSmallestZoom;

            // Prepare to populate the RTSPStream richtext edit box
            // with the current parameter stored in cameraParams
            RTSPStream.Document.Blocks.Clear();
            if (tstr != null && tstr.Length > 0 && IsValidURL(tstr))
            {
                RTSPStream.Document.Blocks.Add(new Paragraph(new Run(tstr)));
                RTSPStream.IsEnabled = false;
                StartStream();
            }
            else
            {
                RTSPStream.Document.Blocks.Add(new Paragraph(new Run("Please enter a valid URL and uncheck the Pause checkbox.")));
                RTSPStream.IsEnabled = false;
                PauseStream.IsChecked = true;
            }

            processingThread = new Thread(() => MainWindow_ProcessVideoThread(cancellationTokenSource.Token));
            processingThread.Name = "VideoUserControl_ProcessVideoThread";

            readingThread = new Thread(() => MainWindow_ReadVideoThread(cancellationTokenSource.Token));
            readingThread.Name = "VideoUserControl_ReadVideoThread";

            processingThread.Start();
            readingThread.Start();

        }

        public void PauseAndKillThreads()
        {
            if (!bInCancel)
            {
                bInCancel = true;

                PauseStreamCapture();

                // Tell the threads to stop
                cancellationTokenSource.Cancel();

                // Dispose of the cancellation token 
                cancellationTokenSource.Dispose();

                // Finally continue to exit main process
            }
        }


        // Capture a request to close the main window, and start the
        // safe thread shutdown procedure.
        private void VideoUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            PauseAndKillThreads();
        }

        private void VideoUserControl_SnapshotClicked(object sender, RoutedEventArgs e)
        {
            _bSnapshot = true;
        }




        private void OnStartStream(object sender, EventArgs e)
        {
            StartStream();
        }

        private void StartStream()
        {
            RTSPStream.IsEnabled = false;
            //Thread.Sleep(150);
            RTSPUrl = new TextRange(RTSPStream.Document.ContentStart, RTSPStream.Document.ContentEnd).Text;
            RTSPUrl = RTSPUrl.Replace("\r\n", "");


            RTSPStream.IsEnabled = false;
            m_bIsPaused = false;
        }

        private void PauseStreamCapture()
        {
            m_bIsPaused = true;
            RTSPStream.IsEnabled = true;
        }

        private void OnPauseStream(object sender, EventArgs e)
        {
            PauseStreamCapture();
        }


        private void RTSPStream_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Make sure we test if the URL is valid.  If it is, we can
            // save it.
            string tstr = new TextRange(RTSPStream.Document.ContentStart, RTSPStream.Document.ContentEnd).Text;
            if (tstr != null && tstr.Length > 0 && IsValidURL(tstr))
                cameraParams.szRTSPStreamURL = tstr;
        }

        // TextChanged Event (tracks changes when text is inserted or deleted)
        private void RichTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateRTSPParam();
        }

        // PreviewKeyDown Event (detects key presses such as delete, backspace, etc.)
        private void RichTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
                UpdateRTSPParam();
            //else
                //UpdateRTSPParam();
        }

        // SelectionChanged Event (tracks changes to the selection in the RichTextBox)
        private void RichTextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateRTSPParam();
        }

        public void MdiChild_Closing(object sender, RoutedEventArgs e)
        {
            if ((e as WPF.MDI.ClosingEventArgs).Cancel) 
                return;
        }

        double offsetY = 0;
        double offsetX = 0;
        double dSmallestZoom = 1.0;

        private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers.Equals(ModifierKeys.Shift))
            {
                // Shift handles the left right panning
                if (e.Delta < 0)
                {
                    MainScrollViewer.LineLeft();
                    e.Handled = true;
                }
                else
                {
                    MainScrollViewer.LineRight();
                    e.Handled = true;
                }
            }
            else
            if (Keyboard.Modifiers.Equals(ModifierKeys.Control))
            {
                if (_PictureBox.IsMouseOver)
                {

                    System.Windows.Point mouseAtImage = e.GetPosition(_PictureBox); // MainScrollViewer.TranslatePoint(middleOfScrollViewer, _PictureBox);
                    System.Windows.Point mouseAtScrollViewer = e.GetPosition(MainScrollViewer);

                    ScaleTransform st = ViewBox_CanvasMain.LayoutTransform as ScaleTransform;
                    if (st == null)
                    {
                        st = new ScaleTransform();
                        ViewBox_CanvasMain.LayoutTransform = st;
                    }

                    if (e.Delta > 0)
                    {
                        st.ScaleX = st.ScaleY = st.ScaleX * 1.25;
                        if (st.ScaleX > 64) st.ScaleX = st.ScaleY = 64;
                    }
                    else
                    {
                        // What's the smallest possible st.ScaleX and st.ScaleY?
                        // Calculate using the ViewBox_CanvasMain's size and
                        // the _PictureBox size.
                        if (ViewBox_CanvasMain.Width>0 && ViewBox_CanvasMain.Height>0
                            && _PictureBox.Width>0 && _PictureBox.Height > 0)
                        {
                            double dHeightRatio = (double)ViewBox_CanvasMain.Height / (double)_PictureBox.Height;
                            double dWidthRatio = (double)ViewBox_CanvasMain.Width / (double)_PictureBox.Width;
                            if (dHeightRatio > dWidthRatio)
                            {
                                dSmallestZoom = dWidthRatio;
                            }
                            else
                            {
                                dSmallestZoom = dHeightRatio;
                            }
                        }
                        else
                        {
                            dSmallestZoom = dSmallestZoom;
                        }

                        Transform rt = ViewBox_CanvasMain.RenderTransform ;
                        st.ScaleX = st.ScaleY = st.ScaleX / 1.25;
                    }
                    #region [this step is critical for offset]
                    MainScrollViewer.ScrollToHorizontalOffset(0);
                    MainScrollViewer.ScrollToVerticalOffset(0);
                    this.UpdateLayout();
                    #endregion

                    System.Windows.Vector offset = _PictureBox.TranslatePoint(mouseAtImage, MainScrollViewer) - mouseAtScrollViewer; // (Vector)middleOfScrollViewer;
                    MainScrollViewer.ScrollToHorizontalOffset(offset.X);
                    MainScrollViewer.ScrollToVerticalOffset(offset.Y);
                    this.UpdateLayout();
                }
            }
            else
            {
                // No modifier key handles the up down panning
                if (e.Delta < 0)
                {
                    MainScrollViewer.LineDown();
                    e.Handled = true;
                }
                else
                {
                    MainScrollViewer.LineUp();
                    e.Handled = true;
                }
            }
        }

        private void CalibrateStream_Checked(object sender, RoutedEventArgs e)
        {
            m_bCheckForCalib = true;
        }

        private void CalibrateStream_Unchecked(object sender, RoutedEventArgs e)
        {
            m_bCheckForCalib = false;
        }

        private void MainScrollViewer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Get the current state of the scroll positions if they're visible.
            //MdiChild myParent = FindParent<MdiChild>(this);

            //if (myParent != null)
            //{
            //    myParent
            //}
            m_ptAnchor = e.GetPosition(MainScrollViewer);
            

            if (MainScrollViewer.ComputedHorizontalScrollBarVisibility == Visibility.Visible) 
                m_dLastScrollH = MainScrollViewer.HorizontalOffset;
            else
                m_dLastScrollH = -999999;

            if (MainScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                m_dLastScrollV = MainScrollViewer.VerticalOffset;
            else
                m_dLastScrollV = -999999;
        }

        private void MainScrollViewer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Get the mouse position relative to the main scroll view window
                // inside the videousercontrol.
                System.Windows.Point pt = e.GetPosition(MainScrollViewer);

                if (m_dLastScrollH != -999999)
                {
                    MainScrollViewer.ScrollToHorizontalOffset(m_dLastScrollH - pt.X + m_ptAnchor.X);
                }

                if (m_dLastScrollV != -999999)
                {
                    MainScrollViewer.ScrollToVerticalOffset(m_dLastScrollV - pt.Y + m_ptAnchor.Y);
                }

                /*
                // Display or use the transformed position
                SKPoint diff = _lastPanPosition - scaledPosition;
                diff.X /= _scale;
                diff.Y /= _scale;
                _translation -= diff;
                _lastPanPosition = scaledPosition;

                SkiaCanvas.InvalidateVisual(); // Redraw the canvas
                */
            }
        }

    }
}
