//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TModule – Базовый модуль.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Collections.Concurrent;
    #endregion Using

    public class TModule : IModule
    {
        #region Declarations

        protected readonly IRuntime Runtime;
        protected readonly ConcurrentQueue<TMessage> _esb = new();
        protected AutoResetEvent _sync;
        Task _task;

        #endregion Declarations

        #region Properties

        public long Id { get; set; }
        public string Name { get; set; }
        public int ProcessId { get; set; }
        public RuntimeStatus Status { get; set; }

        public Version Version { get; protected set; }
        public int[] Subscribe { get; set; }

        public Exception LastError { get; set; }

        #endregion Properties

        public TModule(IRuntime runtime)
        {
            Runtime = runtime;
            Version = System.Reflection.Assembly.GetExecutingAssembly().GetName()?.Version ?? new Version();
        }

        public void Dispose() =>
            GC.SuppressFinalize(this);

        public void ProcessMessage(ref TMessage m)
        {
            if ((Status & RuntimeStatus.Loop) > 0)
            {
                _esb.Enqueue(m);
                _sync.Set();
            }
        }

        public virtual void Start()
        {
            Status = RuntimeStatus.StartPending;
            _sync = new AutoResetEvent(false);
            _task = Task.Factory.StartNew(async () => await ExecuteProcess(), TaskCreationOptions.LongRunning);
        }

        public virtual void Stop()
        {
            Status = RuntimeStatus.StopPending;
            _sync?.Set();
            _sync = null;
        }

        public virtual void Kill()
        {
            Stop();
        }

        protected virtual async Task ExecuteProcess()
        {
            Status = RuntimeStatus.Stopped;
            await Task.Delay(0);
        }

        #region Database methods...

        /// <summary> Подключение и выполнение в среде БД.</summary>
        protected void UseDatabase(Action<IDatabase> handler)
        {
            var db = Runtime.CreateDbConnection();
            try
            {
                db.Open();
                handler?.Invoke(db);
            }
            finally
            {
                db.Close();
            }
        }
        #endregion Database methods...
    }
}