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
using VMS.TPS.Common.Model.API;
using System.IO;
using Microsoft.Win32;

namespace Model_Validation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VMS.TPS.Common.Model.API.Application app = VMS.TPS.Common.Model.API.Application.CreateApplication();
        Patient pat = null;
        Course c = null;
        List<DataScan> ds_list = new List<DataScan>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void patLoad_btn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(patient_txt.Text))
            {
                MessageBox.Show("Input a patient ID before clicking load patient");
            }
            else
            {
                try
                {
                    app.ClosePatient();
                    pat = app.OpenPatientById(patient_txt.Text);
                }
                catch
                {
                    MessageBox.Show("could not find patient ID");
                }
                if (pat != null)
                {
                    //first clear the course combobox so items dont stack up.
                    Course_cmb.Items.Clear();
                    foreach (Course c in pat.Courses)
                    {
                        Course_cmb.Items.Add(c.Id);

                    }

                }
                else
                {
                    MessageBox.Show("Patient Not Found");
                }
            }
        }

        private void Course_cmb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            c = pat.Courses.First(x => x.Id == Course_cmb.SelectedItem.ToString());
            plan_cmb.Items.Clear();
            foreach (PlanSetup ps in c.PlanSetups)
            {
                plan_cmb.Items.Add(ps.Id);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            app.ClosePatient();
            app.Dispose();
        }



        public class DataScan
        {
            public double FieldX { get; set; }
            public double FieldY { get; set; }
            public double StartPos { get; set; }
            public double stepSize { get; set; }
            public int scanLength { get; set; }
            public string axisDir { get; set; }
            public double depth { get; set; }
            public List<Tuple<double, double>> scan_data { get; set; }
            public DataScan()
            {
                scan_data = new List<Tuple<double, double>>();
            }
        }

        private void getScan_btn_Click(object sender, RoutedEventArgs e)
        {

            ds_list.Clear();
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.DefaultExt = ".asc";
            ofd.Filter = "Ascii File (*.asc)|*.asc|Text Files (*.txt)|*.txt|W2CAD Files(*.cdp)|*.cdp";
            if (ofd.ShowDialog() == true)
            {
                //read out content here.
                foreach (string line in File.ReadAllLines(ofd.FileName))
                {
                    if (line.Contains("STOM") && line.ElementAt(0)!='#')
                    {
                        ds_list.Add(new DataScan());
                    }
                    if (line.Contains("FLSZ"))
                    {
                        ds_list.Last().FieldX = Convert.ToDouble(line.Split(' ').Last().Split('*').First());
                        ds_list.Last().FieldY = Convert.ToDouble(line.Split('*').Last());
                    }
                    if (line.Contains("TYPE"))
                    {
                        switch (line.Split(' ').Last())
                        {
                            case "OPP":
                                ds_list.Last().axisDir = "X";
                                break;
                            case "OPD":
                                ds_list.Last().axisDir = "Z";
                                break;
                            case "DPR":
                                ds_list.Last().axisDir = "X";//for now
                                break;
                        }
                    }
                    if (line.Contains("PNTS"))
                    {
                        ds_list.Last().scanLength = Convert.ToInt32(line.Split(' ').Last());
                    }
                    if (line.Contains("STEP"))
                    {
                        ds_list.Last().stepSize = Convert.ToDouble(line.Split(' ').Last());
                    }
                    if (line.Contains("DPTH"))
                    {
                        ds_list.Last().depth = Convert.ToDouble(line.Split(' ').Last());
                    }
                    if (line[0] == '<')
                    {
                        //this is where the beam data exists
                        double pos = ds_list.Last().axisDir == "X" ? //if the direction is x (profile)
                            Convert.ToDouble(line.Split(' ').First().Trim('<')) ://take the first value (x)
                            Convert.ToDouble(line.Split(' ')[2]);//else take the 3rd value (z)
                        double val = Convert.ToDouble(line.Split(' ').Last().Trim('>'));
                        ds_list.Last().scan_data.Add(new Tuple<double, double>(pos, val));
                    }
                    //Preview scan data in stackpanel
                    prevScans_sp.Children.Clear();
                    foreach (DataScan ds in ds_list)
                    {
                        Label lbl = new Label();
                        string scan_type = ds.axisDir == "X" ? "Profile" : "PDD";
                        string depth_type = ds.axisDir == "X" ? ds.depth.ToString() : "NA";
                        lbl.Content = String.Format("{0} - FLSZ ({1} x{2})-Depth({3})", scan_type, ds.FieldX, ds.FieldY, depth_type);
                        prevScans_sp.Children.Add(lbl);

                    }
                }
            }


        }
    }
}
