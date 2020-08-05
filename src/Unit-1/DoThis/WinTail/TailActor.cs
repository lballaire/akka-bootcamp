using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class TailActor : UntypedActor
    {
        private readonly IActorRef _reporterActor;
        private readonly string _filePath;
        private FileObserver _observer;
        private FileStream _fileStream;
        private StreamReader _fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            this._reporterActor = reporterActor;
            this._filePath = filePath;
        }

        protected override void PreStart()
        {
            var fullPath = Path.GetFullPath(this._filePath);
            this._observer = new FileObserver(Self, fullPath);
            this._observer.Start();

            this._fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this._fileStreamReader = new StreamReader(this._fileStream);

            var text = this._fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(this._filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                var text = this._fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    this._reporterActor.Tell(text);
                }
            }
            else if (message is FileError error)
            {
                _reporterActor.Tell($"Tail error: {error.Reason}");
            }
            else if (message is InitialRead initialRead)
            {
                _reporterActor.Tell(initialRead.Text);
            }
        }

        protected override void PostStop()
        {
            this._observer.Dispose();
            this._observer = null;
            this._fileStreamReader.Close();
            this._fileStreamReader.Dispose();
            base.PostStop();
        }

        public class FileWrite
        {
            public string FileName { get; }

            public FileWrite(string fileName)
            {
                FileName = fileName;
            }
        }

        public class FileError
        {
            public string FileName { get; }
            public string Reason { get; }

            public FileError(string fileName, string reason)
            {
                FileName = fileName;
                Reason = reason;
            }
        }

        public class InitialRead
        {
            public string FileName { get; }
            public string Text { get; }

            public InitialRead(string fileName, string text)
            {
                FileName = fileName;
                Text = text;
            }
        }
    }
}
