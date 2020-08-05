using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterCoordinatorActor : ReceiveActor
    {
        private readonly IActorRef _chartingActor;
        private readonly Dictionary<CounterType, IActorRef> _counterActors;

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor) : this(chartingActor,
            new Dictionary<CounterType, IActorRef>())
        {
        }

        public PerformanceCounterCoordinatorActor(IActorRef chartingActor,
            Dictionary<CounterType, IActorRef> counterActors)
        {
            this._chartingActor = chartingActor;
            this._counterActors = counterActors;

            this.Receive<Watch>(this.HandleWatch);
            this.Receive<Unwatch>(this.HandleUnwatch);
        }

        private void HandleWatch(Watch watch)
        {
            var counterType = watch.CounterType;
            if (!this._counterActors.ContainsKey(counterType))
            {
                var counterActor = Context.ActorOf(Props.Create(() =>
                    new PerformanceCounterActor(counterType.ToString(), CounterGenerators[counterType])));
                this._counterActors[counterType] = counterActor;
            }

            this._chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[counterType]()));
            this._counterActors[counterType].Tell(new SubscribeCounter(counterType, this._chartingActor));
        }

        private void HandleUnwatch(Unwatch unwatch)
        {
            if (!this._counterActors.ContainsKey(unwatch.CounterType))
            {
                return;
            }

            this._counterActors[unwatch.CounterType].Tell(new UnsubscribeCounter(unwatch.CounterType, this._chartingActor));
            this._chartingActor.Tell(new ChartingActor.RemoveSeries(unwatch.CounterType.ToString()));
        }

        private static readonly Dictionary<CounterType, Func<PerformanceCounter>> CounterGenerators =
            new Dictionary<CounterType, Func<PerformanceCounter>>
        {
            { CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true) },
            { CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true) },
            { CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true) },
        };

        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries =
            new Dictionary<CounterType, Func<Series>>
            {
                { 
                    CounterType.Cpu,
                    () => new Series(CounterType.Cpu.ToString())
                    {
                        ChartType = SeriesChartType.SplineArea,
                        Color = Color.DarkGreen,
                    }
                },
                {
                    CounterType.Memory,
                    () => new Series(CounterType.Memory.ToString())
                    {
                        ChartType = SeriesChartType.FastLine,
                        Color = Color.MediumBlue,
                    }
                },
                {
                    CounterType.Disk,
                    () => new Series(CounterType.Disk.ToString())
                    {
                        ChartType = SeriesChartType.SplineArea,
                        Color = Color.DarkRed,
                    }
                },
            };

        public class Watch
        {
            public CounterType CounterType { get; }

            public Watch(CounterType counterType)
            {
                this.CounterType = counterType;
            }
        }

        public class Unwatch
        {
            public CounterType CounterType { get; }

            public Unwatch(CounterType counterType)
            {
                this.CounterType = counterType;
            }
        }
    }
}
