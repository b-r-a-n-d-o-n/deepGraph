using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;
using System.Reflection;

namespace graph
{
    public partial class Form1 : Form
    {
        const int MAXPOINTS = 10000;
//tst
        double startZoomLocationRatio;
        double endZoomLocationRatio;

        int startRectX;
        int startRectY;

        int currentStartPlot = 0;
        int currentEndPlot = 0;


        public delegate void UpdateProgressBar(ProgressBar pb, int val, bool vis);
        UpdateProgressBar updateProgressBar = (ProgressBar pb, int val, bool vis) => { pb.Visible = vis; if (vis) pb.Value = val; pb.Update(); };


        System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

        Pen pen = new Pen(Color.Black, (float)2);
        Pen pen2 = new Pen(Color.White, (float)2);

        System.Drawing.Graphics formGraphics;

        Rectangle r;
        Rectangle r2;

        bool drag = false;

        int factor = 0;

        List<double>[] series;


        public Form1()
        {
            InitializeComponent();
            formGraphics = this.chart1.CreateGraphics();
        }

        private int findMinIndex(int singalIndex, int lowerBound, int upperBound)
        {
            int holder = lowerBound;

            if (upperBound >= series[0].Count)
            {
                upperBound = series[0].Count - 1;
            }


            for (int x = lowerBound; x < upperBound; x++)
            {
                if (series[singalIndex][x] < series[singalIndex][holder])
                    holder = x;
            }
            return holder;
        }

        private int findMaxIndex(int singalIndex, int lowerBound, int upperBound)
        {
            int holder = lowerBound;

            if (upperBound >= series[0].Count)
            {
                upperBound = series[0].Count - 1;
            }


            for (int x = lowerBound; x < upperBound; x++)
            {
                if (series[singalIndex][x] > series[singalIndex][holder])
                    holder = x;
            }
            return holder;
        }

        private double findAverage(int singalIndex, int lowerBound, int upperBound)
        {
            double divisor = upperBound - lowerBound;
            double accum = 0;

            if (upperBound >= series[0].Count)
            {
                upperBound = series[0].Count - 1;
            }

            for (int x = lowerBound; x < upperBound; x++)
            {
                accum += series[singalIndex][x];
            }
            return accum / divisor;
        }

        private void doPlot(int from, int to, int signals)
        {
            currentStartPlot = from;
            currentEndPlot = to;

            if (to - from > MAXPOINTS)
            {
                factor = (to - from) / (MAXPOINTS / 2); //factor is the number of points that must be averaged (actually min/maxed) to get MAXPOINTS in the plot
                // divide MAXPOINTS by 2 because will have a min point and a max point for each group  
                for (int k = 0; k < signals; k++)
                {
                    for (int j = from; (j + factor) < to; j += factor)
                    {
                        int localMin = findMinIndex(k, j, j + factor);
                        int localMax = findMaxIndex(k, j, j + factor);

                        if (localMin <= localMax)  //need to know which came first before plotting 
                        {
                            chart1.Series[k].Points.AddXY(localMin, series[k][localMin]);
                            chart1.Series[k].Points.AddXY(localMax, series[k][localMax]);

                        }
                        else
                        {
                            chart1.Series[k].Points.AddXY(localMax, series[k][localMax]);
                            chart1.Series[k].Points.AddXY(localMin, series[k][localMin]);
                        }
                    }
                }
            }

            else
            {
                for (int k = 0; k < signals; k++)
                {
                    for (int j = from; j < to; j++)
                    {
                        chart1.Series[k].Points.AddY(series[k][j]);
                    }
                }
            }
        }

        private void tryZoom()
        {
            if (chart1.Series.Count > 0 && chart1.Series[0].Points.Count > 0)
            {
                int start = (int)Math.Round((startZoomLocationRatio * (currentEndPlot - currentStartPlot)) + currentStartPlot);
                int end = (int)Math.Round((endZoomLocationRatio * (currentEndPlot - currentStartPlot)) + currentStartPlot);

                for (int x = 0; x < chart1.Series.Count; x++)
                {
                    chart1.Series[x].Points.Clear();
                }
                doPlot(start, end, chart1.Series.Count);
            }
        }

        private int getPos(StreamReader s)
        {
            Int32 charpos = (Int32)s.GetType().InvokeMember("charPos", BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.GetField
                            , null, s, null);

            Int32 charlen = (Int32)s.GetType().InvokeMember("charLen", BindingFlags.DeclaredOnly |
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.GetField
                            , null, s, null);

            return (Int32)s.BaseStream.Position - charlen + charpos;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            chart1.Enabled = false;  //do this to avoid the accidental zoom click while selecting the file..enable it at the end of this func

            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fname = ofd.SafeFileName;
                string line;
                double d;

                char[] trimchars = { '\t', ' ', '\n', '\r' };

                StreamReader read;

                read = new StreamReader(ofd.OpenFile());
                line = read.ReadLine();
                line = line.TrimEnd(trimchars);

                progressBar1.Minimum = 0;
                progressBar1.Maximum = (int)read.BaseStream.Length;

                this.Invoke(updateProgressBar, progressBar1, 0, true);

                string[] splitLine = line.Split('\t');
                double temp;

                if (splitLine.Length > 0)
                {
                    if (double.TryParse(splitLine[0], out temp) == true)
                    {
                        for(int x = 0; x < splitLine.Length; x++)
                        {
                            chart1.Series.Add("Col"+x.ToString());
                            chart1.Series["Col" + x.ToString()].ChartType = SeriesChartType.Line;
                        }

                    }
                    else
                    {
                        foreach (string st in splitLine)
                        {
                            chart1.Series.Add(st);
                            chart1.Series[st].ChartType = SeriesChartType.Line;
                        }

                    }
                }

                series = new List<double>[splitLine.Length];

                for (int i = 0; i < series.Length; i++)
                {
                    series[i] = new List<double>();
                }

                int updateRate = 0;

                while (!read.EndOfStream)
                {
                    line = read.ReadLine();
                    line = line.TrimEnd(trimchars);
                    if (line.Length != 0)
                    {
                        splitLine = line.Split('\t');
                        for (int i = 0; i < splitLine.Length; i++)
                        {
                            d = double.Parse(splitLine[i]);
                            series[i].Add(Math.Round(d));
                        }
                    }
                    updateRate++;
                    if (updateRate > 100)
                    {
                        this.Invoke(updateProgressBar, progressBar1, getPos(read), true);
                        updateRate = 0;
                    }
                }

                read.Close();

                this.Invoke(updateProgressBar, progressBar1, 0, false);

                chart1.Enabled = true;

                doPlot(0, series[0].Count, series.Length);  //plot the entire thing
            }
        }

        private void chart1_MouseDown(object sender, MouseEventArgs e)
        {
            double offset = (double)((chart1.ChartAreas[0].AxisX.GetPosition(currentStartPlot)) / 100);
            startZoomLocationRatio = (double)(e.Location.X) / (double)(chart1.Width);

            if (startZoomLocationRatio >= .5)
            {
                startZoomLocationRatio = startZoomLocationRatio + offset;
            }
            else
            {
                startZoomLocationRatio = startZoomLocationRatio - offset;
            }


            if (startZoomLocationRatio < 0)
                startZoomLocationRatio = 0;

            startRectX = e.Location.X;
            startRectY = e.Location.Y;

            r.X = startRectX;
            r.Y = startRectY;
            r.Width = 0;
            r.Height = 0;

            formGraphics.DrawRectangle(pen, r);

            drag = true;

        }

        private void chart1_MouseUp(object sender, MouseEventArgs e)
        {

            double offset = (double)((chart1.ChartAreas[0].AxisX.GetPosition(currentStartPlot)) / 100);
            endZoomLocationRatio = (double)(e.Location.X) / (double)(chart1.Width);
            if (endZoomLocationRatio >= .5)
            {
                endZoomLocationRatio = endZoomLocationRatio + offset;
            }
            else
            {
                endZoomLocationRatio = endZoomLocationRatio - offset;
            }

            drag = false;

            r2 = r;
            formGraphics.DrawRectangle(pen2, r2);

            r.X = 0;
            r.Y = 0;
            r.Width = 0;
            r.Height = 0;

            startRectX = 0;
            startRectY = 0;

            if (endZoomLocationRatio >= 1)
                endZoomLocationRatio = .999;


            if (endZoomLocationRatio - startZoomLocationRatio > 0)
                tryZoom();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (series != null && series[0].Count > 0)
            {
                for (int x = 0; x < chart1.Series.Count; x++)
                {
                    chart1.Series[x].Points.Clear();
                }
                doPlot(0, series[0].Count, series.Length);
            }
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            if (drag)
            {
                r2 = r;
                formGraphics.DrawRectangle(pen2, r2);

                r.X = startRectX;
                r.Y = startRectY;
                r.Width = e.Location.X - startRectX;
                r.Height = e.Location.Y - startRectY;

                formGraphics.DrawRectangle(pen, r);
            }
        }

    }
}
