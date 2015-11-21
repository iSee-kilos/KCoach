using System.Collections.Generic;
using System.Windows;

using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace KCoach
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private MultiSourceFrameReader _reader;
        private IList<Body> bodies;
        private IList<Body> oldBodies;

        private static int steadyCounter;

        private static int IS_STEADY = 30;

        private static Dictionary<JointType, int> squat;

        private void setSquat()
        {
            squat = new Dictionary<JointType, int>();
            squat[JointType.KneeLeft] = 180;
            squat[JointType.KneeRight] = 180;
            squat[JointType.SpineBase] = 180;
            squat[JointType.SpineMid] = 180;
        }

        private bool inMatch = false;

        private Action currentAction = null;


        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;


        public MainWindow()
        {

            // a bone defined as a line between two joints
            this.bones = new List<Tuple<JointType, JointType>>();

            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            setSquat();


            this.sensor = KinectSensor.GetDefault();
            if (sensor != null)
            {
                sensor.Open();
            }

            _reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body);
            _reader.MultiSourceFrameArrived += ReaderMultiSourceFrameArrived;


            InitializeComponent();

            this.itemsControl.ItemsSource = StaticActionDatabase.Instance.Actions;
        }

        private void ReaderMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    //camera.Source = frame.ToBitmap();
                }
            }

            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();
                    // canvas.UpdateLayout();
                    Boolean steadyFlag = false;
                    if (isSteady())
                    {
                        steadyCounter++;
                        if (steadyCounter > IS_STEADY)
                            steadyFlag = true;

                        Point p = new Point();
                        p.X = canvas.ActualWidth / 2;
                        p.Y = canvas.ActualHeight / 2;
                        canvas.WirteText(p, "steady");
                    }
                    else
                    {
                        steadyCounter = 0;
                        steadyFlag = false;
                        Point p = new Point();
                        p.X = canvas.ActualWidth / 2;
                        p.Y = canvas.ActualHeight / 2;
                        canvas.WirteText(p, "not steady");
                    }
                    if (bodies != null)
                    {
                        oldBodies = bodies;
                    }

                    bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(bodies);
                    foreach (var body in bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                // Draw skeleton.
                                var angles = body.GetJointAngles();
                                var wrongJoints = match(squat, angles);
                                canvas.DrawSkeleton(body, sensor, steadyFlag);
                                if (steadyFlag)
                                    canvas.DrawWrongJoints(body, wrongJoints, sensor);
                            }
                        }
                    }
                }

            }


            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    camera.Source = frame.ToBitmap();
                }
            }
        }

        private bool isSteady()
        {
            if (oldBodies == null || bodies == null)
                return false;

            var body_len = oldBodies.Count;
            double delta = 0.05;
            if (bodies.Count < body_len)
            {
                body_len = bodies.Count;
            }
            for (var i = 0; i < body_len; i++)
            {

                Body ob = oldBodies[i];
                Body nb = bodies[i];
                if (ob == null || nb == null)
                    continue;
                foreach (var key in ob.Joints.Keys)
                {
                    var oj = ob.Joints[key];
                    var nj = nb.Joints[key];
                    if (oj.JointType == JointType.WristLeft || oj.JointType == JointType.WristRight ||
                        oj.JointType == JointType.HandLeft || oj.JointType == JointType.HandRight ||
                        oj.JointType == JointType.HandTipLeft || oj.JointType == JointType.HandTipRight ||
                        oj.JointType == JointType.AnkleLeft || oj.JointType == JointType.AnkleRight ||
                        oj.JointType == JointType.FootLeft || oj.JointType == JointType.FootRight)
                    {
                        continue;
                    }

                    if ((nj.Position.X >= oj.Position.X - delta && nj.Position.X <= oj.Position.X + delta) ||
                        (nj.Position.Y >= oj.Position.Y - delta && nj.Position.Y <= oj.Position.Y + delta) ||
                        (nj.Position.Z >= oj.Position.Z - delta && nj.Position.Z <= oj.Position.Z + delta))
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private JointType[] match(IReadOnlyDictionary<JointType, int> template, IReadOnlyDictionary<JointType, int> action)
        {
            var unmatchTypes = new List<JointType>();
            var delta = 5;


            foreach (var kv in template)
            {
                var userAngle = action[kv.Key];
                var targetAngle = template[kv.Key];
                if (userAngle > targetAngle - delta && userAngle < targetAngle + delta)
                {
                    continue;
                }
                else
                {
                    unmatchTypes.Add(kv.Key);
                }

            }
            return unmatchTypes.ToArray();
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            this.inMatch = false;
            this.currentAction = null;
            backButton.Visibility = System.Windows.Visibility.Hidden;
            canvas.Visibility = Visibility.Hidden;
            camera.Visibility = Visibility.Hidden;
            scrollViewer.Visibility = Visibility.Visible;
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            this.inMatch = true;
            this.currentAction = ((Button)e.OriginalSource).DataContext as Action;
            camera.Visibility = Visibility.Visible;
            canvas.Visibility = Visibility.Visible;
            backButton.Visibility = Visibility.Visible;
            scrollViewer.Visibility = Visibility.Hidden;
        }

    }
}
