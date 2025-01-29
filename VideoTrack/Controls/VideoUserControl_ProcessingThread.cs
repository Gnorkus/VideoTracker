using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Freetype;
using Emgu.CV;
using System;
using System.Numerics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Media.Media3D;
using Emgu.CV.Reg;
using System.Windows;
using System.Drawing.Drawing2D;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.CodeDom;
using Npgsql;

namespace VideoTrack.Controls
{
    public partial class VideoUserControl : System.Windows.Controls.UserControl
    {
        public System.Drawing.Size _imgSize;
        DateTime _curDateTime = DateTime.Now;
        DateTime _lastDateTime = DateTime.Now;
        TimeSpan _curLastTimeDiff;
        double _dSecondsUntilCalib = 0;
        string tstrCountDown;
        string tstr;
        bool bFound;
        double _error;
        double _dListInc = 0;
        double _dCnt = 0;
        //double _nFeet = 0;
        //double _nInches = 0;
        double[] values = new double[3];
        Mat axisAngle;
        bool bUseRotatableText = false;

        Mat m_arrRotVecs = new Mat();
        Mat m_arrTransVecs = new Mat();
        Mat m_arrRotMatrices = new Mat();
        List<Quaternions> Qs = new List<Quaternions>();
        List<MarkerPair> listMarkersToRemove = new List<MarkerPair>();
        List<MarkerPair> listMarkersToDraw = new List<MarkerPair>();



        public async void PaintOntoPictureBox ( Emgu.CV.Mat img )
        {
            BitmapImage bitmap;

            using (var stream = new MemoryStream())
            {
                // Convert an OpenCV (Emgu) Mat to a bitmap file stream
                img.ToImage<Bgr, byte>().ToBitmap().Save(stream, ImageFormat.Bmp);

                // Create a bitmap image and copy the file stream to the bitmap imagee
                bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(stream.ToArray());
                bitmap.EndInit();

                // Finally, display the bitmap image in the main window's _PictureBox.
                // A _PictureBox is an System.Windows.Controls.Image object.
                bitmap.Freeze();

                if (Static.bCopyToPictureBox)
                {
                    try
                    {
                        await _PictureBox.Dispatcher.InvokeAsync(() => { _PictureBox.Source = bitmap; }, System.Windows.Threading.DispatcherPriority.SystemIdle);
                    }
                    catch (Exception e) { }
                    finally { }
                }
            };
        }

        public void MainWindow_ProcessVideoThread_DrawMarkerTagIDs(
              ref Emgu.CV.Mat       matResultBitmap
            , ref List<MarkerPair>  listMarkersToDraw )
        {
            System.Drawing.Point pt ;

            foreach (MarkerPair m in listMarkersToDraw)
            {
                // Average of the corners
                pt = m.GetCenter();

                //CvInvoke.Polylines(_colorMat, m.GetPoly(), true, new MCvScalar(0, 255, 255), 7);
                CvInvoke.Polylines(matResultBitmap, m.GetPoly(2), false, new MCvScalar(0, 255, 255), 7);

                if (bUseRotatableText)
                {
                    // Get the polylines for the rotatable text.
                    List<List<System.Drawing.Point>> pts = ConvertFontToPolylines(m.PairID.ToString()
                                                                    , "Segoe UI"
                                                                    , 30f
                                                                    , m.UnderlineStart    // starting point of text baseline
                                                                    , m.UnderlineFinish); // ending point of text baseline

                    foreach (List<System.Drawing.Point> pts2 in pts)
                    {
                        CvInvoke.Polylines(matResultBitmap, pts2.ToArray(), false, new MCvScalar(255, 255, 255), 5);
                        CvInvoke.FillPoly(matResultBitmap, new VectorOfPoint(pts2.ToArray()), new MCvScalar(0, 0, 0));
                    }
                }
                else
                {
                    CvInvoke.PutText(matResultBitmap, m.PairID.ToString()
                        , m.BoxLeftCenter, FontFace.HersheyPlain, 1.5
                        , new MCvScalar(255, 255, 255, 255), 10);

                    CvInvoke.PutText(matResultBitmap, m.PairID.ToString()
                        , m.BoxLeftCenter, FontFace.HersheyPlain, 1.5
                        , new MCvScalar(0, 0, 0, 255), 1);
                }
            }

        }

        public void MainWindow_ProcessVideoThread_DetectMarkers()
        {
            int i, j;
            System.Drawing.Point pt = new System.Drawing.Point();
            System.Numerics.Vector2 v = new System.Numerics.Vector2();

            if (_bCalibrationComplete)
                tstrCountDown = "Calibrated";
            else
                tstrCountDown = "";

            // Before converting to bitmap, find the markers
            Emgu.CV.Aruco.ArucoInvoke.DetectMarkers(_colorMat, myDict, myMarkerCorners, myMarkerIds, myDetectorParams, myRejects);
            axisAngle = new Mat();

            #region Draw detected markers
            if (myMarkerCorners.Size > 0)
            {
                System.Drawing.PointF[][] markerptfs = myMarkerCorners.ToArrayOfArray();
                double[] myMarkerAngles = new double[myMarkerCorners.Size];
                VectorOfVectorOfPoint polys = new VectorOfVectorOfPoint();

                if (_bCalibrationComplete)
                {
                    // Using the camera calibration values, this command
                    // takes the 2D positions of the calibration squares
                    // and turns them into 3D positions relative to the
                    // calibration of this particular camera.
                    Emgu.CV.Aruco.ArucoInvoke.EstimatePoseSingleMarkers(myMarkerCorners
                                                                        , 3.0f
                                                                        , _cameraMatrix
                                                                        , _distortionMatrix
                                                                        , m_arrRotVecs
                                                                        , m_arrTransVecs);
                    //myRotations = m_arrRotVecs.GetData();
                    /*
                    Qs.Clear();

                    // Try converting rotation to a quaternion.
                    for (i = 0; i < myMarkerIds.Size; i++)
                    {
                        using (Mat rvecMat = m_arrRotVecs.Row(i))
                        using (Mat tvecMat = m_arrTransVecs.Row(i))
                        using (Mat rmat = new Mat())
                        using (VectorOfDouble  rvec= new VectorOfDouble())
                        using (VectorOfDouble tvec = new VectorOfDouble())
                        {
                            VectorOfDouble myvec = new VectorOfDouble();

                            double[] values = new double[3];
                            rvecMat.CopyTo(values);
                            rvec.Push(values);
                            tvecMat.CopyTo(values);
                            tvec.Push(values);

                            // We now have a rotation and translation vector for each marker
                            // Convert to a Quaternion
                            CvInvoke.Rodrigues(rvec, rmat);

                            Quaternions Q = ConvertRotationToQuaternion(rmat);
                            Qs.Add(Q);
                        }
                    }*/
                }

                Mat rotationVector = new Mat(3,1,DepthType.Cv32F,1);
                Mat rotationMatrix = new Mat();



                //System.Numerics.Quaternion qval = new System.Numerics.Quaternion();
                //_quaternions = new System.Numerics.Quaternion[m_arrRotVecs.Rows];
                //Mat rotateion
                MCvPoint3D64f axis = new MCvPoint3D64f(0, 0, 0);

                /*
                if (m_arrRotVecs.Rows>0 && m_arrRotVecs.IsContinuous)
                {
                    for (i = 0; i < m_arrRotVecs.Rows; i++)
                    {
                        bool bval;

                        unsafe
                        {
                            IntPtr rvecsPtr = m_arrRotVecs.GetDataPointer();
                            double* rvecsArr = (double*)rvecsPtr.ToPointer();

                            IntPtr rotVecPtr = rotationVector.GetDataPointer();
                            float* rotVecArr = (float*)rotVecPtr.ToPointer();

                        }

                        myMarkerAngles[i] = ConvertRotationVectorToAxisAngle(ref rotationVector, ref axis);
                    }
                }*/

                // Calculate the angle of rotation of the marker.
                // We do this so that we can compare against the so-called 
                // neighbor marker.  If the neighbor marker is significantly
                // different as respects it's angle, then the candidate
                // neighbor is not valid.
                for (i = 0; i < markerptfs.Length; i++)
                {
                    v.Y = markerptfs[i][1].Y - markerptfs[i][0].Y;
                    v.X = markerptfs[i][1].X - markerptfs[i][0].X;
                    myMarkerAngles[i] = (Math.Atan2((double)(v.Y), (double)(v.X)) * 180.0 / Math.PI + 360.0) % 360;
                }

                // We need to determine if we have marker pairs.
                // How close are the markers to each other?
                // We will look at the first two elements of each 
                // boundary found. 

                for (i=0; i< markerptfs.Length; i++)
                {
                    // Take the 2nd point
                    System.Drawing.PointF fPt = markerptfs[i][0];
                    System.Drawing.PointF sPt = markerptfs[i][1];
                    System.Drawing.SizeF vec = new System.Drawing.SizeF(sPt.X-fPt.X, sPt.Y-fPt.Y);
                    float len = (float) Math.Sqrt(vec.Width*vec.Width + vec.Height*vec.Height);
                    float qlen = len / 4.0f;

                    for (j=0; j< markerptfs.Length; j++)
                    {
                        if (j != i)
                        {
                            try
                            {
                                // Make sure that we're pointing roughly in the same direction
                                double dAngle = Math.Abs(myMarkerAngles[i] - myMarkerAngles[j]);
                                if (dAngle < 10 || dAngle>350)
                                {
                                    // Make sure that the distance
                                    // The marker points are returned as an array of PointF
                                    //   e.g. PointF [n][4].  Each element of the 2nd dimension
                                    //   represents the corners of the apriltag box.  
                                    System.Drawing.PointF fPtQ = markerptfs[j][0];
                                    System.Drawing.PointF sPtQ = markerptfs[j][1];
                                    System.Drawing.SizeF vecQ = new System.Drawing.SizeF(sPt.X - fPt.X, sPt.Y - fPt.Y);
                                    float len2 = (float)Math.Sqrt(vec.Width * vec.Width + vec.Height * vec.Height);

                                    float distance = (float) Math.Sqrt(
                                                            Math.Pow((sPt.X - fPtQ.X), 2) 
                                                          + Math.Pow((sPt.Y - fPtQ.Y), 2));

                                    if (distance < qlen)
                                    {
                                        string myTitle = "";
                                        System.Windows.Controls.ContentControl myParent = this.Parent as System.Windows.Controls.ContentControl;
                                        if (myParent != null)
                                        {
                                            WPF.MDI.MdiChild myMDIChild = myParent.TemplatedParent as WPF.MDI.MdiChild;
                                            if (myMDIChild != null)
                                            {
                                                myMDIChild.Dispatcher.Invoke(() => myTitle = myMDIChild.Title);
                                            }
                                        }

                                        // Convert the translation vector from a Mat to MCvPoint3D64f
                                        MCvPoint3D64f ptCurPos;

                                        if (_bCalibrationComplete==false)
                                        {
                                            // If there aren't any rotation vectors (meaning we did not
                                            // have the camera calibrated yet), we cannot measure the
                                            // exact location of an item.  We'll have to use the camera
                                            // itself.
                                            ptCurPos = new MCvPoint3D64f(0, 0, 0);
                                        }
                                        else
                                        {
                                            // If the camera was calibrated, then we can find out 
                                            // the exact location in space of the item, so we can
                                            // check to see if it has moved.
                                            Mat matCurPos = m_arrTransVecs.Row(i);

                                            unsafe
                                            {
                                                Span<double> spanCurPos = new Span<double>(
                                                      matCurPos.DataPointer.ToPointer()
                                                    , matCurPos.NumberOfChannels);

                                                ptCurPos.X = spanCurPos[0];
                                                ptCurPos.Y = spanCurPos[1];
                                                ptCurPos.Z = spanCurPos[2];
                                            }
                                        }

                                        MarkerPair m = new MarkerPair ( myMarkerIds[i]
                                                                      , myMarkerIds[j]
                                                                      , myTitle
                                                                      , markerptfs[i]
                                                                      , markerptfs[j]
                                                                      , ptCurPos
                                                                      , m_nCameraPK );

                                        // If our list contains the key, then copy
                                        // the current location to that key to refresh
                                        // it's stay alive count.

                                        // This list of markers covers the IDs found in this
                                        // camera's field of view.  When updating the database,
                                        // we need to make sure the table has a camera ID also
                                        // so that we don't end up confusing the system.

                                        if (m_listCurrentMarker.ContainsKey(m.PairID))
                                            m_listCurrentMarker[m.PairID].UpdateCurrent(m);
                                        else
                                            m_listCurrentMarker.Add(m.PairID, m);
                                    }
                                }
                            }
                            catch(Exception e)
                            {
                                Debug.WriteLine(e.Message);
                            }
                        }
                    }
                }
            }
            #endregion

            // This loop draws currently alive markers, and decreases their
            // stay alive count.  If the stay alive count is less than zero,
            // it removes the item.
            listMarkersToRemove.Clear();
            listMarkersToDraw.Clear();

            foreach (var m in m_listCurrentMarker)
            {
                if (m.Value.IsAlive())
                    listMarkersToDraw.Add(m.Value);
                else
                    listMarkersToRemove.Add(m.Value);
            }

            MainWindow_ProcessVideoThread_DrawMarkerTagIDs(ref _colorMat, ref listMarkersToDraw);

            UpdateDatabaseEntries(listMarkersToDraw);
            RetireDatabaseEntries(listMarkersToRemove);


            // Finally, remove the marker from the list if it has
            // expired and is no longer seen.
            foreach (MarkerPair m in listMarkersToRemove)
                m_listCurrentMarker.Remove(m.PairID);
        }



        public void MainWindow_ProcessVideoThread_Calibration()
        {
            int i;

            CvInvoke.CvtColor(_colorMat
                              , _greyMat
                              , ColorConversion.Bgr2Gray);

            // Before converting to bitmap, detect the checkerboard pattern if it can
            bFound = CvInvoke.FindChessboardCorners(_greyMat
                                                   , _patternSize
                                                   , _checkerCorners
                                                   , CalibCbType.FastCheck);

            /* we may wish to go for better accuracy during calibration
            if (!bFound) 
                bFound = CvInvoke.FindChessboardCorners(_greyMat
                                       , _patternSize
                                       , _checkerCorners
                                       , CalibCbType.Accuracy);*/


            if (bFound)
            {
                _bCalibrationComplete = false;

                CvInvoke.CornerSubPix(_greyMat
                                     , _checkerCorners
                                     , new System.Drawing.Size(11, 11)
                                     , new System.Drawing.Size(-1, -1)
                                     , new MCvTermCriteria(30, 0.1));

                // Push the current result onto the list
                _checkerCornersList.Add(_checkerCorners);

                // get a new vectorofpointf
                _checkerCorners = new VectorOfPointF();

                _curDateTime = DateTime.Now;
                _lastDateTime = DateTime.Now;
            }

            // How many seconds since last?
            if (_checkerCornersList.Count > 0)
            {
                _curDateTime = DateTime.Now;
                _curLastTimeDiff = _curDateTime - _lastDateTime;
                _dSecondsUntilCalib = Math.Floor((TimeSpan.FromSeconds(MAX_SECONDS_UNTIL_CALIB)
                                    - _curLastTimeDiff).TotalSeconds);
                tstrCountDown = "  seconds until calib=" + _dSecondsUntilCalib.ToString();
            }
            else
            {
                _dSecondsUntilCalib = MAX_SECONDS_UNTIL_CALIB;
                tstrCountDown = "";
            }

            // If we've reached the calibration amount, let the user know by
            // removing the label, and saying we're calibrated.  If, for some
            // reason, 
            if (_dSecondsUntilCalib <= 0)
            {
                tstrCountDown = "  Calibrated";

                // Stop checking for calibration target and find the 
                // calibration matrices.
                m_bCheckForCalib = false;
                CalibrateStream.Dispatcher.Invoke(() => CalibrateStream.IsChecked = false);

                // No more than MAX_CORNERS_LIST_COUNT entries should be sent to the
                // CalibrateCamera command, so trim down by taking a sampling across
                // the entire list of corner entries.
                _checkerCornersTempList.Clear();

                if (_checkerCornersList.Count > 0)
                {


                    if (_checkerCornersList.Count > MAX_CORNERS_LIST_COUNT)
                    {
                        _dListInc = (double)(_checkerCornersList.Count - 1) / (double)MAX_CORNERS_LIST_COUNT;
                        for (_dCnt = 0; _dCnt < (double)(_checkerCornersList.Count-1); _dCnt += _dListInc)
                        {
                            _checkerCornersTempList.Add(new VectorOfPointF(_checkerCornersList[(int)Math.Floor(_dCnt)].ToArray()));
                        }
                    }
                    else
                    {
                        _checkerCornersTempList.AddRange(_checkerCornersList);
                    }


                    // Do the calibration
                    _cornersObjectList = new MCvPoint3D32f[_checkerCornersTempList.Count][];
                    _cornersPointsList = new PointF[_checkerCornersTempList.Count][];
                    for (i = 0; i < _checkerCornersTempList.Count; i++)
                    {
                        _cornersObjectList[i] = CreateObjectPoints(_patternSize, _sqW, _sqH).ToArray();
                        _cornersPointsList[i] = _checkerCornersTempList[i].ToArray();
                    }

                    _error = CvInvoke.CalibrateCamera(_cornersObjectList
                                                      , _cornersPointsList
                                                      , _imgSize
                                                      , _cameraMatrix
                                                      , _distortionMatrix
                                                      , CalibType.UseLU
                                                      , new MCvTermCriteria(300, 0.001)
                                                      , out _rotationVectors
                                                      , out _translationVectors);

                    if (_cameraMatrix != null)
                    {
                        // Prepare to store the intrinsics in a file
                        FileStorage fs = new FileStorage(".xml", FileStorage.Mode.Write | FileStorage.Mode.Memory);
                        fs.Write(_cameraMatrix.Mat, "Camera");
                        Properties.Settings.Default.CameraMatrix = fs.ReleaseAndGetString();
                        cameraParams.szCameraMatrix = Properties.Settings.Default.CameraMatrix;
                    }

                    if (_distortionMatrix != null)
                    {
                        // Store the intrinsics in the user settings
                        FileStorage fs = new FileStorage(".xml", FileStorage.Mode.Write | FileStorage.Mode.Memory);
                        fs.Write(_distortionMatrix.Mat, "Distortion");
                        Properties.Settings.Default.DistortionMatrix = fs.ReleaseAndGetString();
                        cameraParams.szDistortionMatrix = Properties.Settings.Default.DistortionMatrix;
                    }

                    Properties.Settings.Default.Save();
                    _bCalibrationComplete = true;
                }
                else
                {
                    _bCalibrationComplete = false;
                }
                _checkerCornersList.Clear();
                _checkerCornersTempList.Clear();
            }
        }


        public static readonly DependencyProperty LiveImageSourceProperty =
                    DependencyProperty.Register("LiveImageSource", typeof(string),
                      typeof(BitmapImage), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
                      new PropertyChangedCallback(OnLiveImageSourceChanged)));


        private static void OnLiveImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        async public void MainWindow_ProcessVideoThread(CancellationToken ct)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Lowest;
            _checkerCorners = new VectorOfPointF();
            _greyMat = new Mat();


            //Dictionary myDict = new Dictionary(Dictionary);
            //Emgu.CV.Aruco.Dictionary dict = new Emgu.CV.Aruco.Dictionary();
            //dict = CvInvoke.Imread

            // The processing loop will run forever unless the
            // cancellationtoken (ct) receives a cancel request.
            while (true)
            {
                // make sure we can catch an exception
                try
                {
                    // The displayed video matrix data is always from the
                    // frame just before the current one.  It should not be
                    // a negative number.
                    nCurDisplayMat = nCurMat - 1;
                    //Console.WriteLine("Process Frame " + nCurDisplayMat.ToString("D6"));

                    // Can we actually process this frame?
                    if (nCurDisplayMat >= 0)
                    {
                        if (m[nCurDisplayMat % RINGBUFFER_NUM_ELEMENTS] != null)
                        { 
                            if (   m[nCurDisplayMat % RINGBUFFER_NUM_ELEMENTS].Cols
                                 * m[nCurDisplayMat % RINGBUFFER_NUM_ELEMENTS].Rows != 0)
                            {
                                // Copy the current frame and get it's size
                                _colorMat = m[nCurDisplayMat % RINGBUFFER_NUM_ELEMENTS].Clone();
                                _imgSize = m[nCurDisplayMat % RINGBUFFER_NUM_ELEMENTS].Size;

                                if (_bSnapshot)
                                {
                                    _colorMat.Save("Snapshot.png");
                                    _bSnapshot = false;
                                }
                                if (m_bCheckForCalib)
                                {
                                    MainWindow_ProcessVideoThread_Calibration();
                                }
                                else
                                if (Static.bDetectMarkers)
                                {
                                    MainWindow_ProcessVideoThread_DetectMarkers();
                                }

                                tstr = "cur=" + nCurMat.ToString("D8")
                                + " disp=" + nCurDisplayMat.ToString("D8")
                                + " diff=" + (nCurMat - nCurDisplayMat).ToString()
                                + " calibs found=" + _checkerCornersList.Count().ToString()
                                + tstrCountDown;
                                ;

                                CvInvoke.PutText(_colorMat, tstr
                                    , new System.Drawing.Point(20, 50), FontFace.HersheyPlain, 3
                                    , new MCvScalar(255, 255, 255, 255), 10);

                                CvInvoke.PutText(_colorMat, tstr
                                    , new System.Drawing.Point(20, 50), FontFace.HersheyPlain, 3
                                    , new MCvScalar(0, 0, 0, 255), 1);

                                PaintOntoPictureBox(_colorMat);


                            }
                            else
                            {
                                Debug.WriteLine("Matrix[" + nCurDisplayMat.ToString() + "] == null");
                            }
                        }
                    }

                    //CalibrateStream.Dispatcher.Invoke(() => _bShowDistance = (bool)(ShowDistance.IsChecked));
                }
                catch (PostgresException ex)
                {
                    // Postgres-specific exception details
                    Debug.WriteLine($"PostgreSQL Error Code: {ex.SqlState}");
                    Debug.WriteLine($"Message: {ex.Message}");
                    Debug.WriteLine($"Detail: {ex.Detail}");
                    Debug.WriteLine($"Hint: {ex.Hint}");
                    Debug.WriteLine($"Position: {ex.Position}");
                    Debug.WriteLine($"Internal Query: {ex.InternalQuery}");
                    Debug.WriteLine($"Where: {ex.Where}");
                    Debug.WriteLine($"ERRRRRRRRRRRRRR");
                    Debug.WriteLine($"Done");
                }
                catch (Exception e)
                {
                    // If we had an exception, we will simply exit the thread
                    Debug.Write("Processing thread hit an exception!!!  "+e.ToString());
                }


                if (ct.IsCancellationRequested)
                {
                    return;
                }
            }

        }




        private BitmapImage _liveImageSource = null;

        [JsonIgnore]
        public BitmapImage LiveImageSource
        { 
            get
            { 
                return _liveImageSource;
            }
        }


    }
}
