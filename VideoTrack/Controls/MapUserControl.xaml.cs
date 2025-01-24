using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SkiaSharp.Views.Desktop;
using SkiaSharp;
using Svg.Skia;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.Windows.Forms;
using OpenTK.Input;

namespace VideoTrack.Controls
{
    /// <summary>
    /// Interaction logic for MapUserControl.xaml
    /// </summary>
    public partial class MapUserControl : System.Windows.Controls.UserControl
    {
        private Svg.Skia.SKSvg _svg = new Svg.Skia.SKSvg();
        private float _scale = 1f;
        private SKPoint _translation = new SKPoint(0, 0);
        private SKPoint _lastPanPosition;
        private SKMatrix _curTotalMatrix ;
        private bool bDebugShowCurMatrix = false;
        private float _linesizeratio = -.02f;
        public MapUserControl()
        {
            InitializeComponent();
            // Load the SVG file

            try
            {
                _svg.Load("C:\\Users\\Gerhard Norkus\\Documents\\MIller Metal\\SHOP LAYOUT 111324 Purged_Model.svg");
            }
            catch (Exception ex) 
            {
                System.Windows.MessageBox.Show(ex.Message);
            }

            // Enable mouse events for zoom and pan
            SkiaCanvas.MouseWheel += SkiaCanvas_MouseWheel;
            SkiaCanvas.MouseMove += SkiaCanvas_MouseMove;
            SkiaCanvas.MouseDown += SkiaCanvas_MouseDown;
        }

        public void MdiChild_Closing(object sender, RoutedEventArgs e)
        {
            
            if ((e as WPF.MDI.ClosingEventArgs).Cancel)
                return;
        }

        private void SkiaCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;

            // Clear the canvas
            canvas.Clear(SKColors.White);

            // Apply scaling and translation
            canvas.Scale(_scale);
            canvas.Translate(_translation);

            // Update the current matrix variable so we can use it
            // in other locations.
            _curTotalMatrix = canvas.TotalMatrix;

            // Draw the SVG
            if (_svg.Picture != null)
                canvas.DrawPicture(_svg.Picture);

            // Draw the markers that the user is searching
            // for on top of the SVG picture.
            SKPath path = new SKPath();
            SKPoint[] pathpts = new SKPoint[4];
            pathpts[0] = new SKPoint(0, 0);
            pathpts[1] = new SKPoint(3600, 0);
            pathpts[2] = new SKPoint(3600, 2400);
            pathpts[3] = new SKPoint(0, 2400);

            SKPaint strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Red,
                StrokeWidth = 5,
                StrokeJoin = SKStrokeJoin.Round
            };

            path.AddPoly(pathpts);
            canvas.DrawPath(path, strokePaint);
        }

        private SKSize SkiaCanvas_ScaledSize(bool bNoScale=true)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow;
            var dpiScale = VisualTreeHelper.GetDpi(mainWindow);
            float width = (float)SkiaCanvas.ActualWidth;
            float height = (float)SkiaCanvas.ActualHeight;
            float dpiScaleX = (float)dpiScale.DpiScaleX;
            float dpiScaleY = (float)dpiScale.DpiScaleY;

            return new SKSize(width * dpiScaleX / (bNoScale ? _scale:1f), height * dpiScaleY / (bNoScale?_scale:1f));
        }

        private void SkiaCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            SKPoint scaledPosition;
            SKSize scaledSize = SkiaCanvas_ScaledSize();
            // Dimensions of window?

            if (System.Windows.Input.Keyboard.Modifiers.Equals(ModifierKeys.Control))
            {
                if (SkiaCanvas.IsMouseOver)
                {
                    var mousePos = e.GetPosition(SkiaCanvas).ToSKPoint();
                    var curZoomedPos = MousePosToZoomedPos(mousePos, _curTotalMatrix);

                    // Make a copy of the current SkiaCanvas.TotalMatrix
                    SKMatrix tempmat = _curTotalMatrix;

                    // Adjust the scale based on scroll direction
                    var zoomFactor = e.Delta > 0 ? 1.1f : (1.0f/1.1f); 
                    _scale *= zoomFactor;

                    tempmat.ScaleX *= zoomFactor;
                    tempmat.ScaleY *= zoomFactor;
                    tempmat.TransX *= zoomFactor;
                    tempmat.TransY *= zoomFactor;

                    SKPoint newZoomedPos = MousePosToZoomedPos(mousePos, tempmat);

                    // How far to translate?
                    SKPoint diff = curZoomedPos - newZoomedPos;
                    _translation -= diff; // ScaledScreenPosToMousePos(diff);

                    SkiaCanvas.InvalidateVisual(); // Redraw the canvas
                }
            }
            else
            if (System.Windows.Input.Keyboard.Modifiers.Equals (ModifierKeys.Shift)) 
            {
                if (SkiaCanvas.IsMouseOver)
                {
                    // How far to translate?
                    SKPoint diff = new SKPoint(0f, _linesizeratio * Math.Max(scaledSize.Width,scaledSize.Height) * (e.Delta > 0 ? 1 : -1));
                    _translation -= diff; // ScaledScreenPosToMousePos(diff);

                    SkiaCanvas.InvalidateVisual(); // Redraw the canvas
                }
            }
            else
            {
                if (SkiaCanvas.IsMouseOver)
                {
                    // How far to translate?
                    SKPoint diff = new SKPoint(_linesizeratio * Math.Max(scaledSize.Width, scaledSize.Height) * (e.Delta > 0 ? 1 : -1), 0f);
                    _translation -= diff; // ScaledScreenPosToMousePos(diff);)

                    SkiaCanvas.InvalidateVisual(); // Redraw the canvas
                }
            }
        }

        private void SkiaCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                String strPos;

                // Save the first point for a drag operation.
                _lastPanPosition = MousePosToScaledScreenPos(e.GetPosition(SkiaCanvas).ToSKPoint());
            }
        }

        private SKPoint MousePosToScaledScreenPos(SKPoint mousePos)
        {
            SKPoint scaledPixelPosition;

            var mainWindow = System.Windows.Application.Current.MainWindow;
            var dpiScale = VisualTreeHelper.GetDpi(mainWindow);

            float dpiScaleX = (float)dpiScale.DpiScaleX;
            float dpiScaleY = (float)dpiScale.DpiScaleY;

            scaledPixelPosition = new SKPoint(mousePos.X * dpiScaleX, mousePos.Y * dpiScaleY);

            return scaledPixelPosition;
        }

        private SKPoint ScaledScreenPosToMousePos(SKPoint screenPos)
        {
            SKPoint scaledPixelPosition;

            var mainWindow = System.Windows.Application.Current.MainWindow;
            var dpiScale = VisualTreeHelper.GetDpi(mainWindow);

            float dpiScaleX = (float)dpiScale.DpiScaleX;
            float dpiScaleY = (float)dpiScale.DpiScaleY;

            scaledPixelPosition = new SKPoint(screenPos.X / dpiScaleX, screenPos.Y / dpiScaleY);

            return scaledPixelPosition;
        }

        private SKPoint ZoomedPosToScaledPos(SKPoint zoomedPos, SKMatrix mat)
        {
            SKPoint scaledPixelPosition;

            scaledPixelPosition = mat.MapPoint( (float)zoomedPos.X
                                              , (float)zoomedPos.Y);

            return scaledPixelPosition;
        }

        private SKPoint MousePosToZoomedPos ( SKPoint mousePos, SKMatrix matToInvert )
        {
            SKPoint zoomedPos;
            SKPoint scaledPixelPosition;

            // Invert the TotalMatrix to map screen to canvas coordinates
            if (matToInvert.TryInvert(out SKMatrix inverseMatrix))
            {
                scaledPixelPosition = MousePosToScaledScreenPos(mousePos);
                // Apply the inverse matrix
                zoomedPos = inverseMatrix.MapPoint((float)scaledPixelPosition.X,
                                                   (float)scaledPixelPosition.Y);
            }
            else
            {
                // Make sure we don't crash if the matrix inversion is
                // not successful
                zoomedPos = new SKPoint();
            }

            return zoomedPos;
        }

        private void SkiaCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SKPoint scaledPosition;

            scaledPosition = MousePosToScaledScreenPos(e.GetPosition(SkiaCanvas).ToSKPoint());

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Display or use the transformed position
                SKPoint diff = _lastPanPosition - scaledPosition;
                diff.X /= _scale;
                diff.Y /= _scale;
                _translation -= diff ;
                _lastPanPosition = scaledPosition;

                SkiaCanvas.InvalidateVisual(); // Redraw the canvas
            }
            else
            {

            }
        }

    }
}
