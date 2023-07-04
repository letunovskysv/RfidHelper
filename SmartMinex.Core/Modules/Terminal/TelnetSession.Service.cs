//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TelnetSession.Services – Терминальная клиентская сессия telnet.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System.Text;
    using System.Data;
    using System.Diagnostics;
    using System.Reflection;
    using System.Text.RegularExpressions;
    #endregion Using

    partial class TelnetSession
    {
        static DateTime? _prevCpuStartTime;
        static TimeSpan _prevTotalProcTime;

        /// <summary> Получить список модулей.</summary>
        void ShowModules(StringBuilder output, string command, string[] args)
        {
            using var data = new DataTable();
            data.Columns.AddRange(new[] {
                new DataColumn("ИД"),
                new DataColumn("Наименование сервиса"),
                new DataColumn("Статус"),
                new DataColumn("Сообщений"),
                new DataColumn("Время")
            });
            var rtm = (SmartRuntimeService)Runtime;
            data.Rows.Add(0, rtm.Name, RuntimeStatus.Running, rtm.MessageCount);

            ((SmartRuntimeService)Runtime).Modules.ToList().ForEach(m =>
                data.Rows.Add(
                    m.ProcessId,
                    m.Name,
                    m.Status,
                    "—",
                    "—")
                );

            PrintTable(output, data);
        }

        /// <summary> Получить сведения о системе СКПТ (объектовый сервер).</summary>
        void ShowSystemInfo(StringBuilder output, string command, string[] args) => UseDatabase(db =>
        {
            var rtm = (SmartRuntimeService)Runtime;
            var prc = Process.GetCurrentProcess();

            PrintDictionary(output, new Dictionary<string, string>()
            {
                { "Версия ОС", Environment.OSVersion.VersionString
                    + (System.Runtime.InteropServices.Marshal.SizeOf(typeof(IntPtr)) == 8 ? " x64 " : " x86 ")
                    + Environment.OSVersion.ServicePack },
                { "Среда выполнения", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription },
                { "СУБД", db.Version },
                { "Наименование БД", db.DatabaseName },
                { "Загрузка процессора", GetCpuUsage().ToString() + " %" },
                { "Используемая память", Math.Round(prc.PrivateMemorySize64 / 1048576.0, 2).ToString() + " Мб" }
            }
            , ": ");
        });

        static double GetCpuUsage()
        {
            var start = DateTime.UtcNow;
            var usage = Process.GetCurrentProcess().TotalProcessorTime;
            if (!_prevCpuStartTime.HasValue)
            {
                _prevCpuStartTime = start;
                _prevTotalProcTime = usage;
            }
            var cpuUsedMs = (usage - _prevTotalProcTime).TotalMilliseconds;
            var totalMsPassed = (start - _prevCpuStartTime.Value).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            _prevCpuStartTime = start;
            _prevTotalProcTime = usage;

            return Math.Round(cpuUsageTotal * 100.0, 2);
        }

        #region Module command (control)

        List<IModule> GetModules() =>
            ((SmartRuntimeService)Runtime).Modules.GetModules<IModule>();

        IModule GetModuleByNumber(int id) =>
            GetModules().FirstOrDefault(m => m.ProcessId == id);

        void DoModuleCommand(StringBuilder output, string command, string[] args) => UseModule(command, mod =>
        {
            if (args.Length > 2 && args[1].Trim() == "=" && Regex.IsMatch(args[0], @"[_\w]+"))
                SetModuleProperty(mod, args[0], string.Join(' ', args.Where((p, i) => i > 1).ToArray()));
            else
                Runtime.Send(MSG.ConsoleCommand, ProcessId, mod.ProcessId, args);
        });

        /// <summary> Получить конфигурацию модуля (процесса). Команда MOD CONFIG.</summary>
        void GetModuleProperties(StringBuilder output, string command, string[] args) => UseModule(command, mod =>
        {
            foreach (var prop in mod.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(m => m.MetadataToken))
            {
                output.Append("  ").Append(prop.Name).Append(" = ");

                var val = prop.GetValue(mod);
                if (prop.Name.Equals("Subscribe") && val is IEnumerable<int> msgs)
                    output.Append(string.Join(", ", msgs.Select(m => MSG.ToString(m)))).Append(NEWLINE);
                else if (prop.PropertyType.IsSZArray && val is IEnumerable<int> val32s)
                    output.Append(string.Join(", ", val32s)).Append(NEWLINE);
                else if (prop.PropertyType.IsSZArray && val is IEnumerable<long> val64s)
                    output.Append(string.Join(", ", val64s)).Append(NEWLINE);
                else
                    output.Append(val?.ToString() ?? "NULL").Append(NEWLINE);
            }
        });

        /// <summary> Установка свойств модуля.</summary>
        bool SetModuleProperty(IModule mod, string name, string value)
        {
            try
            {
                if (mod.SetProperty(name, value, out string msg))
                {
                    Print(TColor.GOOD("OK"));
                    return true;
                }
                Print(TColor.FAIL(msg));
            }
            catch (Exception ex)
            {
                Print(TColor.FAIL(ex.Message));
            }
            return false;
        }

        void UseModule(string command, Action<IModule> handler)
        {
            if (int.TryParse(Regex.Match(command, @"\d+").Value, out var npp))
            {
                var mod = GetModuleByNumber(npp);
                if (mod != null)
                {
                    handler?.Invoke(mod);
                    return;
                }
            }
            PrintLine($"Модуль #{npp} не найден!");
        }

        #endregion Module command (control)
    }
}
