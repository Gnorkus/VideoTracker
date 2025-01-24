using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace VideoTrack.Controls
{
    public partial class VideoUserControl : System.Windows.Controls.UserControl
    {
        public void MainWindow_ReadVideoThread(CancellationToken ct)
        {
            Thread.CurrentThread.Priority = ThreadPriority.Highest;
            while (true)
            {   
                try
                {
                    if (!m_bIsPaused)
                    {
                        if (m_capture == null)
                        {
                            if (RTSPUrl.Length>0)
                            {
                                // Erase and fill all first frame buffers with a message saying the the 
                                // stream is being negotiated.

                                // Make sure the index is in the ring buffer
                                int i = nCurMat % RINGBUFFER_NUM_ELEMENTS;

                                // Make sure that the buffer was actually allocated
                                if (m[i] != null)
                                {
                                    if (m[i].Rows * m[i].Cols > 0)
                                    {
                                        // If the buffer was allocated, it should have a 
                                        // width and a height.  Set all the pixels to 
                                        // black.

                                        m[i].SetTo(new Emgu.CV.Structure.MCvScalar(0, 0, 0));
                                    }
                                    else
                                    {
                                        // If the buffer wasn't allocated, do so now, giving
                                        // it an arbitrary height and width of 400 and 600,
                                        // respectively..  and set all pixels to black
                                        m[i].Create(400, 600, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
                                        m[i].SetTo(new Emgu.CV.Structure.MCvScalar(0, 0, 0));
                                    }

                                    // put the message up in white lettering on the black background
                                    CvInvoke.PutText(m[i], "Negotiating RTSP stream for URL:" 
                                        , new System.Drawing.Point(m[i].Cols / 60, m[i].Rows / 2 - 50), FontFace.HersheyPlain, 4
                                        , new MCvScalar(255, 255, 255, 255), 3);
                                    CvInvoke.PutText(m[i], RTSPUrl
                                        , new System.Drawing.Point(m[i].Cols / 60, m[i].Rows / 2 + 50), FontFace.HersheyPlain, 4
                                        , new MCvScalar(255, 255, 255, 255), 3);
                                }
                                nCurMat++;

                                // "rtsp://192.168.74.231:554/user=admin&password=admin123&channel=1&stream=0.sdp?"
                                m_capture = new Emgu.CV.VideoCapture(RTSPUrl);
                                if (m_capture.IsOpened)
                                {
                                    // Whenever we start a stream, make sure we save the URL
                                    cameraParams.szRTSPStreamURL = RTSPUrl;


                                    Properties.Settings.Default.RTSPString = tstr;
                                    Properties.Settings.Default.Save();
                                    //m_capture.ImageGrabbed += Capture_ImageGrabbed1;
                                    //m_capture.Start();
                                }
                                else
                                {
                                    System.Windows.MessageBox.Show("The video stream URL " + RTSPUrl + " couldn't be opened.  Please check your internet connection or the URL.");
                                    //m_capture.Release();
                                    //Thread.Sleep(150);
                                    //m_capture = null;
                                }
                            }
                        }
                        else
                        {
                            // If we get here, then m_capture was allocated.
                            try
                            {
                                // Update the ring buffer position
                                nCurMat++;

                                // Make sure that we're not trying to get a frame
                                // that's currently being accessed by the UI thread
                                if (   (nCurMat % RINGBUFFER_NUM_ELEMENTS)
                                    != (nCurDisplayMat % RINGBUFFER_NUM_ELEMENTS))
                                {
                                    // Get the frame
                                    m[nCurMat % RINGBUFFER_NUM_ELEMENTS] = m_capture.QueryFrame();
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.ToString());
                            }

                        }

                    }
                    else
                    {
                        // If we're paused, deallocate the m_capture object.
                        if (m_capture != null)
                        {
                            m_capture.Release();
                            m_capture = null;
                        }
                    }
                }
                catch (Exception e) 
                {
                    Debug.Write("Processing threa hit an exception!!!  " + e.ToString());
                    return;
                }

                if (ct.IsCancellationRequested)
                {
                    if (m_capture != null)
                    {
                        //m_capture.Stop();
                        // make sure all the frames have actually stopped
                        //Thread.Sleep(1000);
                        m_capture.Release();
                        m_capture = null;
                    }
                    return;
                }
            }

        }

        
        private void Capture_ImageGrabbed1(object sender, EventArgs e)
        {


            // Before we retrieve the current matrix of image data,
            // update the ring buffer count.
            nCurMat++;

            // Retrieve the current frame and store it in the ring buffer.
            // The modulo division (% operator) ensures the index of
            // the array will not be exceeded.
            try
            {
                if ((nCurMat % RINGBUFFER_NUM_ELEMENTS)
                    != (nCurDisplayMat % RINGBUFFER_NUM_ELEMENTS))
                {
                    m_capture.Retrieve(m[nCurMat % RINGBUFFER_NUM_ELEMENTS]);
                    //Console.WriteLine("ImageGrabbed "+nCurMat.ToString("D6"));
                }
            }
            catch (Exception ex)
            {
               Debug.WriteLine(ex.ToString());
            }
        }

    }
}