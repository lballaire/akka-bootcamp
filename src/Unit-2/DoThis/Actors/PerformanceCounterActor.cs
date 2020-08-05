using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterActor : ReceiveActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _performanceCounterGenerator;
        private readonly Cancelable _cancelPublishing;
        private readonly HashSet<IActorRef> _subscriptions = new HashSet<IActorRef>();
        private PerformanceCounter _counter;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> performanceCounterGenerator)
        {
            this._seriesName = seriesName;
            this._performanceCounterGenerator = performanceCounterGenerator;
            this._cancelPublishing = new Cancelable(Context.System.Scheduler);
            
            this.Receive<GatherMetrics>(this.HandleGatherMetrics);
            this.Receive<SubscribeCounter>(this.HandleSubscribeCounter);
            this.Receive<UnsubscribeCounter>(this.HandleUnSubscribeCounter);
        }

        protected override void PreStart()
        {
            this._counter = this._performanceCounterGenerator();
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                Self,
                new GatherMetrics(),
                Self,
                this._cancelPublishing);
        }

        protected override void PostStop()
        {
            try
            {
                this._cancelPublishing.Cancel(false);
                this._counter.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            finally
            {
                base.PostStop();
            }
        }

        private void HandleGatherMetrics(GatherMetrics gatherMetrics)
        {
            var metric = new Metric(this._seriesName, this._counter.NextValue());
            foreach (var subscription in this._subscriptions)
            {
                subscription.Tell(metric);
            }
        }

        private void HandleSubscribeCounter(SubscribeCounter subscribeCounter)
        {
            this._subscriptions.Add(subscribeCounter.Subscriber);
        }

        private void HandleUnSubscribeCounter(UnsubscribeCounter unsubscribeCounter)
        {
            this._subscriptions.Remove(unsubscribeCounter.Subscriber);
        }
    }
}
