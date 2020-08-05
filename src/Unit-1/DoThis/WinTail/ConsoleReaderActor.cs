using System;
using Akka.Actor;
using Akka.IO;

namespace WinTail
{
    /// <summary>
    /// Actor responsible for reading FROM the console. 
    /// Also responsible for calling <see cref="ActorSystem.Terminate"/>.
    /// </summary>
    class ConsoleReaderActor : UntypedActor
    {
        public const string StartCommand = "start";
        public const string ExitCommand = "exit";
        
        protected override void OnReceive(object message)
        {
            if (message.Equals(StartCommand))
            {
                PrintInstructions();
            }

            GetAndValidateInput();
        }

        private void GetAndValidateInput()
        {
            var message = Console.ReadLine();
            if (!string.IsNullOrEmpty(message) && String.Equals(message, ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                // shut down the entire actor system (allows the process to exit)
                Context.System.Terminate();
            }
            else
            {
                Context.ActorSelection("akka://MyActorSystem/user/validatorActor").Tell(message);
            }
        }

        private void PrintInstructions()
        {
            //Console.WriteLine("Write whatever you want into the console!");
            //Console.WriteLine("Some entries will pass validation, and some won't...\n\n");
            //Console.WriteLine("Type 'exit' to quit this application at any time.\n");

            Console.WriteLine("Please provide an URI of a log file on disk. \n");
        }
    }
}