//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidMonitorService – Блок опроса меток (БОм).
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid.Modules
{
    #region Using
    using System;
    using System.Text;
    using SmartMinex.Runtime;
    #endregion Using

    internal class RfidMonitorService : TModule
    {
        #region Declarations

        /// <summary> Имя COM-порта.</summary>
        string _port;
        /// <summary> Адрес (modbus) на линии RS-485. 1 по умолчанмию.</summary>
        int _address;

        #endregion Declarations

        public RfidMonitorService(IRuntime runtime, string port, int? address) : base(runtime)
        {
            Subscribe = new[] { MSG.ConsoleCommand };
            _port = port;
            _address = address ?? 1;
        }

        protected override async Task ExecuteProcess()
        {
            Status = RuntimeStatus.Running;
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
                case "CONNECT":
                    Connect();
                    break;

                case "SEND":
                    Send(args[1..].Select(n => byte.TryParse(n[2..], System.Globalization.NumberStyles.HexNumber, null, out byte num) ? num : (byte)0).ToArray());
                    break;
            }
        }

        void Connect()
        {
            var reader = new RfidReader(_port, _address);
            try
            {
                reader.Open();
                Runtime.Send(MSG.Terminal, 0, 0, "Подключение к устройству выполнено успешно!");
                reader.Close();
            }
            catch (Exception ex)
            {
                Runtime.Send(MSG.Terminal, 0, 0, "Ошибка подключения к устройству " + ex.Message);
            }
        }

        void Send(byte[] data)
        {
            var reader = new RfidReader(_port, _address);
            try
            {
                reader.Open();
                reader.Send(data);
                Task.Delay(1000);
            }
            catch (Exception ex)
            {
                Runtime.Send(MSG.Terminal, 0, 0, "Ошибка подключения к устройству. " + ex.Message);
            }
            try
            {
                var resp = reader.Receive();
                reader.Close();
                if (resp == null)
                    Runtime.Send(MSG.Terminal, 0, 0, "Данные не получены.");
                else
                    Runtime.Send(MSG.Terminal, 0, 0, "RX: " + string.Join(' ', resp.Select(n => "0x" + n.ToString("X2"))));
            }
            catch (Exception ex)
            {
                Runtime.Send(MSG.Terminal, 0, 0, "Ошибка получения данных. " + ex.Message);
                Runtime.Send(MSG.ErrorMessage, 0, 0, ex);
            }
        }
    }
}
