
namespace VideoTrack.Model
{
    public class Camera : CoreDatabase
    {
        [PrimaryIdentityKey] public int CameraPK { get; }

        public string CameraMatrix {  get; set; }
        public string DistortionMatrix { get; set; }
        public string HomographyMatrix { get; set; }

        public double ResultScale { get; set; }
        public double ResultWidth { get; set; }
        public double ResultLength { get; set; }
        public double BorderTop { get; set; }
        public double BorderLeft { get; set; }
        public double BorderRight { get; set; }
        public double BorderBottom { get; set; }
        public string URL { get; set; }
        public double DiffOfGaussRadius1 { get; set; }
        public double DiffOfGaussSigma1 { get; set; }
        public double DiffOfGaussRadius2 { get; set; }
        public double DiffOfGaussSigma2 { get; set; }
        public int ImageIndex { get; set; }
        public int FillToleranceLow { get; set; }
        public int FillToleranceHigh { get; set; }
        public int NumIterations { get; set; }
        public int SegmentLength { get; set; }
        public int ColorRadius { get; set; }
        public int SpatialRadius { get; set; }
        public string CameraProcessingURL { get; set; }

        public int ROILeft { get; set; }
        public int ROITop { get; set; }
        public int ROIRight { get; set; }
        public int ROIBottom { get; set; }
    }
}
