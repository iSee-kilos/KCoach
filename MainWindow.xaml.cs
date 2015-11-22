using System.Collections.Generic;
using System.Windows;

using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System;
using System.Windows.Shapes;
using System.Windows.Controls;
using Microsoft.Kinect.Wpf.Controls;
using Microsoft.Samples.Kinect.DepthBasics;
using Microsoft.Kinect.Input;

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
        private static int sampleCounter;

        private static int IS_STEADY = 50;

        private static int SAMPLE_THR = 10;

        private bool inMatch = false;

        private Action currentAction = null;

        /*
        0 not start and not steady
        1 steady (next frame will immediately enter stage 2)
        2 start (and steady)
        3 start and not steady
        4 steady (next frame will immediately enter stage 5)
        5 end
        */
        private static int stage = 0;
        private static bool startFlag = false;
        private static bool pauseFlag = false;
        private static int PAUSE_INTERVAL = 60;
        private static int pauseCounter = 0;

        private static int n_total_movement = 0;
        private static int n_succ_movement = 0;


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

            InitializeComponent();

            this.Loaded += MainWindow_Loaded;

            KinectRegion.SetKinectRegion(this, kinectRegion);

            App app = ((App)Application.Current);
            app.KinectRegion = kinectRegion;

            // Use the default sensor
            this.kinectRegion.KinectSensor = KinectSensor.GetDefault();

            this.sensor = this.kinectRegion.KinectSensor;
            if (sensor != null)
            {
                sensor.Open();
            }

            _reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Body);
            _reader.MultiSourceFrameArrived += ReaderMultiSourceFrameArrived;

            this.itemsControl.ItemsSource = StaticActionDatabase.Instance.Actions;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            KinectCoreWindow kinectCoreWindow = KinectCoreWindow.GetForCurrentThread();
            kinectCoreWindow.PointerMoved += kinectCoreWindow_PointerMoved;
        }

        private void ReaderMultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {


            if (pauseFlag)
            {
                pauseCounter++;
                if (pauseCounter > PAUSE_INTERVAL)
                {
                    pauseFlag = false;
                    pauseCounter = 0;
                    steadyCounter = 0;
                    IS_STEADY = 50;
                    startFlag = false;
                }
                return;
            }
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
                    sampleCounter++;
                    canvas.Children.Clear();
                    String s1 = "total movements: " + n_total_movement.ToString();
                    String s2 = "successful movements: " + n_succ_movement.ToString();
                    String s3 = "success ratio: " + ((double)((double)n_succ_movement / (double)n_total_movement) * 100).ToString() + "%";
                    String s4_1 = "Start pls!";
                    String s4_2 = "Keep moving!";
                    Point p1 = new Point(); p1.X = canvas.ActualWidth - 300; p1.Y = 100;
                    canvas.WirteText(p1, s1, Colors.Red, 20);
                    Point p2 = new Point(); p2.X = canvas.ActualWidth - 300; p2.Y = 200;
                    canvas.WirteText(p2, s2, Colors.Red, 20);
                    Point p3 = new Point(); p3.X = canvas.ActualWidth - 300; p3.Y = 300;
                    canvas.WirteText(p3, s3, Colors.Red, 20);
                    Point p4 = new Point(); p4.X = canvas.ActualWidth - 300; p4.Y = 400;
                    if (startFlag)
                        canvas.WirteText(p4, s4_2, Colors.Red, 20);
                    else
                        canvas.WirteText(p4, s4_1, Colors.Red, 20);

                    // canvas.UpdateLayout();
                    Boolean steadyFlag = false;
                    
                    bodies = new Body[frame.BodyFrameSource.BodyCount];
                    frame.GetAndRefreshBodyData(bodies);

                    if (isSteady())
                    {
                        steadyCounter++;
                        if (steadyCounter > IS_STEADY && !steadyFlag)
                        {
                            oldBodies = bodies;
                            sampleCounter = 0;
                            steadyFlag = true;
                        }
                            

                        //Point p = new Point();
                        //p.X = canvas.ActualWidth / 2;
                        //p.Y = canvas.ActualHeight / 2;
                        //canvas.WirteText(p, "steady", Colors.Red, 20);
                    }
                    else
                    {
                        steadyCounter = 0;

                        if (steadyFlag)
                        {
                            // this.Close();
                            oldBodies = bodies;
                            sampleCounter = 0;
                            steadyFlag = false;
                        }
                                         
                        
                        //Point p = new Point();
                        //p.X = canvas.ActualWidth / 2;
                        //p.Y = canvas.ActualHeight / 2;
                        //canvas.WirteText(p, "not steady", Colors.Red, 20);
                    }
                    if (sampleCounter > SAMPLE_THR)
                    {
                        oldBodies = bodies;
                        sampleCounter = 0;
                    }

                    foreach (var body in bodies)
                    {
                        if (body != null)
                        {
                            if (body.IsTracked)
                            {
                                // Draw skeleton.
                                var angles = body.GetJointAngles();
                                if (currentAction != null)
                                {
                                    /*
                                    0 not start and not steady
                                    1 steady (next frame will immediately enter stage 2)
                                    2 start (and steady)
                                    3 start and not steady
                                    4 steady (next frame will immediately enter stage 5)
                                    5 end
                                    */
                                    JointType[] wrongJoints = null;


                                    //if (stage == 0 && steadyFlag)
                                    //{
                                    //    stage = 1;
                                    //}
                                    //else if (stage == 1 && steadyFlag)
                                    //{
                                    //    stage = 2;
                                    //}
                                    //else if (stage == 2 && !steadyFlag)
                                    //{
                                    //    stage = 3;
                                    //}
                                    //else if (stage == 3 && steadyFlag)
                                    //{
                                    //    stage = 4;
                                    //}
                                    //else if (stage == 4 && steadyFlag)
                                    //{
                                    //    stage = 5;
                                    //}
                                    //else if (stage == 5 && !steadyFlag)
                                    //{
                                    //    stage = 0;
                                    //}

                                    //if (stage == 1)
                                    //{
                                    //    wrongJoints = match(currentAction.StartTemplate, angles);
                                    //    if (wrongJoints.Length == 0)
                                    //        n_total_movement++;
                                    //}
                                    //else if (stage == 4)
                                    //{
                                    //    wrongJoints = match(currentAction.EndTemplate, angles);
                                    //    if (wrongJoints.Length == 0)
                                    //        n_succ_movement++;
                                    //}
                                    if (!startFlag && steadyFlag)
                                    {
                                        wrongJoints = match(currentAction.StartTemplate, angles);
                                        if (wrongJoints.Length == 0)
                                        {
                                            startFlag = true;
                                            steadyCounter = 0;
                                            IS_STEADY = 80;
                                            n_total_movement++;
                                        }
                                            
                                    }
                                    else if (startFlag && steadyFlag)
                                    {
                                        Point p5 = new Point(); p5.X = canvas.ActualWidth - 300; p5.Y = 450;
                                        wrongJoints = match(currentAction.EndTemplate, angles);
                                        if (wrongJoints.Length == 0)
                                        {
                                            n_succ_movement++;
                                            startFlag = false;
                                            IS_STEADY = 50;
                                            pauseFlag = true;
                                            canvas.WirteText(p5, "Suceess", Colors.Red, 20);
                                            canvas.WirteText(p4, "", Colors.Red, 20);
                                        }
                                        else
                                        {
                                            canvas.WirteText(p5, "Failed", Colors.Red, 20);
                                            canvas.WirteText(p4, "", Colors.Red, 20);
                                        }
                                            
                                        pauseFlag = true;
                                    }

                                    canvas.DrawSkeleton(body, sensor, steadyFlag);
                                    if (steadyFlag && wrongJoints != null)
                                    {
                                        canvas.DrawWrongJoints(body, wrongJoints, sensor);
                                    }   
                                }
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
                    int width = frame.FrameDescription.Width;
                    int height = frame.FrameDescription.Height;
                    canvas.setMaxSize(width, height);
                }
            }
        }

        private bool isSteady()
        {
            if (oldBodies == null || bodies == null)
                return false;


            var body_len = oldBodies.Count;
            double delta = 0.03;
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
                if (match(ob.GetJointAngles(), nb.GetJointAngles()).Length > 0)
                    return false;
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
            //Point p = new Point();
            //p.X = canvas.ActualWidth / 2;
            //p.Y = canvas.ActualHeight / 2;
            //canvas.WirteText(p, "null", Colors.Red, 20);
            return true;
        }

        private JointType[] match(IDictionary<JointType, int> template, IDictionary<JointType, int> action)
        {
            var unmatchTypes = new List<JointType>();
            var delta = 10;

            if (template == null || action == null)
                return unmatchTypes.ToArray();


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
            SwitchButtons();
        }

        private void ButtonClick(object sender, RoutedEventArgs e)
        {
            this.inMatch = true;
            this.currentAction = ((Button)e.OriginalSource).DataContext as Action;
            SwitchButtons();
            n_total_movement = 0;
            n_succ_movement = 0;
            stage = 0;
    }

        private void SwitchButtons()
        {
            backButton.Visibility = this.inMatch ? Visibility.Visible : System.Windows.Visibility.Hidden;
            canvas.Visibility = this.inMatch ? Visibility.Visible : Visibility.Hidden;
            camera.Visibility = this.inMatch ? Visibility.Visible : Visibility.Hidden;
            scrollViewer.Visibility = this.inMatch ? Visibility.Hidden : Visibility.Visible;
       //     textBlock.Visibility = this.inMatch ? Visibility.Hidden : Visibility.Visible;
        }



        private const double DotHeight = 60;
        private const double DotWidth = 60;
        private SolidColorBrush blackBrush = Brushes.Black;
        private SolidColorBrush greenBrush = Brushes.Green;
        private SolidColorBrush yellowBrush = Brushes.Yellow;
        private TimeSpan lastTime;

        private void kinectCoreWindow_PointerMoved(object sender, KinectPointerEventArgs args)
        {
            KinectPointerPoint kinectPointerPoint = args.CurrentPoint;
            if (lastTime == TimeSpan.Zero || lastTime != kinectPointerPoint.Properties.BodyTimeCounter)
            {
                lastTime = kinectPointerPoint.Properties.BodyTimeCounter;
                pointerCanvas.Children.Clear();
            }
        }

    }
}
