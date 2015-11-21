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

        public static IReadOnlyDictionary<JointType, int> GetJointAngles(this Body body)
        {
            // write angle
            // knee
            Dictionary<JointType, int> res = new Dictionary<JointType, int>();

            int kneeLeftAngle = GetAngle(body.Joints[JointType.KneeLeft], body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft], body.Joints[JointType.AnkleLeft]);
            res[JointType.KneeLeft] = kneeLeftAngle;

            int kneeRightAngle = GetAngle(body.Joints[JointType.KneeRight], body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight], body.Joints[JointType.AnkleRight]);
            res[JointType.KneeRight] = kneeRightAngle;

            // ankle
            int ankleLeftAngle = GetAngle(body.Joints[JointType.AnkleLeft], body.Joints[JointType.FootLeft], body.Joints[JointType.AnkleLeft], body.Joints[JointType.KneeLeft]);
            res[JointType.AnkleLeft] = ankleLeftAngle;

            int ankleRightAngle = GetAngle(body.Joints[JointType.AnkleRight], body.Joints[JointType.FootRight], body.Joints[JointType.AnkleRight], body.Joints[JointType.KneeRight]);
            res[JointType.AnkleRight] = ankleRightAngle;

            // spine
            //double spineAngle = GetAngle(body.Joints[JointType.SpineMid], body.Joints[JointType.SpineBase], body.Joints[JointType.SpineShoulder]);
            //Point spine = scalePoint(body.Joints[JointType.SpineMid].Project(senser), canvas.ActualWidth, canvas.ActualHeight);
            //WirteText(canvas, spine, spineAngle.ToString());

            // hip
            int hipLeftAngle = GetAngle(body.Joints[JointType.SpineBase], body.Joints[JointType.SpineMid], body.Joints[JointType.HipLeft], body.Joints[JointType.KneeLeft]);
            int hipRightAngle = GetAngle(body.Joints[JointType.SpineBase], body.Joints[JointType.SpineMid], body.Joints[JointType.HipRight], body.Joints[JointType.KneeRight]);
            int hipAngle = Math.Max(hipLeftAngle, hipRightAngle);
            res[JointType.SpineMid] = hipAngle;

            // elbow
            int elbowLeftAngle = GetAngle(body.Joints[JointType.ElbowLeft], body.Joints[JointType.ShoulderLeft], body.Joints[JointType.ElbowLeft], body.Joints[JointType.WristLeft]);
            res[JointType.ElbowLeft] = elbowLeftAngle;

            int elbowRightAngle = GetAngle(body.Joints[JointType.ElbowRight], body.Joints[JointType.ShoulderRight], body.Joints[JointType.ElbowRight], body.Joints[JointType.WristRight]);
            res[JointType.ElbowRight] = elbowRightAngle;

            // neck
            int neckAngle = GetAngle(body.Joints[JointType.Neck], body.Joints[JointType.SpineShoulder], body.Joints[JointType.Neck], body.Joints[JointType.Head]);
            res[JointType.Neck] = neckAngle;

            // legs
            int legsAngle = GetAngle(body.Joints[JointType.KneeLeft], body.Joints[JointType.HipLeft], body.Joints[JointType.KneeRight], body.Joints[JointType.HipRight]);
            res[JointType.SpineBase] = legsAngle;

            return res;
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

        private static Boolean steadyFlag;

        public static void DrawWrongJoints(this Canvas canvas, Body body, JointType[] joints, KinectSensor sensor)
        {
            foreach (JointType jointType in joints)
            {
                canvas.DrawWrongJoint(body.Joints[jointType], sensor);
            }
        }
        public static void DrawSkeleton(this Canvas canvas, Body body, KinectSensor senser, Boolean isSteady)
        {
            if (body == null) return;
            steadyFlag = isSteady;
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

            IReadOnlyDictionary<JointType, int> res = GetJointAngles(body);
            foreach (var kv in res)
            {
                Point p = scalePoint(body.Joints[kv.Key].Project(senser), canvas.ActualWidth, canvas.ActualHeight);
                WirteText(canvas, p, kv.Value.ToString());
            }
            
        }

        public static void DrawJoint(this Canvas canvas, Joint joint, KinectSensor sensor)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;
            Color c;
            if (steadyFlag)
                c = Colors.CadetBlue;
            else
                c = Colors.SandyBrown;
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

        private static void DrawWrongJoint(this Canvas canvas, Joint joint, KinectSensor sensor)
        {
            if (joint.TrackingState == TrackingState.NotTracked) return;
            Color c = Colors.Red;
            
            double alpha = 1.0;
            if (joint.TrackingState == TrackingState.Inferred)
            {
                alpha = 0.75;
            }


            // joint = joint.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            Point p = scalePoint(joint.Project(sensor), canvas.ActualWidth, canvas.ActualHeight);
            if (!inCanvas(canvas, p))
                return;

            SolidColorBrush fill = new SolidColorBrush(c);
            fill.Opacity = alpha;
            Ellipse ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = fill
            };

            Canvas.SetLeft(ellipse, p.X - ellipse.Width / 2);
            Canvas.SetTop(ellipse, p.Y - ellipse.Height / 2);

            canvas.Children.Add(ellipse);
        }


        public static void DrawBone(this Canvas canvas, Joint first, Joint second, KinectSensor sensor)
        {
            if (first.TrackingState == TrackingState.NotTracked || second.TrackingState == TrackingState.NotTracked) return;

            // first = first.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);
            // second = second.ScaleTo(canvas.ActualWidth, canvas.ActualHeight);

            Color c;
            if (steadyFlag)
                c = Colors.LightSeaGreen;
            else
                c = Colors.Yellow;
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
            if (!inCanvas(canvas, position))
                return;

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
            SolidColorBrush fill = new SolidColorBrush(Colors.White);
            textBlock.Foreground = fill;

            canvas.Children.Add(textBlock); // Add to Canvas
        }

        private static int GetAngle(Joint ax, Joint ay, Joint bx, Joint by)
        {
            double xa = ay.Position.X - ax.Position.X;
            double ya = ay.Position.Y - ax.Position.Y;
            double za = ay.Position.Z - ax.Position.Z;
            double xb = by.Position.X - bx.Position.X;
            double yb = by.Position.Y - bx.Position.Y;
            double zb = by.Position.Z - bx.Position.Z;
            double ans = xa * xb + ya * yb + za * zb;
            double lengtha = Math.Pow(xa, 2) + Math.Pow(ya, 2) + Math.Pow(za, 2);
            lengtha = Math.Pow(lengtha, 0.5);
            double lengthb = Math.Pow(xb, 2) + Math.Pow(yb, 2) + Math.Pow(zb, 2);
            lengthb = Math.Pow(lengthb, 0.5);
            double angle = Math.Acos(ans / (lengtha * lengthb)) / Math.PI * 180;
            return Convert.ToInt32(angle);
        }

        public static Boolean inCanvas(this Canvas canvas, Point p)
        {
            return p.X > 0 && p.X < canvas.ActualWidth && p.Y > 0 && p.Y < canvas.ActualHeight;
        }
        #endregion
    }
}
