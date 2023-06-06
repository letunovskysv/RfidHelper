//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidMonitorService – Блок опроса меток (БОм).
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid.Modules
{
    #region Using
    using System;
    using System.IO.Ports;
    using System.Reflection.PortableExecutable;
    using System.Text;
    using Microsoft.Extensions.Logging;
    using SmartMinex.Runtime;
    #endregion Using

    internal class RfidMonitorService : TModule
    {
        #region Declarations

        const string NULL = "<нет данных>";

        /// <summary> Имя COM-порта.</summary>
        string _port;
        /// <summary> Адрес (modbus) на линии RS-485. 1 по умолчанмию.</summary>
        int _address;

        readonly IxLogger _logger;

        /// <summary> Список линий.</summary>
        readonly List<RfidAnchorReader> _context = new();

        #endregion Declarations

        public RfidMonitorService(IRuntime runtime, string port, int? address) : base(runtime)
        {
            Subscribe = new[] { MSG.ConsoleCommand };
            _port = port;
            _address = address ?? 1;
            _logger = new FileLogger(@"logs\rfiddevice.log");
            _logger.WriteLine("****************************************************************************************************");
        }

        protected override async Task ExecuteProcess()
        {
            Status = RuntimeStatus.Running;
            _context.Add(new RfidAnchorReader(_port, _address, _logger));
            Open();
            while (_sync.WaitOne() && (Status & RuntimeStatus.Loop) > 0)
                try
                {
                    while (_esb.TryDequeue(out TMessage m))
                    {
                        switch (m.Msg)
                        {
                            case MSG.ConsoleCommand:
                                if ((m.HParam == ProcessId || m.HParam == 0) && m.Data is string[] args && args.Length > 0)
                                    DoCommand(m.LParam, args);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Runtime.Send(MSG.ErrorMessage, ProcessId, 0, ex);
                }

            await base.ExecuteProcess();
        }

        /// <summary> Выполнить консольную команду.</summary>
        void DoCommand(long idTerminal, string[] args)
        {
            switch (args[0].ToUpper())
            {
                case "FIND":
                case "SEARCH":
                    Search(idTerminal, args.Length > 1 ? args[1] : _port);
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
                    if (args.Length > 1 && int.TryParse(args[1], out int addr))
                        ReadInfo(idTerminal, addr);
                    break;
            }
        }

        void Open()
        {
            foreach (var ctx in _context)
                if (!ctx.Connected)
                    if (ctx.Open())
                        Runtime.Send(MSG.TerminalLine, 0, 0, "Подключение к устройству выполнено успешно!");
                    else
                        Runtime.Send(MSG.TerminalLine, 0, 0, "Ошибка подключения к устройству " + ctx.LastError.Message);
        }

        /// <summary> Найдём все устройства на линии.</summary>
        void Search(long idTerminal, string portName)
        {
            var readers = _context.First();
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
            var dev = _context.FirstOrDefault()?.ReadInfo(address);
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
                    { "Общая наработка", dev.OperatingTimeGeneral.HasValue ? dev.OperatingTimeGeneral.Value.ToString() : NULL }
                });
        }

        void Send(byte[] data)
        {
            var line = _context.First();
            if (!line.Connected && line.Open())
                try
                {
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
    }
}
