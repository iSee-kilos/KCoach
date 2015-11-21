using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KCoach
{
    public static class Extensions
    {
        #region Camera

        public static ImageSource ToBitmap(this ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * ((format.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static ImageSource ToBitmap(this DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] pixelData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(pixelData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
            {
                ushort depth = pixelData[depthIndex];

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                pixels[colorIndex++] = intensity; // Blue
                pixels[colorIndex++] = intensity; // Green
                pixels[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        public static ImageSource ToBitmap(this InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort[] frameData = new ushort[width * height];
            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(frameData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < frameData.Length; infraredIndex++)
            {
                ushort ir = frameData[infraredIndex];

                byte intensity = (byte)(ir >> 7);

                pixels[colorIndex++] = (byte)(intensity / 1); // Blue
                pixels[colorIndex++] = (byte)(intensity / 1); // Green   
                pixels[colorIndex++] = (byte)(intensity / 0.4); // Red

                colorIndex++;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        

        #endregion

        #region Body

        public static Joint ScaleTo(this Joint joint, double width, double height, float skeletonMaxX, float skeletonMaxY)
        {
            joint.Position = new CameraSpacePoint
            {
                X = Scale(width, skeletonMaxX, joint.Position.X),
                Y = Scale(height, skeletonMaxY, -joint.Position.Y),
                Z = joint.Position.Z
            };

            return joint;
        }

        public static Joint ScaleTo(this Joint joint, double width, double height)
        {
            return ScaleTo(joint, width, height, 1.0f, 1.0f);
        }

        private static float Scale(double maxPixel, double maxSkeleton, float position)
        {
            float value = (float)((((maxPixel / maxSkeleton) / 2) * position) + (maxPixel / 2));

            if (value > maxPixel)
            {
                return (float)maxPixel;
            }

            if (value < 0)
            {
                return 0;
            }

            return value;
        }

        public static Point Project(this Joint joint, KinectSensor sensor)
        {
            CameraSpacePoint jointPosition = joint.Position;

            // 2D space point
            Point point = new Point();

            ColorSpacePoint colorPoint = sensor.CoordinateMapper.MapCameraPointToColorSpace(jointPosition);

            point.X = float.IsInfinity(colorPoint.X) ? 0 : colorPoint.X;
            point.Y = float.IsInfinity(colorPoint.Y) ? 0 : colorPoint.Y;
            return point;
        }

        #endregion

        #region Drawing

        private static Point scalePoint(Point p, double actualWidth, double actualHeight)
        {
            Point newPoint = new Point();
            newPoint.X = p.X * actualWidth / 1920;
            newPoint.Y = p.Y * actualHeight / 1080;
            return newPoint;
        }

        public static void DrawSkeleton(this Canvas canvas, Body body, KinectSensor senser)
        {
            if (body == null) return;
            
            foreach (Joint joint in body.Joints.Values)
            {
                DrawJoint(canvas, joint, senser);
            }

            canvas.DrawBone(body.Joints[JointType.Head], body.Joints[JointType.Neck], senser);
            canvas.DrawBone(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder], senser);
            canvas.DrawBone(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderLeft], senser);
            canvas.DrawBone(body.Joints[JointType.SpineShoulder], body.Joints[JointType.ShoulderRight], senser);
            canvas.DrawBone(body.Joints[JointType.SpineShoulder], body.Joints[JointType.SpineMid], senser);
            canvas.DrawBone(body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft], senser);
            canvas.DrawBone(body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight], senser);
            canvas.DrawBone(body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft], senser);
            canvas.DrawBone(body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight], senser);
            canvas.DrawBone(body.Joints[JointType.WristLeft], body.Joints[JointType.HandLeft], senser);
            canvas.DrawBone(body.Joints[JointType.WristRight], body.Joints[JointType.HandRight], senser);
            canvas.DrawBone(body.Joints[JointType.HandLeft], body.Joints[JointType.HandTipLeft], senser);
            canvas.DrawBone(body.Joints[JointType.HandRight], body.Joints[JointType.HandTipRight], senser);
            canvas.DrawBone(body.Joints[JointType.HandTipLeft], body.Joints[JointType.ThumbLeft], senser);
            canvas.DrawBone(body.Joints[JointType.HandTipRight], body.Joints[JointType.ThumbRight], senser);
            canvas.DrawBone(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase], senser);
            canvas.DrawBone(body.Joints[JointType.SpineBase], body.Joints[JointType.HipLeft], senser);
            canvas.DrawBone(body.Joints[JointType.SpineBase], body.Joints[JointType.HipRight], senser);
            canvas.DrawBone(body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft], senser);
            canvas.DrawBone(body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight], senser);
            canvas.DrawBone(body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft], senser);
            canvas.DrawBone(body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight], senser);
            canvas.DrawBone(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft], senser);
            canvas.DrawBone(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight], senser);
        }

        public static void DrawJoint(this Canvas canvas, Joint joint, KinectSensor sensor)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;
            Color c = Colors.Red;
            Boolean inferred = false;
            if (joint.TrackingState == TrackingState.Inferred)
            {
                c = Colors.Gray;
                inferred = true;
            }


            // joint = joint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            Point p = scalePoint(joint.Project(sensor), canvas.ActualWidth, canvas.ActualHeight);
            // Console.WriteLine("Position: " + p.X + " " + p.Y + "canvas size: " + canvas.Width + " " + canvas.Height);
            // WirteText(canvas, joint.Project(sensor), "Position: " + joint.Project(sensor).X + ":" + joint.Project(sensor).Y);
            DrawPoint(canvas, p, c, inferred);
        }


        public static void DrawBone(this Canvas canvas, Joint first, Joint second, KinectSensor sensor)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            // first = first.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            // second = second.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Color c = Colors.Yellow;
            Boolean inferred = false;
            if (first.TrackingState == TrackingState.Inferred || second.TrackingState == TrackingState.Inferred)
            {
                c = Colors.Black;
                inferred = true;
            }

            Point firstP = scalePoint(first.Project(sensor), canvas.ActualWidth, canvas.ActualHeight);
            Point secondP = scalePoint(second.Project(sensor), canvas.ActualWidth, canvas.ActualHeight);
            DrawLine(canvas, firstP, secondP, c, inferred);
        }

        public static void DrawPoint(this Canvas canvas, Point position, Color c, Boolean inferred)
        {
            SolidColorBrush fill = new SolidColorBrush(c);
            if (inferred)
            {
                fill.Opacity = 0.70d;
            }
            Ellipse ellipse = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = fill
            };

            Canvas.SetLeft(ellipse, position.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, position.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
            // WirteText(canvas, position, "Position: " + position.X + ":" + position.Y + "\ncanvas actual size: " + canvas.ActualWidth + ":" + canvas.ActualHeight);

        }

        public static void DrawLine(this Canvas canvas, Point first, Point second, Color c, Boolean inferred)
        {
            SolidColorBrush fill = new SolidColorBrush(c);
            if (inferred)
            {
                fill.Opacity = 0.60d;
            }

            Line line = new Line
            {
                X1 = first.X,
                Y1 = first.Y,
                X2 = second.X,
                Y2 = second.Y,
                StrokeThickness = 3,
                Stroke = fill
            };

            canvas.Children.Add(line);
        }

        public static void WirteText(this Canvas canvas, Point p, String text)
        {
            TextBlock textBlock = new TextBlock();
            // textBlock.UpdateLayout();

            // Position of label centered to given position

            Canvas.SetLeft(textBlock, p.X);
            Canvas.SetTop(textBlock, p.Y);
            textBlock.Text = text;

            canvas.Children.Add(textBlock); // Add to Canvas
        }
        #endregion
    }
}
