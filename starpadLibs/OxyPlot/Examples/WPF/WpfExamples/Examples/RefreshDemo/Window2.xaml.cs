﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Window2.xaml.cs" company="OxyPlot">
//   The MIT License (MIT)
//
//   Copyright (c) 2012 Oystein Bjorke
//
//   Permission is hereby granted, free of charge, to any person obtaining a
//   copy of this software and associated documentation files (the
//   "Software"), to deal in the Software without restriction, including
//   without limitation the rights to use, copy, modify, merge, publish,
//   distribute, sublicense, and/or sell copies of the Software, and to
//   permit persons to whom the Software is furnished to do so, subject to
//   the following conditions:
//
//   The above copyright notice and this permission notice shall be included
//   in all copies or substantial portions of the Software.
//
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//   OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//   MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//   IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//   CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//   TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//   SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   Interaction logic for Window2.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Windows;

namespace RefreshDemo
{
    using System.Windows.Media;

    using OxyPlot;
    using OxyPlot.Series;

    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : Window
    {
        public PlotModel PlotModel { get; set; }

        public Window2()
        {
            InitializeComponent();
            this.PlotModel = new PlotModel();
            this.PlotModel.Series.Add(new FunctionSeries());
            this.DataContext = this;
            CompositionTarget.Rendering += this.CompositionTarget_Rendering;
        }

        private double lastUpdateTime;

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            double seconds = ((RenderingEventArgs)e).RenderingTime.TotalSeconds;
            if (seconds > this.lastUpdateTime + 0.2)
            {
                this.UpdatePlot();
                this.lastUpdateTime = seconds;
            }
        }

        private double x;

        void UpdatePlot()
        {
            this.PlotModel.Title = "Plot updated: " + DateTime.Now;
            this.PlotModel.Series[0] = new FunctionSeries(Math.Sin, x, x + 4, 0.01);
            x += 0.1;

            // Refresh the plot control
            this.plot1.RefreshPlot(true);
        }
    }
}