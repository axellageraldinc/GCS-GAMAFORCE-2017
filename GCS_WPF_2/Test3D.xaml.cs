using HelixToolkit.Wpf;
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
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace GCS_WPF_2
{
    /// <summary>
    /// Interaction logic for Test3D.xaml
    /// </summary>
    public partial class Test3D : Window
    {
        //Path to the model file
        private const string MODEL_PATH = "C:\\Users\\Axellageraldinc A\\Documents\\GCS_Gamaforce_2017\\GCS_WPF_2\\dronev3.obj";
        ModelVisual3D device3D = new ModelVisual3D();
        public Test3D()
        {
            InitializeComponent();
            
            device3D.Content = Display3d(MODEL_PATH);
            // Add to view port
            viewPort3d.Children.Add(device3D);
        }

        /// <summary>
        /// Display 3D Model
        /// </summary>
        /// <param name="model">Path to the Model file</param>
        /// <returns>3D Model Content</returns>
        private Model3D Display3d(string model)
        {
            Model3D device = null;
            try
            {
                //Adding a gesture here
                //viewPort3d.RotateGesture = new MouseGesture(MouseAction.LeftClick);

                //Import 3D model file
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

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var axis = new Vector3D(1, 0, 0);
            var angle = 10;

            var matrix = device3D.Transform.Value;
            matrix.Rotate(new Quaternion(axis, angle));

            device3D.Transform = new MatrixTransform3D(matrix);
        }
    }
}
