﻿using System;
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
        Paragraph _source;
        AutoResetEvent _dbgContinue = new AutoResetEvent(false);

        TraceBackFrame _curFrame;
        FunctionCode _curCode;
        string _curResult;
        object _curPayload;

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

        private void StepIn_Click(object sender, RoutedEventArgs e)
        {
            _dbgContinue.Set();
            dbgResult.Text = "Running";

            foreach (var i in _source.Inlines)
            {
                i.Background = Brushes.Black;
                i.Foreground = Brushes.White;
            }
        }

        private void HighlightLine(int linenum, Brush foreground, Brush background)
        {
            var curline = _source.Inlines.ElementAtOrDefault(linenum - 1);
            if (curline != null)
            {
                curline.Background = foreground;
                curline.Foreground = background;
            }
        }

        private void TracebackCall()
        {
            dbgResult.Text = string.Format("Call {0}", _curCode.co_name);
            HighlightLine((int)_curFrame.f_lineno, Brushes.LightGreen, Brushes.Black);
        }

        private void TracebackReturn()
        {
            dbgResult.Text = string.Format("Return {0}", _curCode.co_name);
            HighlightLine(_curCode.co_firstlineno, Brushes.LightPink, Brushes.Black);
        }

        private void TracebackLine()
        {
            dbgResult.Text = string.Format("Line {0}", _curFrame.f_lineno);
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

        private TracebackDelegate OnTracebackReceived(TraceBackFrame frame, string result, object payload)
        {
            var a = new Action<TraceBackFrame, string, object>(this.OnTraceback);
            this.Dispatcher.Invoke(a, frame, result, payload);
            _dbgContinue.WaitOne();
            return null;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _source = new Paragraph();
            rtbSource.Document = new FlowDocument(_source);
            rtbSource.Document.PageWidth = 10000;
            
            _engine.SetTrace(this.OnTracebackReceived);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _engine.SetTrace(null);
        }

    }
}