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
using Microsoft.Maps.MapControl.WPF;
using System.IO.Ports;
using System.Windows.Threading;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Maps.MapControl.WPF.Design;
using Excel = Microsoft.Office.Interop.Excel;
using System.Threading;
using System.Device.Location;
using Microsoft.Expression.Encoder.Devices;
using WebcamControl;
using System.Windows.Media.Animation;
using AForge.Video.DirectShow;
using AForge.Video;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay;
using System.Globalization;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.ComponentModel;

namespace GCS_WPF_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region DeklarasiVariabel
        PointCollection newPoint;

        private static BackgroundWorker bgWorker;

        private int StatusWaypoint = 0;

        //Path to the model file
        private const string MODEL_PATH_FW = @"/Resources/ModelFWdariBlender.obj";
        private const string MODEL_PATH_QUAD = "\\Resources\\QuaddariBlender.obj";
        ModelVisual3D device3D = new ModelVisual3D();

        private FilterInfoCollection VideoCaptureDevices;
        private VideoCaptureDevice FinalVideo;

        DBHelper db;

        private static Location position = new Location(-7.778301, 110.374690);
        List<MapPolyline> listPolyline;
        private static int zoom = 17, second=0, minute=0, hour=0, waypointIndex=1;
        double x;
        private string TimeStart;
        private DateTime start, stop;
        LocationConverter locConverter = new LocationConverter();
        private GeoCoordinateWatcher Watcher = null;

        SerialPort portGCS;
        DispatcherTimer timer, timerFlight, timerGraph;
        LocationCollection locCollection;

        private static double altitude, yaw, pitch, roll, Lat, Lng, jarak_cetak=0, battery;
        double[] lat = new double[4];
        double[] lng = new double[4];
        double lat1, lat2, lat3, lat4;
        double lng1, lng2, lng3, lng4;

        double YawBaru=0, PitchBaru=0, RollBaru=0;
        double YawLama = 0, PitchLama = 0, RollLama = 0;

        int statusUAV = 0;
        int jumlahWaypoint = 0;

        #endregion

        public MainWindow(int statusUAV)
        {
            this.statusUAV = statusUAV;
            InitializeComponent();
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            // Deklarasi background worker, masih coba2
            bgWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            bgWorker.DoWork += bgWorker_DoWork;
            bgWorker.ProgressChanged += bgWorker_ProgressChanged;
            bgWorker.RunWorkerCompleted += bgWorker_RunWorkerCompleted;
            if (bgWorker.IsBusy)
            {

                bgWorker.CancelAsync();

                Console.ReadLine();

            }
            Uri uri;
            if (statusUAV == 0)
            {
                //Load3DModel();
                device3D.Content = Display3d(System.AppDomain.CurrentDomain.BaseDirectory + "modelFWdariblender.obj");
                // Add to view port
                viewPort3d.Children.Add(device3D);
                //Pitch3D(-90);
                jumlahWaypoint = 1;
            }
            else
            {
                //Load3DModel();
                device3D.Content = Display3d(System.AppDomain.CurrentDomain.BaseDirectory + "QuaddariBlender.obj");
                // Add to view port
                viewPort3d.Children.Add(device3D);
                //Pitch3D(-90);
                jumlahWaypoint = 4;
            }


            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;
            db = new DBHelper();
            //InitiateAttitudeIndicator();
            ConnectingWebcam2();
            //GetLaptopLocation();
            CheckFolderFlightRecord();
            batt_icon.Visibility = Visibility.Visible;
            batt_icon_warning.Visibility = Visibility.Hidden;
            batt_icon_low.Visibility = Visibility.Hidden;
            PopulateComboBoxRecord();
            //Map dibuat focus supaya bisa di double click
            myMap.Focus();
            myMap.Mode = new AerialMode(true);
            //db = new DBHelper();
            listPolyline = new List<MapPolyline>();
            //db.OpenConnection();
            //LoadMap();
            slider_zoom_map.Visibility = Visibility.Hidden;
            PortBaudSetting();
        }

        #region MultiThreading
        private void KontenYPRBackground()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                
            }));
        }
        #endregion

        #region 3D Visualization
        //private void Load3DModel()
        //{
        //    //Import 3D model file
        //    ObjReader CurrentHelixObjReader = new ObjReader();
        //    // Model3DGroup MyModel = CurrentHelixObjReader.Read(@"D:\3DModel\dinosaur_FBX\dinosaur.fbx");
        //    Model3DGroup MyModel = CurrentHelixObjReader.Read(MODEL_PATH);
        //    modell.Content = MyModel;
        //    //MyModel.Children.Add(MyModel);
        //}
        private Model3D Display3d(string model)
        {
            Model3D device = null;
            try
            {
                //Adding a gesture here
                //viewPort3d.RotateGesture = new MouseGesture(MouseAction.LeftClick);

                //Import 3D model file
                //ObjReader CurrentHelixObjReader = new ObjReader();
                //// Model3DGroup MyModel = CurrentHelixObjReader.Read(@"D:\3DModel\dinosaur_FBX\dinosaur.fbx");
                //Model3DGroup MyModel = CurrentHelixObjReader.Read(MODEL_PATH);
                //modell.Content = MyModel;
                //MyModel.Children.Add(MyModel);
                ModelImporter import = new ModelImporter();

                //Load the 3D model file
                device = import.Load(model);
            }
            catch (Exception e)
            {
                // Handle exception in case can not file 3D model
                MessageBox.Show("Exception Error : " + e.StackTrace);
            }
            return device;
        }
        private void Yaw3D(double angleYaw)
        {
            var axis = new Vector3D(0, 0, 1);
            var angle = angleYaw;

            var matrix = device3D.Transform.Value;
            matrix.Rotate(new Quaternion(axis, angle*-1));

            device3D.Transform = new MatrixTransform3D(matrix);
        }
        private void Pitch3D(double anglePitch)
        {
            var axis = new Vector3D(1, 0, 0);
            var angle = anglePitch;

            var matrix = device3D.Transform.Value;
            matrix.Rotate(new Quaternion(axis, angle*-1));

            device3D.Transform = new MatrixTransform3D(matrix);
        }
        private void Roll3D(double angleRoll)
        {
            var axis = new Vector3D(0, 1, 0);
            var angle = angleRoll;

            var matrix = device3D.Transform.Value;
            matrix.Rotate(new Quaternion(axis, angle*-1));

            device3D.Transform = new MatrixTransform3D(matrix);
        }
        #endregion

        #region Webcam plugin WebCamControl (gagal)
        //public void ConnectingWebcam()
        //{
        //    Binding binding_1 = new Binding("SelectedValue");
        //    binding_1.Source = VideoDevicesComboBox;
        //    WebcamCtrl.SetBinding(Webcam.VideoDeviceProperty, binding_1);
        //    WebcamCtrl.FrameRate = 30;
        //    WebcamCtrl.FrameSize = new System.Drawing.Size(1280, 720);

        //    string videoPath = @"E:\VideoClips";
        //    if (!Directory.Exists(videoPath))
        //    {
        //        Directory.CreateDirectory(videoPath);
        //    }
        //    WebcamCtrl.VideoDirectory = videoPath;

        //    // Find available a/v devices
        //    var vidDevices = EncoderDevices.FindDevices(EncoderDeviceType.Video);
        //    VideoDevicesComboBox.ItemsSource = vidDevices;
        //    VideoDevicesComboBox.SelectedIndex = 0;
        //}
        #endregion

        #region DynamicDataDisplay
        void PlotGraphic()
        {
            newPoint = new PointCollection();
            timerGraph = new DispatcherTimer();
            timerGraph.Interval = TimeSpan.FromMilliseconds(600);
            timerGraph.Tick += new EventHandler(timerGraph_Tick);
            timerGraph.Start();

            var ds = new EnumerableDataSource<Points>(newPoint);
            ds.SetXMapping(x => x.Waktu);
            ds.SetYMapping(y => y.Variabel);
            plotter.AddLineGraph(ds, Colors.Green, 2, "Altitude"); // to use this method you need to add manually "using Microsoft.Research.DynamicDataDisplay;"
            plotter.FitToView();
        }
        void timerGraph_Tick(object sender, EventArgs e)
        {
            newPoint.Add(new Points(altitude, x / 1000));
            x += 600;
        }
        #endregion

        #region ConnectWebcam
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::              Connect Webcam, plugin AForge.NET                 :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public void ConnectingWebcam2()
        {
            VideoCaptureDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo VideoCaptureDevice in VideoCaptureDevices)
            {
                VideoDevicesComboBox.Items.Add(VideoCaptureDevice.Name);
            }
            VideoDevicesComboBox.SelectedIndex = 0;
        }
        void FinalVideo_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            System.Drawing.Image img = (System.Drawing.Bitmap)eventArgs.Frame.Clone();
            //BitmapImage bi = new BitmapImage();
            //bi.BeginInit();
            //bi = Compatibility.Compatibility.BitmaptoBitmapImage((System.Drawing.Bitmap)video); //download the compatibility api below
            //image1.Source = bi;
            //bi.EndInit();
            MemoryStream ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();

            bi.Freeze();
            Dispatcher.BeginInvoke(new System.Threading.ThreadStart(delegate
            {
                image1.Source = bi;
            }));
        }
        private void btnConnectWebcam_Click(object sender, RoutedEventArgs e)
        {
            if (btnConnectWebcam.Content.Equals("CONNECT"))
            {
                btnConnectWebcam.Content = "STOP";
                FinalVideo = new VideoCaptureDevice(VideoCaptureDevices[VideoDevicesComboBox.SelectedIndex].MonikerString);
                FinalVideo.NewFrame += new NewFrameEventHandler(FinalVideo_NewFrame);
                FinalVideo.Start();
                //try
                //{
                //    // Display webcam video
                //    WebcamCtrl.StartPreview();
                //    WebcamCtrl.StartRecording();
                //}
                //catch (Microsoft.Expression.Encoder.SystemErrorException ex)
                //{
                //    MessageBox.Show("Device is in use by another application");
                //}
            }
            else
            {
                btnConnectWebcam.Content = "CONNECT";
                if (FinalVideo.IsRunning)
                {
                    FinalVideo.Stop();
                    image1.Source = null;
                }
                // Stop the display of webcam video.
                //WebcamCtrl.StopPreview();
                //WebcamCtrl.StopRecording();
            }
        }
        #endregion

        #region GetLaptopLocation
        public void GetLaptopLocation()
        {
            // Create the watcher.
            Watcher = new GeoCoordinateWatcher();

            // Catch the StatusChanged event.
            Watcher.StatusChanged += Watcher_StatusChanged;

            // Start the watcher.
            Watcher.Start();
        }
        // The watcher's status has change. See if it is ready.
        private void Watcher_StatusChanged(object sender,
            GeoPositionStatusChangedEventArgs e)
        {
            if (e.Status == GeoPositionStatus.Ready)
            {
                // Display the latitude and longitude.
                if (Watcher.Position.Location.IsUnknown)
                {
                    MessageBox.Show("Tidak bisa melacak lokasi laptop ini");
                }
                else
                {
                    GeoCoordinate location =
                        Watcher.Position.Location;
                    double lat = location.Latitude;
                    double lng = location.Longitude;
                    AddCustomPin("pinHome.png", lat, lng, "Lokasi GCS");
                    BoxCommand.Text = lat + "," + lng;
                    Location deviceLoc = new Location(lat, lng);
                    myMap.Center = deviceLoc;
                    myMap.ZoomLevel = zoom;
                    //Pushpin pin = new Pushpin();
                    //pin.Location = deviceLoc;
                    //myMap.Children.Add(pin);
                }
            }
        }
        #endregion

        #region EverythingAboutMap
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                        Load Map awal                           :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void LoadMap()
        {
            //Pushpin pin = new Pushpin();
            //pin.Location = position;
            //myMap.Center = position; //center position sesuai lokasi drone
            //myMap.ZoomLevel = zoom;
            //myMap.Children.Add(pin);
            slider_zoom_map.Value = zoom;
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                    Klik 2x di map, add pushpin                 :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void MapWithPushpins_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (StatusWaypoint == 0)
            {
                MessageBox.Show("Klik terlebih dahulu tombol waypoint di bagian Control.");
            }
            else
            {
                // Disables the default mouse double-click action.
                e.Handled = true;

                // Determine the location to place the pushpin at on the map.

                //Get the mouse click coordinates
                Point mousePosition = e.GetPosition(myMap);
                //Convert the mouse coordinates to a location on the map
                Location pinLocation = myMap.ViewportPointToLocation(mousePosition);

                // The pushpin to add to the map.
                Pushpin pin = new Pushpin();
                pin.Location = pinLocation;
                // Adds the pushpin to the map.
                //myMap.Children.Add(pin);
                //Ambil latitude dan longitude dari pushpin
                double Latitude = pin.Location.Latitude;
                double Longitude = pin.Location.Longitude;
                //waypointIndex default value = 1
                if (waypointIndex == jumlahWaypoint+1)
                {
                    MessageBox.Show("Silakan hapus terlebih dahulu waypoint yang ada. Maksimal hanya " + jumlahWaypoint +" waypoint!");
                    //i = 1;
                    ////myMap.Children.Clear();
                    //Image image = new Image();
                    //removePin(image);
                }
                else
                {
                    lat[waypointIndex - 1] = Latitude; lng[waypointIndex - 1] = Longitude;
                    AddCustomPin("pin.png", Latitude, Longitude, "Point ke-" + waypointIndex);
                    //BoxTestSerial.Text = (string.Format("{0:0.000000}", Latitude) + "," + string.Format("{0:0.000000}", Longitude));
                    //Kirim latitude dan longitude ke controller
                    try
                    {
                        //int x = 1;
                        //byte[] b = BitConverter.GetBytes(x);
                        //portGCS.Write(b, 0, 4);
                        //portGCS.Write("SEMPAK:");
                        //portGCS.Write("waypoint:");
                        if (waypointIndex == jumlahWaypoint)
                        {
                            string datapoint = "";
                            MessageBox.Show("Klik start untuk memulai waypoint");
                            if (statusUAV == 0)
                            {
                                datapoint = string.Format("@20#{0:0.0000000}#{1:0.0000000}*",
                                new object[] { this.lat[0], this.lng[0] });
                            }
                            else
                            {
                                datapoint = string.Format("@20#{0:0.0000000}#{1:0.0000000}#{2:0.0000000}#{3:0.0000000}#{4:0.0000000}#{5:0.0000000}#{6:0.0000000}#{7:0.0000000}*",
                                new object[] { this.lat[0], this.lng[0], this.lat[1], this.lng[1], this.lat[2], this.lng[2], this.lat[3], this.lng[3] });
                            }
                            getDataWaypoint(datapoint);
                            //portGCS.Write(datapoint);
                            //Console.WriteLine(datapoint);
                        }
                        waypointIndex++;
                        string lat = string.Format("{0:0.000000}", Latitude);
                        //portGCS.Write(lat + ":");
                        string lng = string.Format("{0:0.000000}", Longitude);
                        //portGCS.Write(lng + ":");
                        //label_Test.Content = lat + "," + lng;
                        string time = string.Format("{0:HH:mm:ss}", DateTime.Now);
                        //db.InsertData("", "", "", "", Convert.ToString(lat), Convert.ToString(lng), time);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            
        }
        private void AddCustomPin(string NamaGambar, double latt, double lngg, string teks)
        {
            MapLayer imageLayer = new MapLayer();
            Image image = new Image();
            Canvas canvas = new Canvas();
            TextBlock txt = new TextBlock();

            canvas.Background = null;

            image.Height = 30;
            image.Width = 30;
            //Define the URI location of the image
            BitmapImage myBitmap = new BitmapImage();
            Uri uri = new Uri("/Resources/" + NamaGambar, UriKind.Relative);
            myBitmap.BeginInit();
            myBitmap.UriSource = uri;
            myBitmap.DecodePixelHeight = 150;
            myBitmap.EndInit();
            image.Source = myBitmap;
            image.Opacity = 1;
            //image.Stretch = System.Windows.Media.Stretch.Fill;

            txt.Text = teks;
            txt.FontSize = 15;
            txt.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
            txt.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);

            // Add Child Elements to Canvas
            // Set Canvas position
            //Canvas.SetLeft(image, 10);
            //Canvas.SetTop(image, 10);

            // Add Custom pin to Canvas
            canvas.Children.Add(image);
            // Add teks to Canvas
            Canvas.SetLeft(txt, 30);
            Canvas.SetTop(txt, 30);
            canvas.Children.Add(txt);

            Location loc = new Location(latt, lngg);
            //Center the image around the location specified
            PositionOrigin position = PositionOrigin.Center;
            //Add the image to the defined map layer
            MapLayer.SetPosition(canvas, loc);
            imageLayer.AddChild(canvas, loc, position);
            imageLayer.Tag = "waypointPin";
            //Add the image layer to the map
            myMap.Children.Add(imageLayer);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                Zoom level berubah sesuai slider                :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void slider_zoom_map_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            myMap.ZoomLevel = Convert.ToInt32(e.NewValue);
        }

        #endregion

        #region FlightTime
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                  Timer utk hitung Flight Time                  :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void TimerFlightTime()
        {
            timerFlight = new DispatcherTimer();
            //Interval 900ms dan dimulai saat sudah nunggu 900ms dulu..
            timerFlight.Interval = new TimeSpan(0, 0, 1);
            //Interval 900ms dan dimulai saat itu juga
            //timerFlight.Interval = TimeSpan.FromMilliseconds(1000);
            timerFlight.Tick += new EventHandler(StartTimer);
            timerFlight.Start();
        }
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                  Update UI timer flight time                   :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        void StartTimer(object sender, EventArgs e)
        {
            second++;
            if (second > 59)
            {
                second = 0; minute++;
            }
            if (minute > 59)
            {
                minute = 0; hour++;
            }
            label_second.Content = string.Format("{0:00}", second);
            label_minute.Content = string.Format("{0:00}", minute);
            label_hour.Content = string.Format("{0:00}", hour);
        }
        #endregion

        #region SettingPortBaud
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                  Set comboBox port dan baud rate               :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void PortBaudSetting()
        {
            comboBoxPort.Items.Clear();
            comboBoxBaud.Items.Clear();
            //show list of valid com ports
            foreach (string s in SerialPort.GetPortNames())
            {
                comboBoxPort.Items.Add(s);
            }
            //comboBoxPort.Items.Add("COM 3");
            comboBoxPort.SelectedIndex = 0;
            //show list of valid baud rate
            int[] baudRate = { 4800, 9600, 19200, 38400, 57600, 115200, 230400 };
            for (int x = 0; x < baudRate.Length; x++)
            {
                comboBoxBaud.Items.Add(baudRate[x]);
            }
            comboBoxBaud.SelectedIndex = 1;
        }
        #endregion

        #region DraggableWindow
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::        Supaya kalau border atas di-drag, form bisa gerak       :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void rectangle2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }
        #endregion

        #region Connect&Stop
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                  Jika button CONNECT/STOP di klik              :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (btnConnect.Content.Equals("CONNECT"))
            {
                PlotGraphic();
                ConnectPortBaudAttempt();
                btnConnect.Content = "STOP";
                //Mulai Timer
                TimeStart = string.Format("FlightLog__{0:dd_MM_yyyy__HH_mm_ss}", DateTime.Now);
                db.CreateTable2(TimeStart);
                start = DateTime.Now;
                //Timer();
                TimerFlightTime();
                //portGCS.Write("SEMPAK:");
            }
            else
            {
                btnConnect.Content = "CONNECT";
                //Timer dihentikan
                //timer.Stop();
                //Jangan lupa flight time juga di save ke Database
                timerFlight.Stop();
                //Dibawah ini adalah operasi utk menghitung TotalFlightTime
                stop = DateTime.Now;
                TimeSpan span = stop.Subtract(start);
                string TotalHours = Convert.ToString(span.Hours);
                string TotalMinutes = Convert.ToString(span.Minutes);
                string TotalSeconds = Convert.ToString(span.Seconds);

                //port di close supaya transmit data dihentikan
                if (portGCS.IsOpen)
                {
                    CloseSerialOnExit();
                    //Thread CloseDown = new Thread(new ThreadStart(CloseSerialOnExit)); //close port in new thread to avoid hang
                    //CloseDown.Start(); //close port in new thread to avoid hang
                }
                db.ExcelSave(TimeStart, TotalHours, TotalMinutes, TotalSeconds);
                RefreshUI();
                //db.DeleteAllData("GCS_DB");
            }
        }
        private void CloseSerialOnExit()
        {
            try
            {
                //portGCS.Close(); //close the serial port
                //myMap.Children.Clear();
                //myMap.ZoomLevel = 1;
                bgWorker.RunWorkerAsync();
                //Dispatcher.BeginInvoke(new Action(() =>
                //{
                //    //bgWorker.CancelAsync();
                //    //Thread.Sleep(1000);
                //    portGCS.DataReceived -= portGCS_DataReceived;
                //    portGCS.Close(); //close the serial port
                //    myMap.Children.Clear();
                //    myMap.ZoomLevel = 1;
                //}));
            }

            catch (Exception ex)
            {
                MessageBox.Show("Error DC Port : " + ex.Message); //catch any serial port closing error messages

            }
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                    Percobaan connect ke port                   :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void ConnectPortBaudAttempt()
        {
            try
            {
                portGCS = new SerialPort();
                //port sesuai yang dipilih dari combobox
                portGCS.PortName = Convert.ToString(comboBoxPort.SelectedItem);
                //baud rate sesuai yang dipilih dari combobox
                portGCS.BaudRate = Convert.ToInt32(comboBoxBaud.SelectedItem);
                Console.WriteLine(portGCS.BaudRate);
                Console.WriteLine(portGCS.PortName);
                portGCS.Open();
                //Data yang diterima, dioperasikan di method portGCS_DataReceived
                portGCS.DataReceived += new SerialDataReceivedEventHandler(portGCS_DataReceived);
                myMap.ZoomLevel = 17;
                // Background worker jalan async, gak ganggu UI thread (belum ditest)
                //bgWorker.RunWorkerAsync(portGCS);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ConnectPortBaud Error : " + ex.Message);
            }
        }
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::         Jika berhasil connect port, method ini jalan           :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void portGCS_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data_received;

            try
            {
                //data_received adalah baris yang dibaca dari data yang dikirimkan melalui port
                data_received = portGCS.ReadLine();
                //data_received akan diolah di method TerimaData
                //Thread ThreadDatadanDatabase = new Thread(() =>
                //{
                //    TerimaData(data_received);
                //    BoxDataReceived.Text += data_received + "\n";
                //});
                //ThreadDatadanDatabase.Start();
                //Thread.CurrentThread.Priority = ThreadPriority.Highest;
                //bgWorker.RunWorkerAsync(portGCS);
                Dispatcher.Invoke((Action)(() => TerimaData(data_received)));
                //KODING DIBAWAH JANGAN DI-UNCOMMENT
                //Dispatcher.Invoke((Action)(() => BoxDataReceived.Text += data_received + "\n"));
                //TerimaData(data_received);
                //BoxDataReceived.Text += data_received + "\n";
                //Dispatcher.Invoke((Action)(() => PortBaudSetting()));

            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Terima Data :" + ex.Message);
            }
        }
        #endregion

        #region PenerimaanDataDariDrone
        #region Timer ra kanggo
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::             Timer utk sinkronisasi transmisi data              :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void Timer()
        {
            timer = new DispatcherTimer();
            //Interval 900ms dan dimulai saat sudah nunggu 900ms dulu..
            timer.Interval = new TimeSpan(0, 0, 0, 0, 900);
            //Interval 900ms dan dimulai saat itu juga
            //timer.Interval = TimeSpan.FromMilliseconds(900);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::Yang dilakukan jika timer sudah sesuai dengan waktu yg ditentukan::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        void timer_Tick(object sender, EventArgs e)
        {
            //GCS_DB_MODEL model1 = db.GetDataModel("GCS_DB");
            //TrackRoute("GCS_DB");
            //TrackDroneIcon();
            //position = new Location(Convert.ToDouble(model1.Lat), Convert.ToDouble(model1.Lng));
            LoadMap();
        }
        #endregion
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //:: Pengolahan data, data yang diterima diolah dan ditampilkan ke UI::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void TerimaData(string data_received)
        {
            try
            {
                string[] data;
                //BoxDataReceived.ScrollToEnd();
                string time = string.Format("{0:HH:mm:ss}", DateTime.Now);
                #region TerimaDataAxellOld
                ////String yang diterima dari port dipisah menggunakan pemisah yang disepakati
                //data = data_received.Split(' ');
                ////Data yang diterima di convert ke tipe data yang sesuai
                //altitude = Convert.ToInt32(data[0]);
                //yaw = Convert.ToInt32(data[1]);
                //pitch = Convert.ToInt32(data[2]);
                //roll = Convert.ToInt32(data[3]);
                //Lat = Convert.ToDouble(data[4]);
                //Lng = Convert.ToDouble(data[5]);
                //string test = data[6];
                //battery = Convert.ToDouble(data[7]);
                //label_Test.Content = test;
                #endregion
                data_received = Convert.ToString(data_received);
                data = data_received.Split('#');
                //MessageBox.Show(data[0].ToString());
                //Waypoint
                //Console.WriteLine(data_received);
                //Console.WriteLine(data[1]);
                if (data[0].ToString().Equals("@20"))
                {

                    int x = 1, y = 2;
                    //lat1 = Convert.ToDouble(data[1]);
                    //lng1 = Convert.ToDouble(data[2]);
                    //lat2 = Convert.ToDouble(data[3]);
                    //lng2 = Convert.ToDouble(data[4]);
                    //lat3 = Convert.ToDouble(data[5]);
                    //lng3 = Convert.ToDouble(data[6]);
                    //lat4 = Convert.ToDouble(data[7]);
                    //lng4 = Convert.ToDouble(data[8]);

                    for (int i = 0; i < 4; i++)
                    {
                        lat[i] = Convert.ToDouble(data[x]);
                        lng[i] = Convert.ToDouble(data[y]);
                        x += 2; y += 2;
                        int wayPoint = i + 1;
                        AddCustomPin("pin.png", lat[i], lng[i], "Waypoint ke-" + wayPoint);
                    }
                    lat1 = lat[0]; lat2 = lat[1]; lat3 = lat[2]; lat4 = lat[3];
                    lng1 = lng[0]; lng2 = lng[1]; lng3 = lng[2]; lng4 = lng[3];
                    Location position = new Location(lat1, lng1);
                    myMap.Center = position;
                    //myMap.ZoomLevel = 17;
                    //MessageBox.Show(lat1.ToString() + "," + lng1.ToString()
                    //    + "\n" + lat2.ToString() + "," + lng2.ToString()
                    //    + "\n" + lat3.ToString() + "," + lng3.ToString()
                    //    + "\n" + lat4.ToString() + "," + lng4.ToString());
                }
                //Data biasa
                else if (data[0].ToString().Equals("@0"))
                {
                    //altitude = Convert.ToInt32(data[1].ToString());
                    //yaw = Convert.ToInt32(data[2].ToString());
                    //pitch = Convert.ToInt32(data[3].ToString());
                    //roll = Convert.ToInt32(data[4].ToString());
                    //Lat = Convert.ToDouble(data[5].ToString());
                    //Lng = Convert.ToDouble(data[6].ToString());
                    //MessageBox.Show(Lat.ToString() + "," + Lng.ToString());
                    //battery = Convert.ToDouble(data[8]);

                    //Show data dari DB ke label
                    //GCS_DB_MODEL model1 = db.GetDataModel("GCS_DB");
                    //txtAlt.Content = model1.Alt;
                    //txtYaw.Content = model1.Yaw;
                    //txtPitch.Content = model1.Pitch;
                    //txtRoll.Content = model1.Roll;
                    //txtLat.Content = model1.Lat;
                    //txtLng.Content = model1.Lng;

                    //db.InsertData(Convert.ToString(data[1]), Convert.ToString(data[2]), Convert.ToString(data[3]),
                    //    Convert.ToString(data[4]), Convert.ToString(data[5]), Convert.ToString(data[6]), time);
                    db.InsertData2(TimeStart, data[1], data[2], data[3], data[4], data[5], data[6], time);

                    txtAlt.Content = data[1];
                    altitude = Convert.ToDouble(data[1]);
                    txtYaw.Content = data[2];
                    YawBaru = Convert.ToDouble(data[2]);
                    Yaw3D(YawBaru-YawLama);
                    YawLama = YawBaru;
                    txtPitch.Content = data[3];
                    PitchBaru = Convert.ToDouble(data[3]);
                    Pitch3D(PitchBaru-PitchLama);
                    PitchLama = PitchBaru;
                    txtRoll.Content = data[4];
                    RollBaru = Convert.ToDouble(data[4]);
                    Roll3D(RollBaru-RollLama);
                    RollLama = RollBaru;
                    txtLat.Content = data[5];
                    txtLng.Content = data[6];
                    Console.WriteLine(data[1]);
                    Console.WriteLine(data[2]);
                    Console.WriteLine(data[3]);
                    Console.WriteLine(data[4]);
                    Console.WriteLine(data[5]);
                    Console.WriteLine(data[6]);
                    Lat = Convert.ToDouble(data[5]);
                    Lng = Convert.ToDouble(data[6]);
                    TrackDroneIcon(Lat, Lng);
                    position = new Location(Lat, Lng);
                    myMap.Center = position; //center position sesuai lokasi drone
                    //myMap.ZoomLevel = 17;

                    double cekToleransi1 = 0, cekToleransi2 = 0, cekToleransi3 = 0, cekToleransi4 = 0;

                    cekToleransi1 = distance(Lat, Lng, lat1, lng1);
                    cekToleransi2 = distance(Lat, Lng, lat2, lng2);
                    cekToleransi3 = distance(Lat, Lng, lat3, lng3);
                    cekToleransi4 = distance(Lat, Lng, lat4, lng4);
                    //MessageBox.Show(cekToleransi1.ToString() + "\n" + cekToleransi2.ToString() + "\n" + cekToleransi3.ToString() + "\n" + cekToleransi4.ToString());

                    //double rLat1 = lat1 + 0.000018; double rLng1 = lng1 + 0.000018;
                    //double rLat2 = lat2 + 0.000018; double rLng2 = lng2 + 0.000018;
                    //double rLat3 = lat3 + 0.000018; double rLng3 = lng3 + 0.000018;
                    //double rLat4 = lat4 + 0.000018; double rLng4 = lng4 + 0.000018;
                    //double disLat = Lat * Lat; double disLng = Lng * Lng;
                    //double dLat1 = rLat1 * rLat1; double dLng1 = rLng1 * rLng1;
                    //double dLat2 = rLat2 * rLat2; double dLng2 = rLng2 * rLng2;
                    //double dLat3 = rLat3 * rLat3; double dLng3 = rLng3 * rLng3;
                    //double dLat4 = rLat4 * rLat4; double dLng4 = rLng4 * rLng4;

                    if (cekToleransi1 <= 0.004)
                    {
                        AddCustomPin("pinHome.png", lat1, lng1, "");
                        //MessageBox.Show("LatLng1 SUKSES");
                    }
                    if (cekToleransi2 <= 0.004)
                    {
                        AddCustomPin("pinHome.png", lat2, lng2, "");
                        //MessageBox.Show("LatLng2 SUKSES");
                    }
                    if (cekToleransi3 <= 0.004)
                    {
                        AddCustomPin("pinHome.png", lat3, lng3, "");
                        //MessageBox.Show("LatLng3 SUKSES");
                    }
                    if (cekToleransi4 <= 0.004)
                    {
                        AddCustomPin("pinHome.png", lat4, lng4, "");
                        //MessageBox.Show("LatLng4 SUKSES");
                    }

                    #region HUD_Control
                    Slider_Yaw.Value = Convert.ToDouble(txtYaw.Content);
                    Slider_Pitch.Value = Convert.ToDouble(txtPitch.Content);
                    Slider_Roll.Value = Convert.ToDouble(txtRoll.Content)*-1;
                    #endregion

                    //#region battery
                    //label_batt.Content = Convert.ToString(battery) + "%";
                    //if (battery >= 75)
                    //{
                    //    batt_icon.Visibility = Visibility.Visible;
                    //    batt_icon_warning.Visibility = Visibility.Hidden;
                    //    batt_icon_low.Visibility = Visibility.Hidden;
                    //    batt_1.Visibility = Visibility.Visible; batt_2.Visibility = Visibility.Visible;
                    //    batt_3.Visibility = Visibility.Visible; batt_4.Visibility = Visibility.Visible;
                    //}
                    //if (battery < 75)
                    //{
                    //    batt_1.Visibility = Visibility.Hidden;
                    //}
                    //if (battery < 50)
                    //{
                    //    batt_icon.Visibility = Visibility.Hidden;
                    //    batt_icon_warning.Visibility = Visibility.Visible;
                    //    batt_icon_low.Visibility = Visibility.Hidden;
                    //    batt_2.Visibility = Visibility.Hidden;
                    //}
                    //if (battery < 25)
                    //{
                    //    batt_icon.Visibility = Visibility.Hidden;
                    //    batt_icon_warning.Visibility = Visibility.Hidden;
                    //    batt_icon_low.Visibility = Visibility.Visible;
                    //    batt_3.Visibility = Visibility.Hidden;
                    //}
                    //#endregion

                    //db.GetData();
                    //txtAlt.Content = Convert.ToString(altitude);
                    //txtYaw.Content = Convert.ToString(yaw);
                    //txtPitch.Content = Convert.ToString(pitch);
                    //txtRoll.Content = Convert.ToString(roll);
                    //txtLat.Content = Convert.ToString(Lat);
                    //txtLng.Content = Convert.ToString(Lng);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Data received error: " + ex.Message);
            }
        }
        #endregion

        #region FlightRecord&Log
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::              Cek apakah folder FlightRecord udh ada            :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public void CheckFolderFlightRecord()
        {
            string pathFlightRecord = Environment.CurrentDirectory + @"\FlightRecord\";
            if (!Directory.Exists(pathFlightRecord))
            {
                Directory.CreateDirectory(pathFlightRecord);
            }
        }
        private void btnOpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            string filename = Convert.ToString(ComboBoxFlightRecord.SelectedItem);
            if (filename.Equals(""))
            {
                MessageBox.Show("Pilih log file yang akan dilihat");
            }
            else
            {
                Excel.Application xlApp;
                xlApp = new Excel.Application();
                Excel.Workbook wb = xlApp.Workbooks.Open(Environment.CurrentDirectory + @"\FlightRecord\" + filename);
                xlApp.Visible = true;
                myMap.Children.Clear();
                //string dbTarget = filename.Substring(0, 37);
                //TrackRoute("GCS_DB_" + dbTarget);
                //int idAkhir = GetLastID("GCS_DB_" + dbTarget);
                //string LatAwal = db.GetLat("GCS_DB_" + dbTarget, 1);
                //string LngAwal = db.GetLng("GCS_DB_" + dbTarget, 1);
                //string LatAkhir = db.GetLat("GCS_DB_" + dbTarget, idAkhir);
                //string LngAkhir = db.GetLng("GCS_DB_" + dbTarget, idAkhir);
                //Pushpin pinAwal = new Pushpin();
                //Pushpin pinAkhir = new Pushpin();
                //Location locAwal = new Location(Convert.ToDouble(LatAwal), Convert.ToDouble(LngAwal));
                //pinAwal.Location = locAwal;
                //ToolTipService.SetToolTip(pinAwal, "START");
                //Location locAkhir = new Location(Convert.ToDouble(LatAkhir), Convert.ToDouble(LngAkhir));
                //pinAkhir.Location = locAkhir;
                //ToolTipService.SetToolTip(pinAkhir, "FINISH");
                //myMap.Children.Add(pinAwal);
                //myMap.Children.Add(pinAkhir);
                //myMap.ZoomLevel = zoom;
                //myMap.Center = locAwal;
            }
        }
        #endregion

        #region FlightDistance
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                   Hitung jarak metode 1                        :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private double distance(double lat1, double lon1, double lat2, double lon2)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            //if (unit == 'K')
            //{
            dist = dist * 1.609344;
            //}
            //else if (unit == 'N')
            //{
            //    dist = dist * 0.8684;
            //}
            return (dist);
        }
        private double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }
        private double rad2deg(double rad)
        {
            return (rad / Math.PI * 180.0);
        }

        private void btnClearWaypointPin_Click(object sender, RoutedEventArgs e)
        {
            Image image = new Image();
            removePin(image, "waypointPin");
            waypointIndex = 1;
        }

        private void myMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (waypointIndex == 5)
            {
                waypointIndex = 1;
                //myMap.Children.Clear();
                Image image = new Image();
                removePin(image, "waypointPin");
            }
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                   Hitung jarak metode 2                        :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public static double DistanceBetweenPlaces(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // KM

            double sLat1 = Math.Sin(Radians(lat1));
            double sLat2 = Math.Sin(Radians(lat2));
            double cLat1 = Math.Cos(Radians(lat1));
            double cLat2 = Math.Cos(Radians(lat2));
            double cLon = Math.Cos(Radians(lon1) - Radians(lon2));

            double cosD = sLat1 * sLat2 + cLat1 * cLat2 * cLon;

            double d = Math.Acos(cosD);

            double dist = R * d;

            return dist;
        }
        public static double Radians(double x)
        {
            const double PIx = Math.PI;
            return x * PIx / 180;
        }
        #endregion

        #region DroneTrackingDiMap
        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::   Polyline yang muncul, sesuai dengan koordinat yang diterima, utk ngetrack  ::
        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void TrackRoute(string namaTabel)
        {
            MapPolyline polyline = new MapPolyline();
            polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
            polyline.StrokeThickness = 5;
            polyline.Opacity = 1;
            locCollection = new LocationCollection();
            List<GCS_DB_MODEL> listDBModel = db.getAllData(namaTabel);
            foreach (GCS_DB_MODEL item in listDBModel)
            {
                double Lat, Lng;
                Lat = Convert.ToDouble(item.Lat);
                Lng = Convert.ToDouble(item.Lng);
                locCollection.Add(new Location(Lat, Lng));
            }
            polyline.Locations = locCollection;
            myMap.Children.Add(polyline);
        }
        //Polyline Baru (bukan dari DB)
        private void TrackRoute2()
        {
            MapPolyline polyline = new MapPolyline();
            polyline.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
            polyline.StrokeThickness = 5;
            polyline.Opacity = 1;
            locCollection = new LocationCollection();
            locCollection.Add(new Location(Lat, Lng));
            polyline.Locations = locCollection;
            myMap.Children.Add(polyline);
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                 Drone pin ngikutin route                       :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void TrackDronePin()
        {
            Pushpin pin = new Pushpin();
            List<UIElement> elementsToRemove = new List<UIElement>();
            List<UIElement> pushpinToRemove = new List<UIElement>();
            foreach (UIElement element in myMap.Children)
            {
                foreach (UIElement p in myMap.Children)
                {
                    if (p.GetType() == typeof(Pushpin))
                    {
                        pushpinToRemove.Add(p);
                    }
                }
                foreach (UIElement pins in pushpinToRemove)
                {
                    myMap.Children.Remove(pin);

                }
                elementsToRemove.Add(element);
            }
            foreach (UIElement es in pushpinToRemove)
            {
                myMap.Children.Remove(es);
            }
            //GCS_DB_MODEL model1 = db.GetDataModel("GCS_DB");

            //Location pos = new Location(Convert.ToDouble(model1.Lat), Convert.ToDouble(model1.Lng));
            //pin.Location = pos;
            //myMap.Children.Add(pin);
        }

        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                            Drone icon ngikutin route                         ::
        //::  Isinya juga ada untuk menghitung jarak yang ditempuh dan ditampilkan ke UI  ::
        //::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void TrackDroneIcon(double Lat, double Lng)
        {
            MapLayer imageLayer = new MapLayer();
            Image image = new Image();
            removePin(image, "icon");
            image.Height = 40;
            image.Width = 40;
            //Define the URI location of the image
            BitmapImage myBitmapImage = new BitmapImage();
            Uri uri;
            if (statusUAV == 1)
            {
                uri = new Uri("/Resources/Quad-Selected.png", UriKind.Relative);
            }
            else
            {
                uri = new Uri("/Resources/FW-Selected.png", UriKind.Relative);
            }
            myBitmapImage.BeginInit();
            myBitmapImage.UriSource = uri;
            myBitmapImage.DecodePixelHeight = 150;
            myBitmapImage.EndInit();
            image.Source = myBitmapImage;
            image.Opacity = 1;
            image.Stretch = System.Windows.Media.Stretch.Fill;

            //GCS_DB_MODEL model1 = db.GetDataModel("GCS_DB");
            ////The map location to place the image at
            //Location loc = new Location(Convert.ToDouble(model1.Lat), Convert.ToDouble(model1.Lng));
            Location loc = new Location(Lat, Lng);
            //int DataCount = GetLastID("GCS_DB");
            //if (DataCount > 1)
            //{
            //    double lat1 = Convert.ToDouble(db.GetLat("GCS_DB", DataCount - 1));
            //    double lat2 = Convert.ToDouble(db.GetLat("GCS_DB", DataCount));
            //    double lng1 = Convert.ToDouble(db.GetLng("GCS_DB", DataCount - 1));
            //    double lng2 = Convert.ToDouble(db.GetLng("GCS_DB", DataCount));
            //    double jarak = distance(lat1, lng1, lat2, lng2);
            //    jarak_cetak = jarak_cetak + jarak;
            //    label_jarak.Content = String.Format("{0:0.000}", jarak_cetak);
            //}
            ////Center the image around the location specified
            PositionOrigin position = PositionOrigin.Center;
            ////Add the image to the defined map layer
            MapLayer.SetPosition(image, loc);
            ////imageLayer.Children.Add(DroneIcon);
            imageLayer.AddChild(image, loc, position);
            imageLayer.Tag = "icon";
            ////Add the image layer to the map
            myMap.Children.Add(imageLayer);
        }
        #endregion

        #region RemovePin
        private void removePin(Image image, String tag)
        {
            //                   ***REMOVE ICON***
            List<UIElement> elementsToRemove = new List<UIElement>();
            List<UIElement> pushpinToRemove = new List<UIElement>();
            foreach (UIElement element in myMap.Children)
            {
                foreach (UIElement p in myMap.Children.OfType<MapLayer>())
                {
                    if ((((MapLayer)p).Tag) == tag)
                    {
                        pushpinToRemove.Add(p);
                    }
                    //if (p.GetType() == typeof(MapLayer))
                    //{
                    //    pushpinToRemove.Add(imageLayer);
                    //}
                }
                foreach (UIElement pins in pushpinToRemove)
                {
                    myMap.Children.Remove(image);

                }
                elementsToRemove.Add(element);
            }
            foreach (UIElement es in pushpinToRemove)
            {
                myMap.Children.Remove(es);
            }
            //                   ***REMOVE ICON***
        }
        #endregion

        #region Waypoint
        private void btnWaypoint_Click(object sender, RoutedEventArgs e)
        {
            if (portGCS.IsOpen)
            {
                try
                {
                    StatusWaypoint = 1;
                    //portGCS.Write("w");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Port belum terkoneksi");
            }
        }

        String dataWaypoint = "";
        private void getDataWaypoint(String datapoint)
        {
            dataWaypoint = datapoint;
        }

        private void btnStartWaypoint_Click(object sender, RoutedEventArgs e)
        {
            portGCS.Write(dataWaypoint);
            Console.WriteLine(dataWaypoint);
            waypointIndex = 1;
            //portGCS.Write("startWaypoint:");
            //SendDataKeController("GCS_DB");
            //TrackRoute("GCS_DB");
            //TrackDroneIcon();
        }
        #endregion

        #region LainLain
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                     Hitung jumlah data di DB                   :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private int GetLastID(string namaTabel)
        {
            int count = db.GetLastID(namaTabel);
            return count;
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::           Untuk refresh ComboBox dengan file Log yg ada        :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        public void PopulateComboBoxRecord()
        {
            DirectoryInfo dinfo = new DirectoryInfo(Environment.CurrentDirectory + @"\FlightRecord\");
            FileInfo[] Files = dinfo.GetFiles("*.xlsx").OrderByDescending(p => p.CreationTime).ToArray();
            ComboBoxFlightRecord.Items.Clear();
            foreach (FileInfo file in Files)
            {
                ComboBoxFlightRecord.Items.Add(file.Name);
            }
        }

        public void RefreshUI()
        {
            ////Code to Restart WPF Application

            ////Start New Application Before Closing Current
            //Process.Start(Application.ResourceAssembly.Location);

            ////Close the Current
            //Application.Current.Shutdown();

            string angka = "0.000000";
            string angka2 = "00";

            batt_icon.Visibility = Visibility.Visible;
            batt_icon_warning.Visibility = Visibility.Hidden;
            batt_icon_low.Visibility = Visibility.Hidden;

            label_batt.Content = "100%";
            battery = 100;

            batt_1.Visibility = Visibility.Visible;
            batt_2.Visibility = Visibility.Visible;
            batt_3.Visibility = Visibility.Visible;
            batt_4.Visibility = Visibility.Visible;

            myMap.Center = position;
            //myMap.ZoomLevel = 1;
            myMap.Children.Clear();

            txtAlt.Content = angka; altitude = Convert.ToDouble(angka);
            txtYaw.Content = angka; yaw = Convert.ToDouble(angka);
            txtPitch.Content = angka; pitch = Convert.ToDouble(angka);
            txtRoll.Content = angka; roll = Convert.ToDouble(angka);
            txtAccuracy.Content = angka;
            txtLat.Content = angka; Lat = Convert.ToDouble(angka);
            txtLng.Content = angka; Lng = Convert.ToDouble(angka);

            label_jarak.Content = angka2; jarak_cetak = Convert.ToDouble(angka2);
            label_hour.Content = angka2; hour = Convert.ToInt32(angka2);
            label_minute.Content = angka2; minute = Convert.ToInt32(angka2);
            label_second.Content = angka2; second = Convert.ToInt32(angka2);

            PopulateComboBoxRecord();

            //BoxDataReceived.Text = "Data received goes here...";
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::                  Jika button REFRESH di klik                   :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            //RefreshUI();
            PortBaudSetting();
        }

        private void btnMaxHUD_Click(object sender, RoutedEventArgs e)
        {
            this.Panel_HUD.Children.Remove(this.HUD_ATT);
            HUD_Big wind = new HUD_Big(this.HUD_ATT);
            wind.Show();
        }

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //::  Buka directory > Process.Start(Environment.CurrentDirectory + @"\FlightRecord\"); :::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::

        //Add polygon manual, koordinat sudah dikasih..
        private void addNewPolygon()
        {
            MapPolygon polygon = new MapPolygon();
            polygon.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Blue);
            polygon.Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            polygon.StrokeThickness = 5;
            polygon.Opacity = 0.7;
            polygon.Locations = new LocationCollection() {
                new Location(47.6424,-122.3219),
                new Location(47.8424,-122.1747),
                new Location(47.5814,-122.1747)};

            myMap.Children.Add(polygon);
        }

        private void btnSendCommand_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = 8;
                string[] waypoint = BoxCommand.Text.Split(',');
                string datapoint = "@20";
                for (int i=0; i<4; i++)
                {
                    datapoint = datapoint + "#" +waypoint[i];
                }
                datapoint = datapoint + "*";
                //string datapoint = string.Format("@20#{0:0.0000000}#{1:0.0000000}#{2:0.0000000}#{3:0.0000000}#{4:0.0000000}#{5:0.0000000}#{6:0.0000000}#{7:0.0000000}*",
                //        new object[] { waypoint[0], waypoint[1], waypoint[2], waypoint[3], waypoint[4], waypoint[5], waypoint[6], waypoint[7] });
                MessageBox.Show(datapoint);
                portGCS.Write(datapoint);
                //Console.WriteLine(datapoint);
                //portGCS.Write(kata);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region Control
        private void btnCalibrate_Click(object sender, RoutedEventArgs e)
        {
            if (portGCS.IsOpen)
            {
                try
                {
                    portGCS.Write("c");
                }catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Port belum terkoneksi");
            }
            
        }

        private void btnLanding_Click(object sender, RoutedEventArgs e)
        {
            if (portGCS.IsOpen)
            {
                try
                {
                    portGCS.Write("l");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Port belum terkoneksi");
            }
        }

        private void btnTakeOff_Click(object sender, RoutedEventArgs e)
        {
            if (portGCS.IsOpen)
            {
                try
                {
                    portGCS.Write("t");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Port belum terkoneksi");
            }
        }
        #endregion

        private void SendDataKeController(string namaTabel)
        {
            LocationCollection locCollection = new LocationCollection();
            List<GCS_DB_MODEL> listDBModel = db.getAllData(namaTabel);
            foreach (GCS_DB_MODEL item in listDBModel)
            {
                double Lat, Lng;
                Lat = Convert.ToDouble(item.Lat);
                string lat = string.Format("{0:0.000000}", Lat);
                Lng = Convert.ToDouble(item.Lng);
                string lng = string.Format("{0:0.000000}", Lng);
                portGCS.Write(lat + ":");
                portGCS.Write(lng + ":");
                //locCollection.Add(new Location(Lat, Lng));
            }
        }

        #region Background Worker untuk closing port GCS
        // Status proses di console
        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Console.WriteLine("Closing Port");
            ////BoxDataReceived.Text += portGCS.ReadLine() + "\n";
            //string[] data;
            ////BoxDataReceived.ScrollToEnd();
            //string time = string.Format("{0:HH:mm:ss}", DateTime.Now);
            //data = portGCS.ReadLine().Split('#');
            ////MessageBox.Show(Convert.ToString(portGCS.ReadLine()));
            ////MessageBox.Show(data[0].ToString());
            ////Waypoint
            ////Console.WriteLine(data_received);
            ////Console.WriteLine(data[1]);
            //if (data[0].ToString().Equals("@20"))
            //{

            //    int x = 1, y = 2;
                
            //    for (int i = 0; i < 4; i++)
            //    {
            //        lat[i] = Convert.ToDouble(data[x]);
            //        lng[i] = Convert.ToDouble(data[y]);
            //        x += 2; y += 2;
            //        int wayPoint = i + 1;
            //        AddCustomPin("pin.png", lat[i], lng[i], "Waypoint ke-" + wayPoint);
            //    }
            //    lat1 = lat[0]; lat2 = lat[1]; lat3 = lat[2]; lat4 = lat[3];
            //    lng1 = lng[0]; lng2 = lng[1]; lng3 = lng[2]; lng4 = lng[3];
            //    Location position = new Location(lat1, lng1);
            //    myMap.Center = position;
            //    myMap.ZoomLevel = 17;
            //}
            ////Data biasa
            //else if (data[0].ToString().Equals("@0"))
            //{
            //    //db.InsertData(Convert.ToString(data[1]), Convert.ToString(data[2]), Convert.ToString(data[3]),
            //    //    Convert.ToString(data[4]), Convert.ToString(data[5]), Convert.ToString(data[6]), time);
            //    db.InsertData2(TimeStart, data[1], data[2], data[3], data[4], data[5], data[6], time);

            //    txtAlt.Content = data[1];
            //    txtYaw.Content = data[2];
            //    Yaw3D(Convert.ToDouble(data[2]));
            //    txtPitch.Content = data[3];
            //    Pitch3D(Convert.ToDouble(data[3]));
            //    txtRoll.Content = data[4];
            //    Roll3D(Convert.ToDouble(data[4]));
            //    txtLat.Content = data[5];
            //    txtLng.Content = data[6];
            //    Console.WriteLine(data[1]);
            //    Console.WriteLine(data[2]);
            //    Console.WriteLine(data[3]);
            //    Console.WriteLine(data[4]);
            //    Console.WriteLine(data[5]);
            //    Console.WriteLine(data[6]);
            //    Lat = Convert.ToDouble(data[5]);
            //    Lng = Convert.ToDouble(data[6]);
            //    TrackDroneIcon(Lat, Lng);
            //    position = new Location(Lat, Lng);
            //    myMap.Center = position; //center position sesuai lokasi drone
            //    myMap.ZoomLevel = 17;

            //    double cekToleransi1 = 0, cekToleransi2 = 0, cekToleransi3 = 0, cekToleransi4 = 0;

            //    cekToleransi1 = distance(Lat, Lng, lat1, lng1);
            //    cekToleransi2 = distance(Lat, Lng, lat2, lng2);
            //    cekToleransi3 = distance(Lat, Lng, lat3, lng3);
            //    cekToleransi4 = distance(Lat, Lng, lat4, lng4);
                
            //    #region HUD_Control
            //    Slider_Yaw.Value = Convert.ToDouble(txtYaw.Content);
            //    Slider_Pitch.Value = Convert.ToDouble(txtPitch.Content);
            //    Slider_Roll.Value = Convert.ToDouble(txtRoll.Content);
            //    #endregion
            //}
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            //    BoxDataReceived.Text += portGCS.ReadLine() + "\n";
            //}));
        }

        // Background worker proses, closing port
        private void bgWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SerialPort sp = e.Argument as SerialPort;
            while (portGCS.IsOpen)
            {
                if (bgWorker.CancellationPending)
                {
                    // Pause for a bit to demonstrate that there is time between
                    // "Cancelling..." and "Cancel ed".
                    Thread.Sleep(100);

                    // Set the e.Cancel flag so that the WorkerCompleted event
                    // knows that the process was cancelled.
                    e.Cancel = true;
                    return;
                }
                portGCS.Close();
                //try
                //{
                //    sp.ReadLine();
                //    bgWorker.ReportProgress(1);
                //} catch (Exception bge)
                //{
                //    MessageBox.Show("Error bgWorker: " + bge.Message);
                //}
                //Thread.Sleep(100);
                ////TerimaData(portGCS.ReadLine());
            }
        }

        //Background worker completed, show MessageBox
        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MessageBox.Show("Port Closed!");
            //portGCS.Close();
            if (e.Cancelled)
            {

                Console.WriteLine("Operation Cancelled");

            }

            else if (e.Error != null)
            {

                Console.WriteLine("Error in BgWorker Process :" + e.Error);

            }

            else
            {

                Console.WriteLine("Operation Completed :" + e.Result);

            }

        }
        #endregion
    }
}
