using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;

namespace GithubActors.Actors
{
    /// <summary>
    /// Top-level actor responsible for coordinating and launching repo-processing jobs
    /// </summary>
    public class GithubCommanderActor : ReceiveActor, IWithUnboundedStash
    {
        #region Message classes

        public class CanAcceptJob
        {
            public CanAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class AbleToAcceptJob
        {
            public AbleToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        public class UnableToAcceptJob
        {
            public UnableToAcceptJob(RepoKey repo)
            {
                Repo = repo;
            }

            public RepoKey Repo { get; private set; }
        }

        #endregion

        private IActorRef _coordinator;
        private IActorRef _canAcceptJobSender;
        private int _pendingJobReplies;
        private RepoKey _jobRepoKey;

        public GithubCommanderActor()
        {
            this.Ready();

            Receive<UnableToAcceptJob>(job =>
            {
                _canAcceptJobSender.Tell(job);
            });

            Receive<AbleToAcceptJob>(job =>
            {
                _canAcceptJobSender.Tell(job);

                //start processing messages
                _coordinator.Tell(new GithubCoordinatorActor.BeginJob(job.Repo));

                //launch the new window to view results of the processing
                Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(new MainFormActor.LaunchRepoResultsWindow(job.Repo, Sender));
            });
        }
        
        public IStash Stash { get; set; }

        private void Ready()
        {
            Receive<CanAcceptJob>(job =>
            {
                this._coordinator.Tell(job);
                this._jobRepoKey = job.Repo;
                this.BecomeAsking();
            });
        }

        private void BecomeAsking()
        {
            this._canAcceptJobSender = Sender;

            this._pendingJobReplies = this._coordinator.Ask<Routees>(new GetRoutees()).Result.Members.Count();
            this.Become(this.Asking);

            Context.SetReceiveTimeout(TimeSpan.FromSeconds(3));
        }

        private void Asking()
        {
            this.Receive<CanAcceptJob>(_ => this.Stash.Stash());

            this.Receive<UnableToAcceptJob>(job =>
            {
                this._pendingJobReplies--;
                if (this._pendingJobReplies == 0)
                {
                    this._canAcceptJobSender.Tell(job);
                    this.BecomeReady();
                }
            });

            this.Receive<AbleToAcceptJob>(job =>
            {
                this._canAcceptJobSender.Tell(job);

                this.Sender.Tell(new GithubCoordinatorActor.BeginJob(job.Repo));

                Context.ActorSelection(ActorPaths.MainFormActor.Path).Tell(new MainFormActor.LaunchRepoResultsWindow(job.Repo, this.Sender));

                this.BecomeReady();
            });

            this.Receive<ReceiveTimeout>(_ =>
            {
                this._canAcceptJobSender.Tell(new UnableToAcceptJob(this._jobRepoKey));
                this.BecomeReady();
            });
        }

        private void BecomeReady()
        {
            this.Become(this.Ready);
            this.Stash.UnstashAll();

            Context.SetReceiveTimeout(null);
        }

        protected override void PreStart()
        {
            this._coordinator = Context.ActorOf(Props.Create(() => new GithubCoordinatorActor()).WithRouter(FromConfig.Instance), ActorPaths.GithubCoordinatorActor.Name);
            base.PreStart();
        }

        protected override void PreRestart(Exception reason, object message)
        {
            //kill off the old coordinator so we can recreate it from scratch
            _coordinator.Tell(PoisonPill.Instance);
            base.PreRestart(reason, message);
        }
    }
}
