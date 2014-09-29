using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Drawing;

using Microsoft.Kinect;

namespace K4W.BasicOverview.UI
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Size fo the RGB pixel in bitmap
        /// </summary>
        private readonly int _bytePerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>
        /// Representation of the Kinect Sensor
        /// </summary>
        private KinectSensor _kinect = null;

        /// <summary>
        /// FrameReader for our coloroutput
        /// </summary>
        private ColorFrameReader _colorReader = null;

        /// <summary>
        /// FrameReader for our depth output
        /// </summary>
        private DepthFrameReader _depthReader = null;

        /// <summary>
        /// FrameReader for our infrared output
        /// </summary>
        private InfraredFrameReader _infraReader = null;

        /// <summary>
        /// FrameReader for our body output
        /// </summary>
        private BodyFrameReader _bodyReader = null;

        /// <summary>
        /// Array of color pixels
        /// </summary>
        private byte[] _colorPixels = null;

        /// <summary>
        /// Array of depth pixels used for the output
        /// </summary>
        private byte[] _depthPixels = null;

        /// <summary>
        /// Array of infrared pixels used for the output
        /// </summary>
        private byte[] _infraPixels = null;

        /// <summary>
        /// Array of depth values
        /// </summary>
        private ushort[] _depthData = null;

        /// <summary>
        /// Array of infrared data
        /// </summary>
        private ushort[] _infraData = null;

        /// <summary>
        /// All tracked bodies
        /// </summary>
        private Body[] _bodies = null;

        /// <summary>
        /// Color WriteableBitmap linked to our UI
        /// </summary>
        private WriteableBitmap _colorBitmap = null;

        /// <summary>
        /// Color WriteableBitmap linked to our UI
        /// </summary>
        private WriteableBitmap _depthBitmap = null;

        /// <summary>
        /// Infrared WriteableBitmap linked to our UI
        /// </summary>
        private WriteableBitmap _infraBitmap = null;


        private ulong[] bodyTrackingIds = new ulong[6];
        // init text
        string[] textToDisplay = new string[6]; //"text not set";
        string[] myMessages = 
                        { 
                            "What is BizSpark?", 
                            "UMBC Rocks!", 
                            "Free software?", 
                            "HackUMBC!!!", 
                            "Azure cloud!", 
                            "Go Retrievers!", 
                            "Free T-shirts?", 
                            "Need caffeine..."
                        };


        /// <summary>
        /// Default CTOR
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Initialize Kinect
            InitializeKinect();

            // init messages
            InitializeMessages();

            // Close Kinect when closing app
            Closing += OnClosing;
        }

        private void InitializeMessages()
        {
            for (int i = 0; i < bodyTrackingIds.Length; i++ )
            {
                textToDisplay[i] = i + "text not set";
                bodyTrackingIds[i] = 0;
            }
        }

        /// <summary>
        /// Close Kinect & Kinect Service
        /// </summary>
        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Close Kinect
            if (_kinect != null) _kinect.Close();
        }


        #region INITIALISATION
        /// <summary>
        /// Initialize Kinect Sensor
        /// </summary>
        private void InitializeKinect()
        {
            // Get first Kinect
            _kinect = KinectSensor.GetDefault();

            if (_kinect == null) return;

            // Open connection
            _kinect.Open();

            // Initialize Camera
            InitializeCamera();

            // Initialize Depth
            InitializeDepth();

            // Initialize Infrared
            InitializeInfrared();

            // Initialize Body
            IntializeBody();
        }

        /// <summary>
        /// Initialize Kinect Camera
        /// </summary>
        private void InitializeCamera()
        {
            if (_kinect == null) return;

            // Get frame description for the color output
            FrameDescription desc = _kinect.ColorFrameSource.FrameDescription;

            // Get the framereader for Color
            _colorReader = _kinect.ColorFrameSource.OpenReader();

            // Allocate pixel array
            _colorPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            // Create new WriteableBitmap
            _colorBitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

            // Link WBMP to UI
            CameraImage.Source = _colorBitmap;

            // Hook-up event
            _colorReader.FrameArrived += OnColorFrameArrived;
        }

        /// <summary>
        /// Initialize Kinect Depth
        /// </summary>
        private void InitializeDepth()
        {
            if (_kinect == null) return;

            // Get frame description for the color output
            FrameDescription desc = _kinect.DepthFrameSource.FrameDescription;

            // Get the framereader for Color
            _depthReader = _kinect.DepthFrameSource.OpenReader();

            // Allocate pixel array
            _depthData = new ushort[desc.Width * desc.Height];
            _depthPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            // Create new WriteableBitmap
            _depthBitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

            // Link WBMP to UI
            DepthImage.Source = _depthBitmap;

            // Hook-up event
            _depthReader.FrameArrived += OnDepthFrameArrived;
        }

        /// <summary>
        /// Initialize Kinect Infrared
        /// </summary>
        private void InitializeInfrared()
        {
            if (_kinect == null) return;

            // Get frame description for the color output
            FrameDescription desc = _kinect.InfraredFrameSource.FrameDescription;

            // Get the framereader for Color
            _infraReader = _kinect.InfraredFrameSource.OpenReader();

            // Allocate pixel array
            _infraData = new ushort[desc.Width * desc.Height];
            _infraPixels = new byte[desc.Width * desc.Height * _bytePerPixel];

            // Create new WriteableBitmap
            _infraBitmap = new WriteableBitmap(desc.Width, desc.Height, 96, 96, PixelFormats.Bgr32, null);

            // Link WBMP to UI
            InfraredImage.Source = _infraBitmap;

            // Hook-up event
            _infraReader.FrameArrived += OnInfraredFrameArrived;
        }

        /// <summary>
        /// Initialize Body Tracking
        /// </summary>
        private void IntializeBody()
        {
            if (_kinect == null) return;

            // Allocate Bodies array
            _bodies = new Body[_kinect.BodyFrameSource.BodyCount];

            // Open reader
            _bodyReader = _kinect.BodyFrameSource.OpenReader();

            // Hook-up event
            _bodyReader.FrameArrived += OnBodyFrameArrived;
        }
        #endregion INITIALISATION


        #region FRAME PROCESSING
        /// <summary>
        /// Process color frames & show in UI
        /// </summary>
        private void OnColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // Get the reference to the color frame
            ColorFrameReference colorRef = e.FrameReference;

            if (colorRef == null) return;

            // Acquire frame for specific reference
            ColorFrame frame = colorRef.AcquireFrame();

            // It's possible that we skipped a frame or it is already gone
            if (frame == null) return;

            using (frame)
            {
                // Get frame description
                FrameDescription frameDesc = frame.FrameDescription;

                // Check if width/height matches
                if (frameDesc.Width == _colorBitmap.PixelWidth && frameDesc.Height == _colorBitmap.PixelHeight)
                {
                    // Copy data to array based on image format
                    if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        frame.CopyRawFrameDataToArray(_colorPixels);
                    }
                    else frame.CopyConvertedFrameDataToArray(_colorPixels, ColorImageFormat.Bgra);

                    // Copy output to bitmap
                    _colorBitmap.WritePixels(
                            new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
                            _colorPixels,
                            frameDesc.Width * _bytePerPixel,
                            0);
                }
            }
        }

        /// <summary>
        /// Process the depth frames and update UI
        /// </summary>
        private void OnDepthFrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            DepthFrameReference refer = e.FrameReference;

            if (refer == null) return;

            DepthFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            using (frame)
            {
                FrameDescription frameDesc = frame.FrameDescription;

                if (((frameDesc.Width * frameDesc.Height) == _depthData.Length) && (frameDesc.Width == _depthBitmap.PixelWidth) && (frameDesc.Height == _depthBitmap.PixelHeight))
                {
                    // Copy depth frames
                    frame.CopyFrameDataToArray(_depthData);

                    // Get min & max depth
                    ushort minDepth = frame.DepthMinReliableDistance;
                    ushort maxDepth = frame.DepthMaxReliableDistance;

                    // Adjust visualisation
                    int colorPixelIndex = 0;
                    for (int i = 0; i < _depthData.Length; ++i)
                    {
                        // Get depth value
                        ushort depth = _depthData[i];

                        if (depth == 0)
                        {
                            _depthPixels[colorPixelIndex++] = 41;
                            _depthPixels[colorPixelIndex++] = 239;
                            _depthPixels[colorPixelIndex++] = 242;
                        }
                        else if (depth < minDepth || depth > maxDepth)
                        {
                            _depthPixels[colorPixelIndex++] = 25;
                            _depthPixels[colorPixelIndex++] = 0;
                            _depthPixels[colorPixelIndex++] = 255;
                        }
                        else
                        {
                            double gray = (Math.Floor((double)depth / 250) * 12.75);

                            _depthPixels[colorPixelIndex++] = (byte)gray;
                            _depthPixels[colorPixelIndex++] = (byte)gray;
                            _depthPixels[colorPixelIndex++] = (byte)gray;
                        }

                        // Increment
                        ++colorPixelIndex;
                    }

                    // Copy output to bitmap
                    _depthBitmap.WritePixels(
                            new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
                            _depthPixels,
                            frameDesc.Width * _bytePerPixel,
                            0);
                }
            }
        }

        /// <summary>
        /// Process the infrared frames and update UI
        /// </summary>
        private void OnInfraredFrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            // Reference to infrared frame
            InfraredFrameReference refer = e.FrameReference;

            if (refer == null) return;

            // Get infrared frame
            InfraredFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            // Process it
            using (frame)
            {
                // Get the description
                FrameDescription frameDesc = frame.FrameDescription;

                if (((frameDesc.Width * frameDesc.Height) == _infraData.Length) && (frameDesc.Width == _infraBitmap.PixelWidth) && (frameDesc.Height == _infraBitmap.PixelHeight))
                {
                    // Copy data
                    frame.CopyFrameDataToArray(_infraData);

                    int colorPixelIndex = 0;

                    for (int i = 0; i < _infraData.Length; ++i)
                    {
                        // Get infrared value
                        ushort ir = _infraData[i];

                        // Bitshift
                        byte intensity = (byte)(ir >> 8);

                        // Assign infrared intensity
                        _infraPixels[colorPixelIndex++] = intensity;
                        _infraPixels[colorPixelIndex++] = intensity;
                        _infraPixels[colorPixelIndex++] = intensity;

                        ++colorPixelIndex;
                    }

                    // Copy output to bitmap
                    _infraBitmap.WritePixels(
                            new Int32Rect(0, 0, frameDesc.Width, frameDesc.Height),
                            _infraPixels,
                            frameDesc.Width * _bytePerPixel,
                            0);
                }
            }
        }

        /// <summary>
        /// Process the body-frames and draw joints
        /// </summary>
        private void OnBodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            // Get frame reference
            BodyFrameReference refer = e.FrameReference;

            if (refer == null) return;

            // Get body frame
            BodyFrame frame = refer.AcquireFrame();

            if (frame == null) return;

            using (frame)
            {
                // Aquire body data
                frame.GetAndRefreshBodyData(_bodies);

                // Clear Skeleton Canvas
                SkeletonCanvas.Children.Clear();

                int bodyIndex = 0;
                // Loop all bodies
                foreach (Body body in _bodies)
                {
                    // Only process tracked bodies
                    if (body.IsTracked)
                    {
                        // replace with custom messages
                        Random rnd = new Random();
                        int randomValue = rnd.Next(0, myMessages.Length);
                        if (bodyTrackingIds[bodyIndex] == body.TrackingId)
                        {
                            // same body, don't update text
                            //textToDisplay = body.TrackingId.ToString();
                        }
                        else
                        {
                            // new body detected, update text!
                            textToDisplay[bodyIndex] = "" + myMessages[randomValue] + "";
                            // set body tracking id!
                            bodyTrackingIds[bodyIndex] = body.TrackingId;
                        }
                        // pass on the message
                        DrawBody(body, textToDisplay[bodyIndex]);
                    }
                    bodyIndex++;
                }
            }
        }

        /// <summary>
        /// Visualize the body
        /// </summary>
        /// <param name="body">Tracked body</param>
        private void DrawBody(Body body, string textToDisplay)
        {
            // Draw points
            foreach (JointType type in body.Joints.Keys)
            {
                // Draw all the body joints
                switch (type)
                {
                    case JointType.Head:
                        DrawJoint(body.Joints[type], 20, Brushes.Yellow, 2, Brushes.White, textToDisplay);
                    //case JointType.FootLeft:
                    //case JointType.FootRight:
                    //    DrawJoint(body.Joints[type], 20, Brushes.Yellow, 2, Brushes.White);
                    //    break;
                    //case JointType.ShoulderLeft:
                    //case JointType.ShoulderRight:
                    //case JointType.HipLeft:
                    //case JointType.HipRight:
                    //    DrawJoint(body.Joints[type], 20, Brushes.YellowGreen, 2, Brushes.White);
                    //    break;
                    //case JointType.ElbowLeft:
                    //case JointType.ElbowRight:
                    //case JointType.KneeLeft:
                    //case JointType.KneeRight:
                    //    DrawJoint(body.Joints[type], 15, Brushes.LawnGreen, 2, Brushes.White);
                    //    break;
                    //case JointType.HandLeft:
                    //    DrawHandJoint(body.Joints[type], body.HandLeftState, 20, 2, Brushes.White);
                    //    break;
                    //case JointType.HandRight:
                    //    DrawHandJoint(body.Joints[type], body.HandRightState, 20, 2, Brushes.White);
                        break;
                    //default:
                    //    DrawJoint(body.Joints[type], 15, Brushes.RoyalBlue, 2, Brushes.White);
                    //    break;
                }
            }
        }

        /// <summary>
        /// Draws a body joint
        /// </summary>
        /// <param name="joint">Joint of the body</param>
        /// <param name="radius">Circle radius</param>
        /// <param name="fill">Fill color</param>
        /// <param name="borderWidth">Thickness of the border</param>
        /// <param name="border">Color of the boder</param>
        private void DrawJoint(Joint joint, double radius, SolidColorBrush fill, double borderWidth, SolidColorBrush border, string textToDisplay = "not set")
        {
            if (joint.TrackingState != TrackingState.Tracked) return;
            
            // Map the CameraPoint to ColorSpace so they match
            ColorSpacePoint colorPoint = _kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);

            // Create the UI element based on the parameters
            Ellipse el = new Ellipse();
            el.Fill = fill;
            el.Stroke = border;
            el.StrokeThickness = borderWidth;
            el.Width = el.Height = radius;


            Ellipse cb = new Ellipse();
            cb.Fill = fill;
            cb.Stroke = border;
            el.StrokeThickness = borderWidth;
            cb.Height = radius * 4;
            cb.Width = radius * 15;

            TextBlock tb = new TextBlock();
            tb.Text = textToDisplay;

            
            tb.FontSize = 34;
            tb.Width = cb.Width;
            tb.Height = cb.Height;

            // Add the Ellipse to the canvas
            //SkeletonCanvas.Children.Add(el);
            SkeletonCanvas.Children.Add(cb);
            SkeletonCanvas.Children.Add(tb);

            // Avoid exceptions based on bad tracking
            if (float.IsInfinity(colorPoint.X) || float.IsInfinity(colorPoint.X)) return;

            // Allign ellipse on canvas (Divide by 2 because image is only 50% of original size)
            //Canvas.SetLeft(el, colorPoint.X / 2);
            //Canvas.SetTop(el, colorPoint.Y / 2);


            Canvas.SetLeft(cb, colorPoint.X / 2 + 60);
            Canvas.SetTop(cb, colorPoint.Y / 2 - 100);
            Canvas.SetLeft(tb, colorPoint.X / 2 + 100);
            Canvas.SetTop(tb, colorPoint.Y / 2 - 80);
        }

        /// <summary>
        /// Draw a body joint for a hand and assigns a specific color based on the handstate
        /// </summary>
        /// <param name="joint">Joint representing a hand</param>
        /// <param name="handState">State of the hand</param>
        private void DrawHandJoint(Joint joint, HandState handState, double radius, double borderWidth, SolidColorBrush border)
        {
            switch (handState)
            {
                case HandState.Lasso:
                    DrawJoint(joint, radius, Brushes.Cyan, borderWidth, border);
                    break;
                case HandState.Open:
                    DrawJoint(joint, radius, Brushes.Green, borderWidth, border);
                    break;
                case HandState.Closed:
                    DrawJoint(joint, radius, Brushes.Red, borderWidth, border);
                    break;
                default:
                    break;
            }
        }
        #endregion FRAME PROCESSING

        #region UI Methods
        private void OnToggleCamera(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Camera");
        }

        private void OnToggleDepth(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Depth");
        }

        private void OnToggleInfrared(object sender, RoutedEventArgs e)
        {
            ChangeVisualMode("Infrared");
        }

        /// <summary>
        /// Change the UI based on the mode
        /// </summary>
        /// <param name="mode">New UI mode</param>
        private void ChangeVisualMode(string mode)
        {
            // Invis all
            CameraGrid.Visibility = Visibility.Collapsed;
            DepthGrid.Visibility = Visibility.Collapsed;
            InfraredGrid.Visibility = Visibility.Collapsed;

            switch (mode)
            {
                case "Camera":
                    CameraGrid.Visibility = Visibility.Visible;
                    break;
                case "Depth":
                    DepthGrid.Visibility = Visibility.Visible;
                    break;
                case "Infrared":
                    InfraredGrid.Visibility = Visibility.Visible;
                    break;
            }
        }
        #endregion UI Methods
    }
}

