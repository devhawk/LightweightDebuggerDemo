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
using IronPython.Runtime;

namespace LightweightDebuggerDemo
{
    public static class DebugCommands
    {
        public static readonly RoutedUICommand StepIn   = new RoutedUICommand("Step In", "StepIn", typeof(DebugWindow));
        public static readonly RoutedUICommand StepOut  = new RoutedUICommand("Step Out", "StepOut", typeof(DebugWindow));
        public static readonly RoutedUICommand StepOver = new RoutedUICommand("Step Over", "StepOver", typeof(DebugWindow));
    }
    /// <summary>
    /// Interaction logic for DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        static Thread _debugThread;
        static DebugWindow _debugWindow;
        static ManualResetEvent _debugWindowReady = new ManualResetEvent(false);

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

            _debugWindowReady.WaitOne();
            engine.SetTrace(_debugWindow.OnTracebackReceived);
        }

        public static void Shutdown()
        {
            _debugWindow._engine.SetTrace(null);
            _debugWindow.Dispatcher.InvokeShutdown();
        }

        ScriptEngine _engine;
        Paragraph _source;
        AutoResetEvent _dbgContinue = new AutoResetEvent(false);
        Action<TraceBackFrame, string, object> _tracebackAction;
        TraceBackFrame _curFrame;
        FunctionCode _curCode;
        string _curResult;
        object _curPayload;


        private DebugWindow(ScriptEngine engine)
        {
            InitializeComponent();

            _tracebackAction = new Action<TraceBackFrame, string, object>(this.OnTraceback);
            _engine = engine;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //Since the DebugWindow is on a seperate thread, we have
            //to shut down the dispatcher manually when we close the window
            this.Dispatcher.InvokeShutdown();
        }

        private void HighlightLine(int linenum, Brush foreground, Brush background)
        {
            var curline = _source.Inlines.ElementAtOrDefault(linenum - 1);
            if (curline != null)
            {
                var visible_start = rtbSource.GetPositionFromPoint(new Point(0,0), true);
                var visible_end = rtbSource.GetPositionFromPoint(new Point(0, rtbSource.ActualHeight), true);
                if (visible_start.CompareTo(curline.ContentStart) > 0
                    || visible_end.CompareTo(curline.ContentStart) < 0)
                {
                    var top = curline.ContentStart.GetCharacterRect(LogicalDirection.Forward).Top - 3;
                    rtbSource.ScrollToVerticalOffset(top);
                }

                curline.Background = foreground;
                curline.Foreground = background;
            }
        }

        private void TracebackCall()
        {
            dbgStatus.Text = string.Format("Call {0}", _curCode.co_name);
            HighlightLine((int)_curFrame.f_lineno, Brushes.LightGreen, Brushes.Black);
        }

        private void TracebackReturn()
        {
            dbgStatus.Text = string.Format("Return {0}", _curCode.co_name);
            HighlightLine(_curCode.co_firstlineno, Brushes.LightPink, Brushes.Black);
        }

        private void TracebackLine()
        {
            dbgStatus.Text = string.Format("Line {0}", _curFrame.f_lineno);
            HighlightLine((int)_curFrame.f_lineno, Brushes.Yellow, Brushes.Black);
        }

        private void OnTraceback(TraceBackFrame frame, string result, object payload)
        {
            var code = (FunctionCode)frame.f_code;
            if (_curCode == null || _curCode.co_filename != code.co_filename)
            {
                _source.Inlines.Clear();
                foreach (var line in System.IO.File.ReadAllLines(code.co_filename))
                {
                    _source.Inlines.Add(new Run(line + "\r\n"));
                }
            }

            _curFrame = frame;
            _curCode = code;
            _curResult = result;
            _curPayload = payload;


            switch (result)
            {
                case "call":
                    TracebackCall();
                    break;

                case "line":
                    TracebackLine();
                    break;

                case "return":
                    TracebackReturn();
                    break;

                default:
                    MessageBox.Show(string.Format("{0} not supported!", result));
                    break;
            }
        }

        bool breaktrace = true;
        private TracebackDelegate OnTracebackReceived(TraceBackFrame frame, string result, object payload)
        {
            if (breaktrace)
            {
                this.Dispatcher.BeginInvoke(_tracebackAction, frame, result, payload);
                _dbgContinue.WaitOne();
                return _traceback;
            }
            else
                return null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _source = new Paragraph();
            rtbSource.Document = new FlowDocument(_source);
            rtbSource.Document.PageWidth = 10000;
            
            _debugWindowReady.Set();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            breaktrace = false;
            _dbgContinue.Set();
        }

        TracebackDelegate _traceback;

        private void StepInExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _traceback = this.OnTracebackReceived;
            ExecuteStep();
        }

        private void ExecuteStep()
        {
            dbgStatus.Text = "Running";

            foreach (var i in _source.Inlines)
            {
                i.Background = Brushes.Black;
                i.Foreground = Brushes.White;
            }

            _dbgContinue.Set();
        }

        private void StepOutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _traceback = null;
            ExecuteStep();
        }

    }
}
