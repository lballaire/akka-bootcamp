using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor, IWithUnboundedStash
    {
        private const int MaxPoints = 250;

        private readonly Chart _chart;
        private readonly Button _pauseButton;
        private Dictionary<string, Series> _seriesIndex;
        private int _xPosCounter;

        public ChartingActor(Chart chart, Button pauseButton) : this(chart, pauseButton, new Dictionary<string, Series>())
        {
        }

        public ChartingActor(Chart chart, Button pauseButton, Dictionary<string, Series> seriesIndex)
        {
            this._chart = chart;
            this._pauseButton = pauseButton;
            this._seriesIndex = seriesIndex;

            this.Charting();
        }

        public IStash Stash { get; set; }

        private void Charting()
        {
            this.Receive<InitializeChart>(this.HandleInitialize);
            this.Receive<AddSeries>(this.HandleAddSeries);
            this.Receive<RemoveSeries>(this.HandleRemoveSeries);
            this.Receive<Metric>(this.HandleMetric);
            this.Receive<TogglePause>(this.HandleTogglePause);
        }

        private void Paused()
        {
            this.Receive<AddSeries>(_ => this.Stash.Stash());
            this.Receive<RemoveSeries>(_ => this.Stash.Stash());
            this.Receive<Metric>(this.HandleMetricPaused);
            this.Receive<TogglePause>(this.HandleToggleResume);
        }

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                this._seriesIndex = ic.InitialSeries;
            }

            this._chart.Series.Clear();

            var area = this._chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;

            this.SetChartBoundaries();

            if (_seriesIndex.Any())
            {
                foreach (var series in _seriesIndex)
                {
                    //force both the chart and the internal index to use the same names
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }

            this.SetChartBoundaries();
        }

        private void HandleAddSeries(AddSeries addSeries)
        {
            var series = addSeries.Series;
            if (!string.IsNullOrEmpty(series.Name) && !this._seriesIndex.ContainsKey(series.Name))
            {
                this._seriesIndex.Add(series.Name, series);
                this._chart.Series.Add(series);
                this.SetChartBoundaries();
            }
        }

        private void HandleRemoveSeries(RemoveSeries removeSeries)
        {
            var seriesName = removeSeries.SeriesName;
            if (!string.IsNullOrEmpty(seriesName) && this._seriesIndex.ContainsKey(seriesName))
            {
                var series = this._seriesIndex[seriesName];
                this._seriesIndex.Remove(seriesName);
                this._chart.Series.Remove(series);
                this.SetChartBoundaries();
            }
        }

        private void HandleMetric(Metric metric)
        {
            this.AddSeriesPoint(metric.Series, metric.CounterValue);
        }

        private void HandleMetricPaused(Metric metric)
        {
            this.AddSeriesPoint(metric.Series, 0.0d);
        }

        private void AddSeriesPoint(string seriesName, double value)
        {
            if (!string.IsNullOrEmpty(seriesName) && this._seriesIndex.ContainsKey(seriesName))
            {
                var series = this._seriesIndex[seriesName];
                series.Points.AddXY(this._xPosCounter++, value);

                while (series.Points.Count > MaxPoints)
                {
                    series.Points.RemoveAt(0);
                }

                this.SetChartBoundaries();
            }
        }

        private void HandleTogglePause(TogglePause toggle)
        {
            this.SetPauseButtonText(true);
            this.BecomeStacked(this.Paused);
        }

        private void HandleToggleResume(TogglePause obj)
        {
            this.SetPauseButtonText(false);
            this.UnbecomeStacked();

            this.Stash.UnstashAll();
        }

        private void SetPauseButtonText(bool isPaused)
        {
            this._pauseButton.Text = isPaused ? "RESUME |>" : "PAUSE ||";
        }

        private void SetChartBoundaries()
        {
            var allPoints = this._seriesIndex.Values.SelectMany(x => x.Points).ToList();
            var yValues = allPoints.SelectMany(x => x.YValues).ToList();

            var minAxisX = this._xPosCounter - MaxPoints;
            var maxAxisX = this._xPosCounter;
            var minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;
            var maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;

            if (allPoints.Count > 2)
            {
                var area = this._chart.ChartAreas[0];
                area.AxisX.Minimum = minAxisX;
                area.AxisX.Maximum = maxAxisX;
                area.AxisY.Minimum = minAxisY;
                area.AxisY.Maximum = maxAxisY;
            }
        }

        public class AddSeries
        {
            public Series Series { get; }

            public AddSeries(Series series)
            {
                Series = series;
            }
        }

        public class RemoveSeries
        {
            public string SeriesName { get; }

            public RemoveSeries(string seriesName)
            {
                this.SeriesName = seriesName;
            }
        }

        public class InitializeChart
        {
            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }

            public Dictionary<string, Series> InitialSeries { get; private set; }
        }

        public class TogglePause { }
    }
}
