//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: SmartRuntimeService –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using SmartMinex.Rfid.Modules;
    #endregion Using

    public sealed class SmartRuntimeService : BackgroundService, IRuntime
    {
        #region Declarations

        /// <summary> Системная шина предприятия. Очередь сообщений.</summary>
        readonly ConcurrentQueue<TMessage> _esb = new();
        /// <summary> Системная шина предприятия. Менеджер расписаний.</summary>
        readonly SmartScheduler<TMessage> _schedule = new();

        /// <summary> Запущенные модули в системе. Диспетчер задач.</summary>
        public readonly ModuleManager Modules;
        /// <summary> Диспетчер системной шины предприятия ESB.</summary>
        internal readonly ConcurrentDictionary<int, ProcessMessageEventHandler> Dispatcher = new();
        readonly ILogger<SmartRuntimeService> _logger;

        #endregion Declarations

        #region Properties

        public int[] Subscribe { get; set; }
        public long Id { get; set; }
        public int ProcessId { get; set; }
        public string Name { get; set; }
        public RuntimeStatus Status { get; private set; }
        public long MessageCount { get; private set; }
        public DateTime Started { get; }

        public IMetadata Metadata => throw new NotImplementedException();
        public Version Version => throw new NotImplementedException();

        public readonly TUser[] Users;

        #endregion Properties

        public SmartRuntimeService(ILogger<SmartRuntimeService> logger, IConfiguration config)
        {
            Started = DateTime.Now;
            Name = "Сервер приложений ПНВДНП";
            _logger = logger;
            _schedule.Fire += (o, m) => Send(m);

            Modules = new ModuleManager(this, config, logger);
            Modules.Created += OnModuleCreated;
            Modules.Removed += OnModuleRemoved;

            Users = config.GetSection("runtimeOptions:users").Get<TUser[]>();
        }

        protected override async Task ExecuteAsync(CancellationToken tkn)
        {
            Status = RuntimeStatus.StartPending;

            AppDomain.CurrentDomain.GetAssemblies() // Запуск системных обязательных модулей -->
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Contains(typeof(IStartup))).ToList()
                .ForEach(t => Modules.AddSingleton(t));

            _schedule.Start();
            await Task.Delay(100, tkn);

            Status = RuntimeStatus.Running;
            Send(MSG.RuntimeStarted, 0, 0, null);

            while (!tkn.IsCancellationRequested && (Status & RuntimeStatus.Loop) > 0)
            {
                if (_esb.TryDequeue(out TMessage m))
                {
                    if (Dispatcher.ContainsKey(MSG.All))
                        Dispatcher[MSG.All].Invoke(ref m);

                    if (Dispatcher.ContainsKey(m.Msg))
                        Dispatcher[m.Msg]?.Invoke(ref m);
                }
                else
                    await Task.Delay(50, tkn);
            }
            _schedule.Stop();
            Status = RuntimeStatus.Stopped;
        }

        #region Message

        public void Send(TMessage m)
        {
            ++MessageCount;
            _esb.Enqueue(m);
        }

        int Send(TMessage m, ScheduleParams pars)
        {
            if (_schedule.Add(m, pars) is int id)
                return id;

            Send(m);

            return 0;
        }

        public void Send(int type, long lparam) =>
            Send(type, lparam, 0, null);

        public void Send(int type, long lparam, long hparam) =>
            Send(type, lparam, hparam, null);

        public void Send(int type, long lparam, long hparam, object data) =>
            Send(new TMessage(type, lparam, hparam, data));

        public int Send(TMessage m, long delay) =>
            Send(m, new ScheduleParams { InitialDelay = delay });

        public int Send(TMessage m, long delay, long period) =>
            Send(m, new ScheduleParams { InitialDelay = delay, Period = period });

        public int Send(TMessage m, DateTime dateTime) =>
            Send(m, new ScheduleParams { DateTime = dateTime });

        public int Send(int type, long lparam, long hparam, object data, long delay) =>
            Send(new TMessage(type, lparam, hparam, data), new ScheduleParams { InitialDelay = delay });

        public int Send(int type, long lparam, long hparam, object data, long delay, long period) =>
            Send(new TMessage(type, lparam, hparam, data), new ScheduleParams { InitialDelay = delay, Period = period });

        public int Send(int type, long lparam, long hparam, object data, DateTime dateTime) =>
            Send(new TMessage(type, lparam, hparam, data), new ScheduleParams { DateTime = dateTime });

        public bool RemoveTask(int id) =>
            _schedule.Remove(id);

        public bool ResetTask(int id) =>
            _schedule.Reset(id);

        public bool Modify(int id, TMessage m, long delay) =>
            _schedule.Modify(id, m, new ScheduleParams { InitialDelay = delay });

        public bool Modify(int id, TMessage m, long delay, long period) =>
            _schedule.Modify(id, m, new ScheduleParams { InitialDelay = delay, Period = period });

        public bool Modify(int id, TMessage m, DateTime dateTime) =>
            _schedule.Modify(id, m, new ScheduleParams { DateTime = dateTime });

        #endregion Message

        #region Events

        void OnModuleCreated(object sender, EventArgs e)
        {
            if (sender is IModule mod)
                Console.WriteLine($"Created[{mod.ProcessId}]: {mod.Name}");
        }

        void OnModuleRemoved(object sender, EventArgs e)
        {
            if (sender is IModule mod)
                Console.WriteLine($"Removed[{mod.ProcessId}]: {mod.Name}");
        }

        public void ServiceStatusChanged(IModule sender)
        {
            if (sender is IModule mod)
                switch (sender.Status)
                {
                    case RuntimeStatus.StartPending:
                        Console.WriteLine($"Starting[{mod.ProcessId}]: {mod.Name}");
                        break;

                    case RuntimeStatus.Running:
                        Console.WriteLine($"Started[{mod.ProcessId}]: {mod.Name}");
                        break;

                    case RuntimeStatus.StopPending:
                        Console.WriteLine($"Stopping[{mod.ProcessId}]: {mod.Name}");
                        break;

                    case RuntimeStatus.Stopped:
                        Console.WriteLine($"Stopped[{mod.ProcessId}]: {mod.Name}");
                        break;

                    case RuntimeStatus.Pause:
                        Console.WriteLine($"Paused[{mod.ProcessId}]: {mod.Name}");
                        break;

                    case RuntimeStatus.Failed:
                        Console.WriteLine($"Failed[{mod.ProcessId}]: {mod.Name}");
                        break;

                    default:
                        Console.WriteLine($"Unknown[{mod.ProcessId}]: {mod.Name}");
                        break;
                }
        }

        #endregion Events

        #region IRuntime implementations

        public void Start() => throw new NotImplementedException();
        public void Stop() => throw new NotImplementedException();
        public void Kill() => throw new NotImplementedException();
        public void ProcessMessage(ref TMessage m) => throw new NotImplementedException();

        public T? GetService<T>() =>
            Modules.GetModule<T>();

        public string GetWorkDirectory()
        {
            throw new NotImplementedException();
        }

        public IDatabase CreateDbConnection()
        {
            throw new NotImplementedException();
        }

        #endregion IRuntime implementations
    }
}
