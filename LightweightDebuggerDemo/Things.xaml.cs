using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using IronPython.Hosting;

namespace LightweightDebuggerDemo
{
    /// <summary>
    /// Interaction logic for Things.xaml
    /// </summary>
    public partial class Things : Window
    {
        public Things()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var engine = Python.CreateEngine();

            var s = engine.CreateScope();
            s.SetVariable("items", lbThings.Items);
            engine.ExecuteFile("getthings.py", s);
        }
    }
}
