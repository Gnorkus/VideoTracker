﻿using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace VideoTrack.Controls
{
    public partial class MarkerPair : IEquatable<MarkerPair>
    {
        public const int DEFAULT_MARKER_COUNTDOWN = 60;

        int id1, id2;
        PointF[] poly1, poly2;
        string _szPairDescription;
        MCvPoint3D64f m_matLastKnownPos;
        DateTime m_lastTimeSeen;

        int m_nCameraFK = 0;
        public int CameraFK { get { return m_nCameraFK; } set { m_nCameraFK = value; } }

        private PointF AddF (PointF a, PointF b)
        {
            return new PointF(a.X+b.X, a.Y+b.Y);
        }

        private PointF AverageF(PointF a, PointF b)
        {
            return new PointF( (a.X + b.X)/2.0f, (a.Y + b.Y)/2.0f);
        }

        private Point Add(PointF a, PointF b)
        {
            return Point.Round(new PointF(a.X + b.X, a.Y + b.Y));
        }

        public Point Average(PointF a, PointF b)
        {
            return Point.Round(new PointF((a.X + b.X) / 2.0f, (a.Y + b.Y) / 2.0f));
        }


        public int nCountDown { get; set; }

        public string PairDescription 
        {  
            get { return _szPairDescription; } 
            set { _szPairDescription = value; } 
        }

        public MarkerPair() { id1 = -1; id2 = -1; _szPairDescription = ""; nCountDown = DEFAULT_MARKER_COUNTDOWN; }

        public int PairID { get { return id1 + id2 * 1000; } }

        public Size Direction { get { return new Size((int)(poly2[1].Y - poly1[0].Y), (int)(poly2[1].X - poly1[0].X)); } }

        public Point UnderlineStart { get { return Point.Round(poly1[0]); } }

        public Point BoxTopLeft { get { return Point.Round(poly1[0]); }  }

        public Point BoxLeftCenter { get { return Average(poly1[0], poly1[3]); } }

        public Point BoxTopCenter { get { return Average(poly1[0], poly2[1]); } }

        public Point BoxRightCenter { get { return Average(poly2[1], poly2[2]); } }

        public Point BoxBottomCenter { get { return Average(poly1[3], poly2[2]); } }

        public Point BoxBottomLeft { get { return Point.Round(poly1[3]); } }

        public Point BoxTopRight { get { return Point.Round(poly2[1]); } }

        public Point BoxBottomRight { get { return Point.Round(poly2[2]); } }


        public Point UnderlineFinish { get { return Point.Round(poly2[1]); } }

        // Checking an item will decrement it's count
        public bool IsAlive()
        {
            // Record the timestamp if we actually saw it.
            // Each time we actually see a marker, it's
            // count down is reset to the default.
            if (nCountDown == DEFAULT_MARKER_COUNTDOWN)
                m_lastTimeSeen = DateTime.Now;

            // Decrement the countdown.  We do this 
            // just in case noise or other issues cause 
            // marker not to be seen.  We should 
            // differentiate between colors drawn if
            // an object was seen, wasn't seen but still
            // is active, wasn't seen but is retired.
            nCountDown--;
            if (nCountDown > 0)
                return true;
            else
                return false;
        }

        // Copying an item's current position will reset it's stay alive count
        public void UpdateCurrent(MarkerPair other)
        {
            this.id1 = other.id1;
            this.id2 = other.id2;
            this.poly1 = other.poly1;
            this.poly2 = other.poly2;
            nCountDown = DEFAULT_MARKER_COUNTDOWN;
        }

        public MarkerPair ( int id1new
                          , int id2new
                          , string szDesc
                          , PointF[] poly1new
                          , PointF[] poly2new 
                          , MCvPoint3D64f curpos
                          , int nCameraPK
                            )  
        { 
            id1 = id1new; 
            id2 = id2new;
            poly1 = new PointF[4];
            poly2 = new PointF[4];
            _szPairDescription = szDesc;

            m_matLastKnownPos = curpos;

            poly1new.CopyTo(poly1, 0);
            poly2new.CopyTo(poly2, 0);
            nCountDown = DEFAULT_MARKER_COUNTDOWN;

            CameraFK = nCameraPK;
        }

        public Point[] GetPoly(int nNumElem=4)
        {
            if (nNumElem > 4)
                nNumElem = 4;
            if (nNumElem < 2) 
                nNumElem = 2;

            Point[] rval = new Point[nNumElem];
            rval[0] = Point.Round(poly1[0]);
            rval[1] = Point.Round(poly2[1]);
            if (nNumElem>2) rval[2] = Point.Round(poly2[2]);
            if (nNumElem>3)rval[3] = Point.Round(poly1[3]);

            return rval;
        }

        public VectorOfPoint GetPolyV()
        {
            VectorOfPoint rval = new VectorOfPoint();
            rval.Push ( GetPoly() );
            return rval;
        }

        public PointF[] GetPolyF()
        {
            PointF[] rval = new PointF[4];
            rval[0] = poly1[0];
            rval[1] = poly2[1];
            rval[2] = poly2[2];
            rval[3] = poly1[3];

            return rval;
        }

        public Point GetCenter()
        {
            Point[] p = GetPoly();
            Point rval = new Point();
            rval.X = (p[0].X + p[1].X + p[2].X + p[3].X) / 4;
            rval.Y = (p[0].Y + p[1].Y + p[2].Y + p[3].Y) / 4;

            return rval;
        }

        public bool Equals(MarkerPair other)
        {
            if (this.PairID == other.PairID)
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return PairID;
        }
    }


}
