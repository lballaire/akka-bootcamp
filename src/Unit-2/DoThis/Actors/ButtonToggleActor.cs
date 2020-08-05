using System.Windows.Forms;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ButtonToggleActor : UntypedActor
    {
        private readonly IActorRef _coordinatorActor;
        private readonly Button _button;
        private readonly CounterType _counterType;
        private bool _isToggledOn;

        public ButtonToggleActor(IActorRef coordinatorActor, Button button, CounterType counterType, bool isToggledOn = false)
        {
            this._coordinatorActor = coordinatorActor;
            this._button = button;
            this._counterType = counterType;
            this._isToggledOn = isToggledOn;
        }

        protected override void OnReceive(object message)
        {
            if (message is Toggle)
            {
                if (this._isToggledOn)
                {
                    this._coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Unwatch(this._counterType));
                }
                else
                {
                    this._coordinatorActor.Tell(new PerformanceCounterCoordinatorActor.Watch(this._counterType));
                }
                
                this.FlipToggle();
            }
            else
            {
                this.Unhandled(message);
            }
        }

        private void FlipToggle()
        {
            this._isToggledOn = !this._isToggledOn;

            var statusText = this._isToggledOn ? "ON" : "OFF";
            this._button.Text = $"{this._counterType.ToString().ToUpperInvariant()} ({statusText})";
        }

        public class Toggle { }
    }
}
