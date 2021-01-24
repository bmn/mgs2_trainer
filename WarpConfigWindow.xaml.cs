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
using System.Windows.Shapes;

namespace MGS2Trainer
{
    /// <summary>
    /// Interaction logic for WarpConfigWindow.xaml
    /// </summary>
    public partial class WarpConfigWindow : Window
    {
        private Trainer Train;
        private WarpConfig WarpData;

        public Warp SelectedWarp;


        public WarpConfigWindow(Trainer trainer)
        {
            Train = trainer;
            WarpData = Train.WarpData;

            InitializeComponent();
        }
    }
}
