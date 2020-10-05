using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MGS2Trainer
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {

        private string LatestVer { get; set; }
        private static string BaseUrl = "https://www.w00ty.com/mgsr/mgs2/trainer/";

        public AboutWindow()
        {
            string ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            InitializeComponent();
            textBlock.Text = $@"
Metal Gear Solid 2 Trainer
version {ver.Substring(0, ver.LastIndexOf("."))} ({BuildInfo.BuildDateUtc.Date:dd MMM yyyy})

Created for Metal Gear Speedrunners
by bmn";

            CheckVersion();
        }

        public bool? ShowDialog(Window owner)
        {
            this.Owner = owner;
            return this.ShowDialog();
        }

        private async void CheckVersion()
        {
            var client = new HttpClient();
            var content = await client.GetStringAsync(BaseUrl + "index.php?ver=1");
            btnUpdate.Content += $" ({content})";
        }

        private void btnUpdate_Click_1(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(BaseUrl + "index.php?dl=1");
        }
    }
}
