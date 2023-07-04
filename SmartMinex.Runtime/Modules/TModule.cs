//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TModule – Базовый модуль.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Collections.Concurrent;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Reflection;
    #endregion Using

    public delegate IDatabase DatabaseConnectionHandler();

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

        /// <summary> Отправка сообщения непосредственно модулю напрямую, миную общую очередь.</summary>
        protected void SendDirect(TMessage m)
        {
            _esb.Enqueue(m);
            _sync?.Set();
        }

        public bool SetProperty(string propertyName, object value, out string? message)
        {
            message = null;
            var prop = GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null)
            {
                object val = null;
                if (prop.PropertyType.IsEnum)
                {
                    if (Enum.TryParse(prop.PropertyType, value?.ToString(), true, out var eval))
                        prop.SetValue(this, val = eval);
                    else
                    {
                        message = "Не верное значение «" + value + "» параметра «" + propertyName + "»!";
                        return false;
                    }
                }
                else if (prop.PropertyType == typeof(int))
                    prop.SetValue(this, val = val is int ? val : int.Parse(value.ToString()));
                else if (prop.PropertyType == typeof(float))
                    prop.SetValue(this, val = val is float ? val : float.Parse(value.ToString()));
                else if (prop.PropertyType == typeof(double))
                    prop.SetValue(this, val = val is double ? val : double.Parse(value.ToString()));
                else if (prop.PropertyType == typeof(bool))
                    prop.SetValue(this, val = val is bool ? val : bool.Parse(value.ToString().Replace("1", "True").Replace("0", "False")));
                else if (prop.PropertyType == typeof(string))
                    prop.SetValue(this, val = val is string ? val : value.ToString());
                else
                {
                    message = "Не верное значение «" + value + "» параметра «" + propertyName + "»!";
                    return false;
                }
                return true;
            }
            message = "Параметр «" + propertyName + "» не найден!";
            return false;
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