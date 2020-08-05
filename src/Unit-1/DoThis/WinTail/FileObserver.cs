using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileObserver : IDisposable
    {
        private readonly IActorRef _tailActor;
        private readonly string _filePath;
        private readonly string _fileDirectory;
        private readonly string _fileName;
        private FileSystemWatcher _fileWatcher;

        public FileObserver(IActorRef tailActor, string filePath)
        {
            _tailActor = tailActor;
            _filePath = filePath;
            _fileDirectory = Path.GetDirectoryName(_filePath);
            _fileName = Path.GetFileName(_filePath);
        }

        public void Dispose()
        {
            this._fileWatcher.Dispose();
        }

        public void Start()
        {
            this._fileWatcher = new FileSystemWatcher(this._fileDirectory, this._fileName)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            };

            this._fileWatcher.Changed += OnFileChanged;
            this._fileWatcher.Error += OnFileError;
            this._fileWatcher.EnableRaisingEvents = true;

        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                this._tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRefs.NoSender);
            }
        }

        private void OnFileError(object sender, ErrorEventArgs e)
        {
            this._tailActor.Tell(new TailActor.FileError(this._fileName, e.GetException().Message), ActorRefs.NoSender);
        }
    }
}
