using System;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Configuration;

namespace WinTail
{
    #region Program
    class Program
    {
        public static ActorSystem MyActorSystem;

        static void Main(string[] args)
        {
            // initialize MyActorSystem
            MyActorSystem = ActorSystem.Create("MyActorSystem");

            // time to make your first actors!
            var consoleWriterProps = Props.Create<ConsoleWriterActor>();
            var consoleWriterActor = MyActorSystem.ActorOf(consoleWriterProps, "consoleWriterActor");
            
            //var validationProps = Props.Create<ValidationActor>(consoleWriterActor);
            //var validationActor = MyActorSystem.ActorOf(validationProps, "validationActor");

            var tailCoordinatorProps = Props.Create<TailCoordinatorActor>();
            var tailCoordinatorActor = MyActorSystem.ActorOf(tailCoordinatorProps, "tailCoordinatorActor");

            var fileValidatorProps =
                Props.Create(() => new FileValidatorActor(consoleWriterActor));
            var validatorActor = MyActorSystem.ActorOf(fileValidatorProps, "validatorActor");
            
            var consoleReaderProps = Props.Create<ConsoleReaderActor>();
            var consoleReaderActor = MyActorSystem.ActorOf(consoleReaderProps, "consoleReaderActor");

            // tell console reader to begin
            consoleReaderActor.Tell(ConsoleReaderActor.StartCommand);

            // blocks the main thread from exiting until the actor system is shut down
            MyActorSystem.WhenTerminated.Wait();
        }
    }
    #endregion
}
