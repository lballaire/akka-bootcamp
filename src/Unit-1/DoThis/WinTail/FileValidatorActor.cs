using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileValidatorActor : UntypedActor 
    {
        private readonly IActorRef _consoleWriter;

        public FileValidatorActor(IActorRef consoleWriter)
        {
            _consoleWriter = consoleWriter;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                this._consoleWriter.Tell(new Messages.NullInputError("Input was blank. Please try again.\n"));

                Sender.Tell(new Messages.ContinueProcessing());
            }
            else
            {
                var isValid = IsFileUri(msg);
                if (isValid)
                {
                    this._consoleWriter.Tell(new Messages.InputSuccess($"Starting processing for {msg}"));
                    Context.ActorSelection("akka://MyActorSystem/user/tailCoordinatorActor").Tell(new TailCoordinatorActor.StartTail(msg, _consoleWriter));
                }
                else
                {
                    this._consoleWriter.Tell(new Messages.ValidationError($"{msg} is not an existing URI on disk."));
                    Sender.Tell(new Messages.ContinueProcessing());
                }
            }
        }

        private bool IsFileUri(string msg)
        {
            return File.Exists(msg);
        }
    }
}
