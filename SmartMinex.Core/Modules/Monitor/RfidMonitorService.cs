//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidMonitorService – Блок опроса меток (БОм).
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid
{
    #region Using
    using System;
    using System.IO.Ports;
    using System.Reflection;
    using SmartMinex.Rfid.Modules;
    using SmartMinex.Runtime;
    using static System.Runtime.InteropServices.JavaScript.JSType;
    #endregion Using

    internal class RfidMonitorService : TModule, IOServer
    {
        #region Declarations

        const string NULL = "<нет данных>";

        /// <summary> Настройки COM-порта.</summary>
        SerialPortSetting _serial;
        /// <summary> Устройства (из конфигурации).</summary>
        TDevice[] _init_devices;

        readonly IxLogger _logger;
        CancellationTokenSource _cts;
        volatile bool _busy;
        readonly object _syncRoot = new();

        /// <summary> Список линий.</summary>
        readonly List<RfidAnchorReader> _readers = new();
        RfidTag[]? _tags_historian;

        #endregion Declarations

        #region Properties

        /// <summary> Период опроса меток. 0 - вручную.</summary>
        public int SamplingInterval { get; set; }
        /// <summary> Время, когда метка считается ушедшей.</summary>
        public int TagIdle { get; set; }
        /// <summary> Счётчик циклов опроса.</summary>
        public int PollCount { get; set; }

        public List<IDevice> Devices => _readers.SelectMany(r => r.Devices).ToList();

        #endregion Properties

        public RfidMonitorService(IRuntime runtime, SerialPortSetting serial, TDevice[]? devices, int? samplingInterval, int? tagIdle) : base(runtime)
        {
            Subscribe = new[] { MSG.ConsoleCommand, MSG.ReadTagsRuntime, MSG.ReadTagsHistorian, MSG.GetPollInterval, MSG.SetPollInterval };
            _serial = serial;
            _init_devices = devices;
            SamplingInterval = samplingInterval ?? 0;
            TagIdle = tagIdle ?? 3600;
            _logger = new FileLogger(@"logs\rfiddevice.log");
            _logger.WriteLine("****************************************************************************************************");
        }

        protected override async Task ExecuteProcess()
        {
            Status = RuntimeStatus.Running;
            _readers.Add(new RfidAnchorReader(_serial, _logger, _init_devices));
            Open();
            PollTagsAsync();
            while (_sync.WaitOne() && (Status & RuntimeStatus.Loop) > 0)
                try
                {
                    while (_esb.TryDequeue(out TMessage m))
                    {
                        switch (m.Msg)
                        {
                            case MSG.ReadTagsRuntime:
                                OnPollTags(m.LParam);
                                break;

                            case MSG.ReadTagsHistorian:
                                Runtime.Send(MSG.ReadTagsData, m.LParam, 0, _tags_historian);
                                break;

                            case MSG.ConsoleCommand:
                                if ((m.HParam == ProcessId || m.HParam == 0) && m.Data is string[] args && args.Length > 0)
                                    DoCommand(m.LParam, args);
                                break;

                            case MSG.GetPollInterval:
                                Runtime.Send(MSG.ReadTagsData, m.LParam, 0, SamplingInterval);
                                break;

                            case MSG.SetPollInterval:
                                SamplingInterval = m.Data is int interval ? interval : SamplingInterval;
                                StopPoll();
                                PollTagsAsync();
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Runtime.Send(MSG.ErrorMessage, ProcessId, 0, ex);
                }

            StopPoll();
            await base.ExecuteProcess();
        }

        #region Acquisition

        void PollTagsAsync()
        {
            if (SamplingInterval > 0)
            {
                while (_busy) Task.Delay(50).Wait();
                _busy = true;
                _cts = new CancellationTokenSource();
                var tkn = _cts.Token;
                Task.Factory.StartNew(() =>
                {
                    var timer = new TimerLaps(SamplingInterval * 1000, _cts.Token);
                    while (!tkn.IsCancellationRequested)
                    {
                        try
                        {
                            var tags = ReadTagsFromBuffer();
                            if (tags != null)
                            {
                                var hist = _tags_historian;
                                var ts = DateTime.Now;
                                var res = new List<RfidTag>();
                                foreach (var tag in tags)
                                {
                                    var battery = tag.BatteryWait && hist != null
                                        ? hist.Any(t => t.Code == tag.Code) ? hist.First(t => t.Code == tag.Code).Battery : tag.Battery : tag.Battery;

                                    res.Add(new RfidTag(tag.Code, (int)tag.Flags, battery));
                                }
                                hist?.Where(t => (ts - t.Modified).TotalSeconds <= TagIdle && !tags.Any(nt => nt.Code == t.Code)).ToList()
                                    .ForEach(t => res.Add(new RfidTag(t.Code, (int)t.Flags, t.Battery, t.Modified, RfidStatus.Fault)));

                                _tags_historian = res.OrderBy(t => t.Code).ToArray();
                                Runtime.Send(MSG.TagsUpdated, ProcessId);
                                PollCount++;
                            }
                        }
                        catch
                        {
                        }
                        timer.DoEvents();
                    }
                });
                _busy = false;
            }
        }

        void StopPoll()
        {
            _cts?.Cancel();
        }

        RfidTag[]? ReadTagsFromBuffer()
        {
            lock (_syncRoot)
                return _readers.FirstOrDefault()?.ReadTagsFromBuffer();
        }
   
        #endregion Acquisition

        /// <summary> Выполнить консольную команду.</summary>
        void DoCommand(long idTerminal, string[] args)
        {
            switch (args[0].ToUpper())
            {
                case "FIND":
                case "SEARCH":
                    Search(idTerminal, args.Length > 1 ? args[1] : _serial.Name);
                    break;

                case "SEND":
                    Send(args[1..].Select(n => byte.TryParse(n[2..], System.Globalization.NumberStyles.HexNumber, null, out byte num) ? num : (byte)0).ToArray());
                    break;

                case "PORTS":
                    Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, "Доступные порты: ");
                    foreach (string portName in SerialPort.GetPortNames())
                        Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, portName);
                    break;

                case "DEV":
                    if (args.Length > 1)
                    {
                        if (int.TryParse(args[1], out int addr))
                            ReadInfo(idTerminal, addr);
                    }
                    else
                        Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, string.Join("\r\n", _readers.First().Devices.Select(d => d.Name)));
                    break;

                case "POLL":
                    if (args.Length > 1 && int.TryParse(args[1], out int addr2))
                        PollTags(idTerminal, addr2);
                    break;

                case "TAGS":
                    ShowTags(idTerminal);
                    break;

                case "?":
                case "HELP":
                    OnHelp(idTerminal);
                    break;

                default:
                    Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, $"Неверная команда: " + string.Join(' ', args));
                    break;
            }
        }

        void Open()
        {
            foreach (var ctx in _readers)
                if (!ctx.Connected)
                    if (ctx.Open())
                        Runtime.Send(MSG.TerminalLine, 0, 0, "Подключение к устройству выполнено успешно!");
                    else
                        Runtime.Send(MSG.TerminalLine, 0, 0, "Ошибка подключения к устройству " + ctx.LastError.Message);
        }

        /// <summary> Найдём все устройства на линии.</summary>
        void Search(long idTerminal, string portName)
        {
            var readers = _readers.First();
            try
            {
                Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, $"Поиск устройств на линии " + portName + ":");
                int cnt = 0;
                for (int addr = 1; addr < 255; addr++)
                    if (readers.TryGetName(addr, out var name))
                    {
                        Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, $"{TColor.STARTLINE}Найдено устройство \"{name}\" по адресу {addr}");
                        cnt++;
                    }
                    else if (addr % 20 == 0)
                        Runtime.Send(MSG.Terminal, ProcessId, idTerminal, TColor.STARTLINE + new string(' ', 20) + TColor.STARTLINE + ".");
                    else
                        Runtime.Send(MSG.Terminal, ProcessId, idTerminal, ".");

                Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, TColor.STARTLINE + new string(' ', 20) + TColor.STARTLINE + $"Найдено {cnt} устройств.");
            }
            catch (Exception ex)
            {
                Runtime.Send(MSG.TerminalLine, 0, 0, "Ошибка подключения к устройству " + ex.Message);
            }
        }

        void ReadInfo(long idTerminal, int address)
        {
            var dev = _readers.FirstOrDefault()?.ReadInfo(address);
            if (dev != null)
                Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, new Dictionary<string, string>()
                {
                    { "ID", dev.Uid },
                    { "Address", dev.Address.ToString() },
                    { "Name", dev.Name },
                    { "HW", dev.HW },
                    { "Serial", dev.Serial },
                    { "App name", dev.AppName },
                    { "App type", dev.AppType },
                    { "App version", dev.AppVersion },
                    { "App Git Hash", dev.AppGitHash },
                    { "App Git Tick", dev.AppGitTick },
                    { "App Git Stamp", dev.AppGitStamp },
                    { "App Git Tag", dev.AppGitTag },
                    { "Boot version", dev.BootVersion },
                    { "Tag ANQ VPL", dev.TagAnqVpl },
                    { "Srv ANQ VPL", dev.SrvAnqVpl },
                    { "Modbus version", dev.ModbusVersion },
                    { "AnchorSettings CRC-16", dev.AnchorCRC },
                    { "Дата/Время запуска", dev.Started.HasValue ? dev.Started.Value.ToString() : NULL },
                    { "Наработка после старта", dev.OperatingTimeStarted.HasValue ? dev.OperatingTimeStarted.Value.ToString() : NULL },
                    { "Общая наработка", dev.OperatingTimeGeneral.HasValue ? dev.OperatingTimeGeneral.Value.ToString() : NULL },
                    { "Состояние", dev.State.ToDescription() },
                    { "Режим ответа RTLS", dev.RtlsMode.ToString() }
                });
        }

        void Send(byte[] data)
        {
            var line = _readers.First();
            if (line.Connected || line.Open())
                try
                {
                    line.Send(data);
                    var resp = line.Receive();
                    if (resp == null)
                        Runtime.Send(MSG.TerminalLine, 0, 0, "Данные не получены.");
                    else
                        Runtime.Send(MSG.TerminalLine, 0, 0, "RX: " + string.Join(' ', resp.Select(n => "0x" + n.ToString("X2"))));
                }
                catch (Exception ex)
                {
                    Runtime.Send(MSG.TerminalLine, 0, 0, "Ошибка получения данных. " + ex.Message);
                    Runtime.Send(MSG.ErrorMessage, 0, 0, ex);
                }
        }

        void PollTags(long idTerminal, int address)
        {
            var tags = ReadTagsFromBuffer()?.Select(t => t.ToString());
            if (tags != null)
            {
                Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal,
                    "Найдено " + tags.Count() + " RFID-меток:\r\n  Номер Батарея   Флаги\r\n" +
                    string.Join("\r\n", tags) +
                    "\r\n, найдено " + tags.Count() + " RFID-меток.\r\n");
            }
        }

        void ShowTags(long idTerminal)
        {
            var tags = ReadTagsFromBuffer()?.Select(t => t.ToString());
            if (tags != null)
                Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, "Последние " + tags.Count() + " RFID-меток:\r\n  Номер Батарея   Флаги\r\n" + string.Join("\r\n", tags));
        }

        void OnPollTags(long idRequest)
        {
            try
            {
                var tags = ReadTagsFromBuffer();
                if (tags != null)
                    Runtime.Send(MSG.ReadTagsData, idRequest, 0, tags);
                else
                    Runtime.Send(MSG.ReadTagsData, idRequest, -1, "Ошибка запроса меток.");
            }
            catch (Exception ex)
            {
                Runtime.Send(MSG.ReadTagsData, idRequest, -1, ex);
            }
        }

        void OnHelp(long idTerminal)
        {
            var asm = Assembly.GetAssembly(typeof(RfidMonitorService));
            var name = asm.GetManifestResourceNames().FirstOrDefault(r => r.EndsWith(nameof(RfidMonitorService) + ".man"));
            if (name != null)
            {
                using var ms = asm.GetManifestResourceStream(name);
                using var reader = new StreamReader(ms);
                Runtime.Send(MSG.TerminalLine, ProcessId, idTerminal, reader.ReadToEnd());
            }
        }
    }
}
