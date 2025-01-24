using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Documents;
using Emgu.CV.Structure;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Diagnostics;





namespace VideoTrack.Controls
{
    public partial class VideoUserControl : System.Windows.Controls.UserControl
    {
        Matrix<float> matPtToRotate = new Matrix<float>(2, 1);

        public bool IsMarkerInCurrentList(int nHashTag)
        {
            return m_listCurrentMarker.ContainsKey(nHashTag);
        }

        public List<Point> RotateRoundAndOffsetPointF ( List<System.Drawing.PointF> points
                                                      , ref Matrix<float> rotMat
                                                      , System.Drawing.Point start )
        {
            List<Point> rval = new List<Point>();
            Point mypt;

            foreach ( System.Drawing.PointF point in points)
            {
                // Get ready to rotate
                matPtToRotate[0, 0] = point.X;
                matPtToRotate[1, 0] = point.Y;

                // Perform the matrix multiplication (rotation)
                Matrix<float> matRotated = rotMat * matPtToRotate;

                // Extract the rotated point
                mypt = new Point ( (int)Math.Round(matRotated[0, 0],0,MidpointRounding.ToEven)
                                 , (int)Math.Round(matRotated[1, 0],0,MidpointRounding.ToEven) );
                mypt.X += start.X;
                mypt.Y += start.Y;

                // First, rotate the 
                rval.Add(mypt);
            }

            return rval;
        }

        public double AngleFromPoints(Point start, Point finish)
        {
            // What's the rotation of the text, using the start and finish points
            float deltaX = finish.X - start.X;
            float deltaY = finish.Y - start.Y;

            double angleInRadians = Math.Atan2(deltaY, deltaX);
            double angleInDegrees = angleInRadians * 180.0 / Math.PI;

            return angleInDegrees;
        }

        public List<List<Point>> ConvertFontToPolylines ( string text
                                                        , string fontFamilyName
                                                        , float fontSize
                                                        , System.Drawing.Point start
                                                        , System.Drawing.Point finish)
        {
            int i;

            // Create a Font and GraphicsPath
            Font font = new Font(fontFamilyName, fontSize);
            GraphicsPath path = new GraphicsPath();
            List<List<Point>> rval = new List<List<Point>>();
            List<PointF> letter = new List<PointF>();

            // Add text to the path
            path.AddString(text, font.FontFamily, (int)font.Style, font.Size, new PointF(0, 0), StringFormat.GenericDefault);

            // Get path data
            PathData pathData = path.PathData;

            // Get the rotation matrix
            double angleInDegrees = AngleFromPoints(start, finish);
            Matrix<float> matRot = RotateMatrix2DFromPts(start, finish);

            // Convert to polylines (just lines, no curves)
            for (i=0; i<pathData.Points.Length; i++) 
            {
                // If we have added points to the letter already
                // and the next point is of type 0 (Start Path)
                // then add the letter to the list, and 
                // start a new letter
                if (letter.Count > 0 && pathData.Types[i] == 0)
                {
                    // Rotate and offset the letter.  Then, add it to the return value
                    rval.Add(RotateRoundAndOffsetPointF(letter,ref matRot, start));

                    // Start a new Letter
                    letter = new List<PointF>();
                }
                letter.Add(pathData.Points[i]);
            }

            // Don't miss the final letter
            if (letter.Count>0)
                rval.Add(RotateRoundAndOffsetPointF(letter, ref matRot, start));

            return rval;
        }
        
        Matrix<float> RotateMatrix2DFromPts( System.Drawing.Point start
                                            , System.Drawing.Point finish)
        {
            // Step 1: Calculate the angle between two points relative to the x-axis (in radians)
            //float angle1 = (float)Math.Atan2(start.Y, start.X); // angle for the first point
            //float angle2 = (float)Math.Atan2(finish.Y, finish.X); // angle for the second point

            // Step 2: Find the difference between the angles
            float angleDifference = (float)Math.Atan2(finish.Y-start.Y, finish.X-start.X);

            // Step 3: Create the rotation matrix based on the angle difference
            float cosTheta = (float)Math.Cos(angleDifference);
            float sinTheta = (float)Math.Sin(angleDifference);

            // Construct the rotation matrix
            Matrix<float> matRot = new Matrix<float>(2, 2);
            matRot[0, 0] = cosTheta;
            matRot[0, 1] = -sinTheta;
            matRot[1, 0] = sinTheta;
            matRot[1, 1] = cosTheta;

            return matRot;
        }

        private Quaternions ConvertRotationToQuaternion(Mat rotationMatrix)
        {
            // Ensure the matrix is 3x3
            if (rotationMatrix.Rows != 3 || rotationMatrix.Cols != 3)
                throw new ArgumentException("Input matrix must be a 3x3 rotation matrix.");

            double[] values = new double[9];
            rotationMatrix.CopyTo(values);

            // Extract the elements of the 3x3 rotation matrix
            float m00 = (float)values[0]; // = rotationMatrix[0, 0];
            float m01 = (float)values[1]; //  = rotationMatrix[0, 1];
            float m02 = (float)values[2]; //  = rotationMatrix[0, 2];
            float m10 = (float)values[3]; //  = rotationMatrix[1, 0];
            float m11 = (float)values[4]; //  = rotationMatrix[1, 1];
            float m12 = (float)values[5]; //  = rotationMatrix[1, 2];
            float m20 = (float)values[6]; //  = rotationMatrix[2, 0];
            float m21 = (float)values[7]; //  = rotationMatrix[2, 1];
            float m22 = (float)values[8]; //  = rotationMatrix[2, 2];

            // Calculate the trace of the matrix (sum of diagonal elements)
            float trace = m00 + m11 + m22;

            float qw, qx, qy, qz;

            // If the trace is positive, use this method for efficiency
            if (trace > 0)
            {
                float s = (float)Math.Sqrt(trace + 1.0f) * 2; // s = 4 * qw
                qw = 0.25f * s;
                qx = (m21 - m12) / s;
                qy = (m02 - m20) / s;
                qz = (m10 - m01) / s;
            }
            else
            {
                // Find the largest diagonal element and calculate the corresponding quaternion
                float maxDiag = Math.Max(Math.Max(m00, m11), m22);

                if (maxDiag == m00)
                {
                    float s = (float)Math.Sqrt(1.0f + m00 - m11 - m22) * 2; // s = 4 * qx
                    qw = (m21 - m12) / s;
                    qx = 0.25f * s;
                    qy = (m01 + m10) / s;
                    qz = (m02 + m20) / s;
                }
                else if (maxDiag == m11)
                {
                    float s = (float)Math.Sqrt(1.0f + m11 - m00 - m22) * 2; // s = 4 * qy
                    qw = (m02 - m20) / s;
                    qx = (m01 + m10) / s;
                    qy = 0.25f * s;
                    qz = (m12 + m21) / s;
                }
                else
                {
                    float s = (float)Math.Sqrt(1.0f + m22 - m00 - m11) * 2; // s = 4 * qz
                    qw = (m10 - m01) / s;
                    qx = (m02 + m20) / s;
                    qy = (m12 + m21) / s;
                    qz = 0.25f * s;
                }
            }

            return new Quaternions(qx, qy, qz, qw);
        }



        private double ConvertRotationVectorToAxisAngle(ref Mat rotationVector, ref MCvPoint3D64f axis)
        {
            Mat rotationMatrix = new Mat();
            Mat axisAngle = new Mat();

            // Using the Rodrigues function to convert the rotation vector to axis-angle
            CvInvoke.Rodrigues(rotationVector, rotationMatrix);


            axis = new MCvPoint3D64f(0, 0, 0);
            double angle = 0.0 ;

            unsafe
            {
                IntPtr rotMatPtr = (IntPtr)rotationMatrix.DataPointer.ToPointer();
                double* rotMatArr = (double*)rotMatPtr;

                // Extract the angle from the rotation matrix
                angle = Math.Acos(rotMatArr[0] + rotMatArr[4] + rotMatArr[8]) * 180.0 / Math.PI ;

                // Now extract the axis of rotation
                axis.X = rotMatArr[7] - rotMatArr[5];
                axis.Y = rotMatArr[2] - rotMatArr[6];
                axis.Z = rotMatArr[3] - rotMatArr[1];

                // Normalize the axis
                double length = Math.Sqrt(axis.X * axis.X + axis.Y * axis.Y + axis.Z * axis.Z);
                axis.X /= length;
                axis.Y /= length;
                axis.Z /= length;
            }

            return angle;
        }





        // Create a list of the object points associated with the checkerboard
        private List<MCvPoint3D32f> CreateObjectPoints(System.Drawing.Size sz
                                                       , float w = 1.0f
                                                       , float h = 1.0f)
        {
            float x, y;

            var chessboard = new List<MCvPoint3D32f>();

            for (y = 0; y < sz.Height; y++)
            {
                for (x = 0; x < sz.Width; x++)
                {
                    chessboard.Add(new MCvPoint3D32f(x * w, y * h, 0));
                }
            }

            return chessboard;
        }
        public static bool ValidateRTSPUrl(string url)
        {
            // Regular expression to validate basic RTSP URL with optional query parameters
            //string pattern = @"^rtsp:\/\/(([\w\-]+\:([\w\-]+)@)?([\w\-\.]+)(:(\d+))?)(\/[\w\-\/]+)?(\?([\w\-]+=[\w\-]+(&[\w\-]+=[\w\-]+)*))?$";
            //string pattern = @"^rtsp:\/\/([a-zA-Z0-9.-]+)(:([0-9]+))?(/[\w/.-]*)?(\?([a-zA-Z0-9_-]+=[a-zA-Z0-9_-]+(&[a-zA-Z0-9_-]+=[a-zA-Z0-9_-]+)*))?$";
            string pattern = @"^rtsp:\/\/([a-zA-Z0-9.-]+)(:([0-9]+))?(/[\w/.-]*)?(([a-zA-Z0-9_-]+=[a-zA-Z0-9_-]+(&[a-zA-Z0-9_-]+=[a-zA-Z0-9_-]+)*))?$";


            Regex regex = new Regex(pattern);
            Match match = regex.Match(url);

            // If the URL doesn't match the RTSP format, return false
            if (match.Success)
            {
                return true;
            }

            //pattern = @"[A-Za-z]+://\b(?:(?:2(?:[0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9])\.){3}(?:(?:2([0-4][0-9]|5[0-5])|[0-1]?[0-9]?[0-9]))\b:[0-9]+/[A-Za-z]+=[A-Za-z0-9]+&[A-Za-z0-9]+=[A-Za-z0-9]+&[A-Za-z0-9]+=[A-Za-z0-9]+&[A-Za-z0-9]+=[A-Za-z0-9]+\.[A-Za-z0-9]+\?";
            pattern = @"^(?i)\brtsp\b(?-i):\/\/[a-zA-Z0-9.-]+(:\d+)\/[a-zA-Z0-9=&?]+([a-zA-Z0-9=&]+)?.([a-zA-Z0-9?]+)";
            regex = new Regex(pattern);
            match = regex.Match(url);
            if (match.Success)
            {
                return true;
            }

            // Validate query parameters (if any)
            string queryString = match.Groups[8].Value;
            if (!string.IsNullOrEmpty(queryString))
            {
                // Parse and validate each query parameter
                NameValueCollection queryParameters = HttpUtility.ParseQueryString(queryString);

                foreach (string key in queryParameters)
                {
                    string value = queryParameters[key];
                    if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    {
                        // Invalid key-value pair
                        return false;
                    }
                }
            }

            // URL is valid
            return true;
        }

        bool IsValidURL(string URL)
        {
            string Pattern = @" ^ rtsp:\/\/([a-zA-Z0-9_-]+(:[a-zA-Z0-9_-]+)?@)?[a-zA-Z0-9.-]+(:[0-9]+)?(\/[a-zA-Z0-9%/._-]*)?(\?[a-zA-Z0-9=&_-]*)?$";

            //@"^rtsp://([a-zA-Z0-9.-]+)(:(\d+))?(\/[a-zA-Z0-9\-._~%!$&'()*+,;=]+)*$";
            Regex Rgx = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            if (Rgx.IsMatch(URL))
                return true;

            return ValidateRTSPUrl(URL);
        }

        private bool CheckCalibrationMatricesLoaded()
        {
            bool rval = true;

            if (_cameraMatrix != null)
            {
                string tstr = "";
                if (cameraParams.szCameraMatrix!=null && cameraParams.szCameraMatrix.Length > 0)
                {
                    tstr = cameraParams.szCameraMatrix;
                }
                else
                if (Properties.Settings.Default.CameraMatrix.Length > 0)
                {
                    tstr = Properties.Settings.Default.CameraMatrix;
                }
                if (tstr.Length > 0)
                {
                    try
                    {
                        Emgu.CV.Mat tmat = new Mat();
                        FileStorage fsr = new FileStorage(tstr, FileStorage.Mode.Read | FileStorage.Mode.Memory);
                        FileNode fn = fsr.GetNode("Camera");
                        fn.ReadMat(tmat);
                        tmat.ConvertTo(_cameraMatrix, DepthType.Cv64F);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        rval = false;
                    }
                }
                if (_cameraMatrix.Sum == 0.0)
                    rval = false;
            }
            else
                rval = false;

            if (_distortionMatrix != null)
            {
                string tstr = "";
                if (cameraParams.szDistortionMatrix != null && cameraParams.szDistortionMatrix.Length > 0)
                {
                    tstr = cameraParams.szDistortionMatrix;
                }
                else
                if (Properties.Settings.Default.DistortionMatrix.Length > 0)
                {
                    tstr = Properties.Settings.Default.DistortionMatrix;
                }
                if (tstr.Length > 0)
                {
                    try
                    {
                        Emgu.CV.Mat tmat = new Mat();
                        FileStorage fsr = new FileStorage(tstr, FileStorage.Mode.Read | FileStorage.Mode.Memory);
                        FileNode fn = fsr.GetNode("Distortion");
                        fn.ReadMat(tmat);
                        tmat.ConvertTo(_distortionMatrix, DepthType.Cv64F);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        rval = false;
                    }
                }
                if (_distortionMatrix.Sum == 0.0)
                    rval = false;
            }
            else
                rval = false;

            if (!rval)
            {
                // If we failed, make sure that all the entries are fresh 
                // for a future calibration.
                if (Properties.Settings.Default.CameraMatrix.Length > 0
                    || Properties.Settings.Default.DistortionMatrix.Length > 0
                    )
                {
                    Properties.Settings.Default.CameraMatrix = "";
                    Properties.Settings.Default.DistortionMatrix = "";
                    Properties.Settings.Default.Save();
                }
            }

            return rval;
        }

        public void UpdateRTSPParam()
        {
            string tstr = new TextRange(RTSPStream.Document.ContentStart, RTSPStream.Document.ContentEnd).Text;

            tstr = tstr.Replace("\n", "");
            tstr = tstr.Replace("\r", "");
            if (tstr != null && tstr.Length > 0 && IsValidURL(tstr))
                cameraParams.szRTSPStreamURL = tstr;
        }


    }
}
