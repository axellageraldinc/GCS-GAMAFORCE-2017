﻿using System;
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

namespace GCS_WPF_2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region DeklarasiVariabel
        PointCollection newPoint;

        private FilterInfoCollection VideoCaptureDevices;
        private VideoCaptureDevice FinalVideo;

        DBHelper db;

        private static Location position = new Location(-7.778301, 110.374690);
        List<MapPolyline> listPolyline;
        private static int zoom = 15, second=0, minute=0, hour=0, i=1;
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
        #endregion

        public MainWindow()
        {
            InitializeComponent();
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
            // Disables the default mouse double-click action.
            e.Handled = true;

            // Determin the location to place the pushpin at on the map.

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
            AddCustomPin("pin.png", Latitude, Longitude, "Point ke-"+i);
            i++;
            //BoxTestSerial.Text = (string.Format("{0:0.000000}", Latitude) + "," + string.Format("{0:0.000000}", Longitude));
            //Kirim latitude dan longitude ke controller
            try
            {
                //int x = 1;
                //byte[] b = BitConverter.GetBytes(x);
                //portGCS.Write(b, 0, 4);
                //portGCS.Write("SEMPAK:");
                //portGCS.Write("waypoint:");
                string lat = string.Format("{0:0.000000}", Latitude);
                portGCS.Write(lat + ":");
                string lng = string.Format("{0:0.000000}", Longitude);
                portGCS.Write(lng + ":");
                label_Test.Content = lat + "," + lng;
                string time = string.Format("{0:HH:mm:ss}", DateTime.Now);
                //db.InsertData("", "", "", "", Convert.ToString(lat), Convert.ToString(lng), time);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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
                TimeStart = string.Format("FlightLog__{0:dd_MMMM_yyyy__HH_mm_ss}", DateTime.Now);
                start = DateTime.Now;
                Timer();
                TimerFlightTime();
                //portGCS.Write("SEMPAK:");
            }
            else
            {
                btnConnect.Content = "CONNECT";
                //Timer dihentikan
                timer.Stop();
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
                    Thread CloseDown = new Thread(new ThreadStart(CloseSerialOnExit)); //close port in new thread to avoid hang
                    CloseDown.Start(); //close port in new thread to avoid hang
                }
                db.ExcelSave(TimeStart, TotalHours, TotalMinutes, TotalSeconds);
                RefreshUI();
                db.DeleteAllData("GCS_DB");
            }
        }
        private void CloseSerialOnExit()
        {
            try
            {
                portGCS.Close(); //close the serial port
                myMap.Children.Clear();
                myMap.ZoomLevel = 1;
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message); //catch any serial port closing error messages

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
                portGCS.Open();
                //Data yang diterima, dioperasikan di method portGCS_DataReceived
                portGCS.DataReceived += new SerialDataReceivedEventHandler(portGCS_DataReceived);
            }
            catch (Exception ex)
            {
                MessageBox.Show("0 : " + ex.Message);
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
                Dispatcher.Invoke((Action)(() => TerimaData(data_received)));
                Dispatcher.Invoke((Action)(() => BoxDataReceived.Text += data_received + "\n"));
                Dispatcher.Invoke((Action)(() => PortBaudSetting()));

            }
            catch (Exception ex)
            {
                MessageBox.Show("1 :" + ex.Message);
            }
        }
        #endregion

        #region PenerimaanDataDariDrone
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

        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        //:: Pengolahan data, data yang diterima diolah dan ditampilkan ke UI::
        //:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
        private void TerimaData(string data_received)
        {
            try
            {
                string[] data;
                BoxDataReceived.ScrollToEnd();
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
                data = data_received.Split('#');
                //MessageBox.Show(data[0].ToString());
                //Waypoint
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
                    //MessageBox.Show(lat1.ToString() + "," + lng1.ToString()
                    //    + "\n" + lat2.ToString() + "," + lng2.ToString()
                    //    + "\n" + lat3.ToString() + "," + lng3.ToString()
                    //    + "\n" + lat4.ToString() + "," + lng4.ToString());
                }
                //Data biasa
                else if (data[0].ToString().Equals("@0"))
                {
                    altitude = Convert.ToInt32(data[1]);
                    yaw = Convert.ToInt32(data[2]);
                    pitch = Convert.ToInt32(data[3]);
                    roll = Convert.ToInt32(data[4]);
                    Lat = Convert.ToDouble(data[5]);
                    Lng = Convert.ToDouble(data[6]);
                    //MessageBox.Show(Lat.ToString() + "," + Lng.ToString());
                    battery = Convert.ToDouble(data[7]);
                    //db.InsertData(Convert.ToString(altitude), Convert.ToString(yaw), Convert.ToString(pitch),
                    //    Convert.ToString(roll), Convert.ToString(Lat), Convert.ToString(Lng), time);
                    //Show data dari DB ke label
                    //GCS_DB_MODEL model1 = db.GetDataModel("GCS_DB");
                    //txtAlt.Content = model1.Alt;
                    //txtYaw.Content = model1.Yaw;
                    //txtPitch.Content = model1.Pitch;
                    //txtRoll.Content = model1.Roll;
                    //txtLat.Content = model1.Lat;
                    //txtLng.Content = model1.Lng;
                    txtAlt.Content = altitude.ToString();
                    txtYaw.Content = yaw.ToString();
                    txtPitch.Content = pitch.ToString();
                    txtRoll.Content = roll.ToString();
                    txtLat.Content = Lat.ToString();
                    txtLng.Content = Lng.ToString();

                    TrackDroneIcon(Lat, Lng);
                    position = new Location(Lat, Lng);
                    myMap.Center = position; //center position sesuai lokasi drone
                    myMap.ZoomLevel = 25;

                    double cekToleransi1 = 0, cekToleransi2 = 0, cekToleransi3 = 0, cekToleransi4 = 0;

                    cekToleransi1 = distance(Lat, Lng, lat1, lng1);
                    cekToleransi2 = distance(Lat, Lng, lat2, lng2);
                    cekToleransi3 = distance(Lat, Lng, lat3, lng3);
                    cekToleransi4 = distance(Lat, Lng, lat4, lng4);
                    MessageBox.Show(cekToleransi1.ToString() + "\n" + cekToleransi2.ToString() + "\n" + cekToleransi3.ToString() + "\n" + cekToleransi4.ToString());

                    //double rLat1 = lat1 + 0.000018; double rLng1 = lng1 + 0.000018;
                    //double rLat2 = lat2 + 0.000018; double rLng2 = lng2 + 0.000018;
                    //double rLat3 = lat3 + 0.000018; double rLng3 = lng3 + 0.000018;
                    //double rLat4 = lat4 + 0.000018; double rLng4 = lng4 + 0.000018;
                    //double disLat = Lat * Lat; double disLng = Lng * Lng;
                    //double dLat1 = rLat1 * rLat1; double dLng1 = rLng1 * rLng1;
                    //double dLat2 = rLat2 * rLat2; double dLng2 = rLng2 * rLng2;
                    //double dLat3 = rLat3 * rLat3; double dLng3 = rLng3 * rLng3;
                    //double dLat4 = rLat4 * rLat4; double dLng4 = rLng4 * rLng4;
                    
                    if (cekToleransi1<=0.004)
                    {
                        AddCustomPin("pinHome.png", lat1, lng1, "");
                        //MessageBox.Show("LatLng1 SUKSES");
                    }
                    if (cekToleransi2<=0.004)
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
                    Slider_Roll.Value = Convert.ToDouble(txtRoll.Content);
                    #endregion
                    
                    #region battery
                    label_batt.Content = Convert.ToString(battery) + "%";
                    if (battery >= 75)
                    {
                        batt_icon.Visibility = Visibility.Visible;
                        batt_icon_warning.Visibility = Visibility.Hidden;
                        batt_icon_low.Visibility = Visibility.Hidden;
                        batt_1.Visibility = Visibility.Visible; batt_2.Visibility = Visibility.Visible;
                        batt_3.Visibility = Visibility.Visible; batt_4.Visibility = Visibility.Visible;
                    }
                    if (battery < 75)
                    {
                        batt_1.Visibility = Visibility.Hidden;
                    }
                    if (battery < 50)
                    {
                        batt_icon.Visibility = Visibility.Hidden;
                        batt_icon_warning.Visibility = Visibility.Visible;
                        batt_icon_low.Visibility = Visibility.Hidden;
                        batt_2.Visibility = Visibility.Hidden;
                    }
                    if (battery < 25)
                    {
                        batt_icon.Visibility = Visibility.Hidden;
                        batt_icon_warning.Visibility = Visibility.Hidden;
                        batt_icon_low.Visibility = Visibility.Visible;
                        batt_3.Visibility = Visibility.Hidden;
                    }
                    #endregion

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
                MessageBox.Show("2 : " + ex.Message);
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

            //                   ***REMOVE ICON***
            List<UIElement> elementsToRemove = new List<UIElement>();
            List<UIElement> pushpinToRemove = new List<UIElement>();
            foreach (UIElement element in myMap.Children)
            {
                foreach (UIElement p in myMap.Children.OfType<MapLayer>())
                {
                    if ((((MapLayer)p).Tag) == "icon")
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

            image.Height = 30;
            image.Width = 30;
            //Define the URI location of the image
            BitmapImage myBitmapImage = new BitmapImage();
            Uri uri = new Uri("/Resources/drone.png", UriKind.Relative);
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

        #region Waypoint
        private void btnWaypoint_Click(object sender, RoutedEventArgs e)
        {
            if (portGCS.IsOpen == false)
            {
                MessageBox.Show("Silakan connect terlebih dahulu ke port controller", "Belum connect!");
            }
            else
            {
                portGCS.Write("waypoint:");
            }
        }

        private void btnStartWaypoint_Click(object sender, RoutedEventArgs e)
        {

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
            FileInfo[] Files = dinfo.GetFiles("*.xlsx");
            ComboBoxFlightRecord.Items.Clear();
            foreach (FileInfo file in Files)
            {
                ComboBoxFlightRecord.Items.Add(file.Name);
            }
        }

        public void RefreshUI()
        {
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

            BoxDataReceived.Text = "Data received goes here...";
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
                string penanda = "tanda>";
                portGCS.Write(penanda);
                string kata2 = BoxCommand.Text;
                portGCS.Write(kata2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        private void btnCalibrate_Click(object sender, RoutedEventArgs e)
        {
            if (portGCS.IsOpen)
            {
                try
                {
                    portGCS.Write("calibrate");
                }catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            
        }

        private void btnLanding_Click(object sender, RoutedEventArgs e)
        {
            if (portGCS.IsOpen)
            {
                try
                {
                    portGCS.Write("landing");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void btnTakeOff_Click(object sender, RoutedEventArgs e)
        {
            if (portGCS.IsOpen)
            {
                try
                {
                    portGCS.Write("takeoff");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

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

    }
}
