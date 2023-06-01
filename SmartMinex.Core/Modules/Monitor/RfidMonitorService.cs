//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidMonitorService – Блок опроса меток (БОм).
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid.Modules
{
    #region Using
    using System;
    using System.IO.Ports;
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
                case "FIND":
                case "SEARCH":
                    Search();
                    break;

                case "CONNECT":
                    Connect();
                    break;

                case "SEND":
                    Send(args[1..].Select(n => byte.TryParse(n[2..], System.Globalization.NumberStyles.HexNumber, null, out byte num) ? num : (byte)0).ToArray());
                    break;

                case "PORTS":
                    foreach (string portName in SerialPort.GetPortNames())
                        Runtime.Send(MSG.Terminal, 0, 0, portName);
                    break;
            }
        }

        void Search()
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

        private void Port_ErrorReceived1(object sender, SerialErrorReceivedEventArgs e)
        {
            Runtime.Send(MSG.ErrorMessage, 0, 0, "Port_ErrorReceived1 " + e.EventType.ToString());
        }

        void Send(byte[] data)
        {
            try
            {
                SerialPort port = new SerialPort("COM1", 38400, Parity.Even, 8, StopBits.One);
                port.ReadTimeout = 100;
                port.WriteTimeout = 100;
                port.Open();
          
                var cmd = RfidReader.CRC16(new byte[] { 0x02, 0x43, 0x01, 0x0f, 0xfe, 0x0d, 0x47, 0x65, 0x74, 0x44, 0x65, 0x76, 0x69, 0x63, 0x65, 0x4e, 0x61, 0x6d, 0x65 });
                port.Write(cmd, 0, cmd.Length);
            //    Task.Delay(500);

                port.ErrorReceived += Port_ErrorReceived1;

                int attempt = 1000;
                var buf = new byte[256];
                var readpos = 0;
                while (readpos < 10 && attempt-- > 0)
                    readpos += port.Read(buf, readpos, 256 - readpos);

      //          var buf = new byte[port.BytesToRead];
            //    var r = port.BytesToRead > 0 ? port.Read(buf, 0, buf.Length) : -1;
                Runtime.Send(MSG.Terminal, 0, 0, "Получено " + readpos + " байт." + string.Join(" ", buf[0..readpos].Select(n=>n.ToString("X2"))));
                port.Close();
            }
            catch (Exception ex)
            {
                Runtime.Send(MSG.ErrorMessage, 0, 0, ex);
            }
            return;
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
