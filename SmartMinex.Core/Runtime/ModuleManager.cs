//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: ModuleManager – Модуль управления модулями в Системе.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System.Collections;
    using System.Collections.Concurrent;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    #endregion Using

    /// <summary> Модуль управления модулями (микросервисами).</summary>
    public sealed class ModuleManager : IEnumerable<IModule>
    {
        #region Declarations

        readonly ILogger _logger;
        readonly SmartRuntimeService _rtm;
        readonly IConfiguration _cfg;
        readonly ConcurrentDictionary<ModuleDescript, IModule> _modules = new();

        volatile int _count;

        public event EventHandler Created;
        public event EventHandler Removed;

        #endregion Declarations

        public ModuleManager(SmartRuntimeService runtime, IConfiguration configuration, ILogger<SmartRuntimeService> logger)
        {
            _rtm = runtime;
            _cfg = configuration;
            _logger = logger;
        }

        #region IEnumerable implementaion

        public IEnumerator<IModule> GetEnumerator() =>
            _modules.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            _modules.Values.GetEnumerator();

        #endregion IEnumerable implementaion

        #region Add/Remove operations

        public bool Contains(long idProcess) =>
            _modules.ContainsKey(idProcess);

        public ModuleManager AddSingleton<TModule>(IModule implementationInstance) where TModule : IModule
        {
            _modules.TryAdd(new ModuleDescript(++_count, typeof(IModule)), implementationInstance);
            return this;
        }

        /// <summary> Создание и запуск единственного экземпляра модуля.</summary>
        public IModule AddSingleton(Type serviceType, params object[] args)
        {
            IModule mod = null;
            try
            {
                var parameters = serviceType.GetConstructors()[0].GetParameters();
                var prms = new object[parameters.Length];
                int n = 0;
                var modinfo = args.FirstOrDefault(m => m is ModuleInfo) as ModuleInfo;
                for (int i = 0; i < prms.Length; i++)
                {
                    var prm = parameters[i];
                    var ptype = prm.ParameterType;
                    var injectionType = ptype == typeof(IRuntime) ? _rtm
                        : GetModule(ptype) ?? (ptype == typeof(IConfiguration) ? _cfg : null);

                    if (injectionType == null)
                    {
                        object pval = modinfo?[prm.Name, prm.ParameterType];
                        prms[i] = (args.Length > n && ptype == args[n].GetType())
                            ? args[n]
                            : pval != null
                              ? (ptype.IsEnum && Enum.TryParse(ptype, modinfo[prm.Name], true, out object enumval))
                                ? enumval
                                : ptype == typeof(int?) && pval is string ? int.Parse(pval.ToString()) : pval
                              : ptype.Equals(typeof(string))
                                ? default(string)
                                : ptype.IsClass
                                    ? modinfo.Get(ptype, prm.Name)
                                    : Activator.CreateInstance(ptype);

                        n++;
                    }
                    else prms[i] = injectionType;
                }
                mod = (IModule)Activator.CreateInstance(serviceType, prms);
                mod.ProcessId = ++_count;
                mod.Name = mod.Name ?? modinfo?.Name;
                _modules.TryAdd(new ModuleDescript(mod.ProcessId, mod.GetType()), mod);
                Task.Run(() => Created?.Invoke(mod, EventArgs.Empty));

                if (mod.Subscribe != null)
                    foreach (var msg in mod.Subscribe)
                        if (_rtm.Dispatcher.ContainsKey(msg))
                            _rtm.Dispatcher[msg] += mod.ProcessMessage;
                        else
                            _rtm.Dispatcher.TryAdd(msg, mod.ProcessMessage);

                mod.Start();
            }
            catch (Exception ex)
            {
                mod?.Kill();
                _logger.LogError($"Ошибка запуска модуля {serviceType.Name}: \n" + ex.Message);
            }
            return mod;
        }

        /// <summary> Remove (kill) of the module.</summary>
        public bool Remove(IModule instance)
        {
            if (instance == null) return false;

            _modules.TryRemove(instance.ProcessId, out IModule removed);

            if (instance is IModule mod && mod.Subscribe != null)
            {
                foreach (var msg in mod.Subscribe)
                    _rtm.Dispatcher[msg] -= mod.ProcessMessage;

                var timer = DateTime.Now;
                mod.Stop();
                while (instance.Status != RuntimeStatus.Stopped)
                {
                    if ((DateTime.Now - timer).TotalSeconds > 5)
                    {
                        mod.Kill();
                        break;
                    }
                    Thread.Sleep(250);
                }
                Task.Run(() => Removed?.Invoke(instance, EventArgs.Empty));
                mod.Dispose();
            }
            return true;
        }

        #endregion Add/Remove operations

        #region Access methods

        public object GetProcess(long processId) =>
            _modules.FirstOrDefault(s => s.Key.ProcessId == processId).Value;

        /// <summary> Возвращает модуль содержащий указанный интерфейс (тип).</summary>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        object GetModule(Type serviceType) =>
            _modules.Values.FirstOrDefault(m => m.GetType().GetInterfaces().Contains(serviceType));

        public TService GetModule<TService>() =>
            (TService)_modules.Values.FirstOrDefault(m => m is TService);

        public List<TService> GetModules<TService>() =>
            _modules.Values.Where(m => m is TService).Select(m => (TService)m).ToList();

        #endregion Access methods

        #region Nested types

        struct ModuleDescript
        {
            public long ProcessId;
            public Type Type;

            public ModuleDescript(long processId, Type type)
            {
                ProcessId = processId;
                Type = type;
            }

            public static implicit operator ModuleDescript(long value) =>
                new ModuleDescript(value, typeof(object));

            public static bool operator ==(ModuleDescript a, ModuleDescript b)
            {
                return a.ProcessId == b.ProcessId;
            }

            public static bool operator !=(ModuleDescript a, ModuleDescript b)
            {
                return a.ProcessId != b.ProcessId;
            }

            public override bool Equals(object obj)
            {
                return this == (ModuleDescript)obj;
            }

            public override int GetHashCode()
            {
                return (int)ProcessId;
            }
        }

        #endregion Nested types
    }

    class ModuleInfo
    {
        public long Id { get; }
        public StartState Start { get; }
        public string Name { get; }
        public string Type { get; }

        /// <summary> Зависимости. Идентификаторы процесса от которого зависит данная служба.</summary>
        public long[] Depend { get; set; }

        IConfigurationSection _cfg;

        public string? this[string name] =>_cfg.GetSection(name)?.Value;
        public object? this[string name, Type type] => _cfg.GetSection(name).Get(type);
        public object Get(Type type, string name)
        {
            object res = Activator.CreateInstance(type);
            _cfg.GetSection(name).Bind(res);
            return res;
        }

        /// <summary> Чтение параметров запуска модуля.</summary>
        public ModuleInfo(IConfigurationSection config)
        {
            _cfg = config;
            foreach (var p in config.GetChildren().ToDictionary(sect => sect.Key, sect => sect.Value))
                switch (p.Key)
                {
                    case "start":
                        Start = Enum.TryParse<StartState>(config[p.Key], true, out var start) ? start : StartState.Disable;
                        break;
                    case "name":
                        Name = p.Value;
                        break;
                    case "type":
                        Type = p.Value;
                        break;
                }
        }
    }

    public enum StartState
    {
        Auto, Manual, Disable
    }
}