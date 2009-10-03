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
using Microsoft.Scripting.Hosting;
using IronPython.Hosting;
using IronPython.Runtime.Exceptions;
using System.Threading;

namespace LightweightDebuggerDemo
{
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        static Thread _debugThread;
        static DebugWindow _debugWindow;

        public static void InitDebugWindow(ScriptEngine engine)
        {
            _debugThread = new Thread(() =>
            {
                _debugWindow = new DebugWindow(engine);
                _debugWindow.Show();
                System.Windows.Threading.Dispatcher.Run();
            });
            _debugThread.SetApartmentState(ApartmentState.STA);
            _debugThread.Start();
        }

        public static void Shutdown()
        {
            _debugWindow.Dispatcher.InvokeShutdown();
        }


        ScriptEngine _engine;

        private DebugWindow(ScriptEngine engine)
        {
            InitializeComponent();
            _engine = engine;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //Since the DebugWindow is on a seperate thread, we have
            //to shut down the dispatcher manually when we close the window
            this.Dispatcher.InvokeShutdown();
        }
    }
}
