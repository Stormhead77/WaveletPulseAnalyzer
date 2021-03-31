using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WaveletPulseAnalyzer
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        #region -- Процедуры --
        private int[] ReadSignal()
        {
            string script = @"C:\Users\Dima\Desktop\maga\read.py ";
            string signal = @"C:\Users\Dima\Desktop\maga\d0001";

            var startInfo = new ProcessStartInfo()
            {
                FileName = "cmd.exe",
                Arguments = "/c python " + script + signal,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            int[] sphygmogram = null;

            using (var proc = new Process())
            {
                proc.StartInfo = startInfo;
                proc.Start();

                using (StreamReader reader = proc.StandardOutput)
                {
                    while (!reader.EndOfStream)
                    {
                        string text = reader.ReadLine();
                        sphygmogram = text.Trim(new[] { '(', ')' }).Split(',').Select(int.Parse).ToArray();
                    }
                }
            }

            return sphygmogram;
        }
        #endregion

        #region -- События --
        private void Main_Load(object sender, EventArgs e)
        {
            Series signal = new Series()
            {
                Name = "Sphygmogram",
                ChartType = SeriesChartType.Line
            };

            var sphygmogram = ReadSignal();

            var xAx = Enumerable.Range(0, sphygmogram.Length).ToList();

            signal.Points.DataBindXY(xAx, sphygmogram);

            chart1.Series.Clear();
            chart1.Series.Add(signal);

            chart1.ChartAreas["caLineChart"].AxisY.ScaleView.Zoom(sphygmogram.Min(), sphygmogram.Max());

            chart1.MouseWheel += new MouseEventHandler(chart1_MouseWheel);
        }
        #endregion

        private class ZoomFrame
        {
            public double XStart { get; set; }
            public double XFinish { get; set; }
            public double YStart { get; set; }
            public double YFinish { get; set; }
        }

        private readonly Stack<ZoomFrame> _zoomFrames = new Stack<ZoomFrame>();
        private void chart1_MouseWheel(object sender, MouseEventArgs e)
        {
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                var chart = (Chart)sender;
                var xAxis = chart.ChartAreas[0].AxisX;
                var yAxis = chart.ChartAreas[0].AxisY;

                try
                {
                    if (e.Delta < 0)
                    {
                        if (0 < _zoomFrames.Count)
                        {
                            var frame = _zoomFrames.Pop();
                            if (_zoomFrames.Count == 0)
                            {
                                xAxis.ScaleView.ZoomReset();
                                yAxis.ScaleView.ZoomReset();
                            }
                            else
                            {
                                xAxis.ScaleView.Zoom(frame.XStart, frame.XFinish);
                                yAxis.ScaleView.Zoom(frame.YStart, frame.YFinish);
                            }
                        }
                    }
                    else if (e.Delta > 0)
                    {
                        var xMin = xAxis.ScaleView.ViewMinimum;
                        var xMax = xAxis.ScaleView.ViewMaximum;
                        var yMin = yAxis.ScaleView.ViewMinimum;
                        var yMax = yAxis.ScaleView.ViewMaximum;

                        _zoomFrames.Push(new ZoomFrame { XStart = xMin, XFinish = xMax, YStart = yMin, YFinish = yMax });

                        var posXStart = xAxis.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
                        var posXFinish = xAxis.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;
                        var posYStart = yAxis.PixelPositionToValue(e.Location.Y) - (yMax - yMin) / 4;
                        var posYFinish = yAxis.PixelPositionToValue(e.Location.Y) + (yMax - yMin) / 4;

                        xAxis.ScaleView.Zoom(posXStart, posXFinish);
                        yAxis.ScaleView.Zoom(posYStart, posYFinish);
                    }
                }
                catch { }
            }
        }
    }
}