﻿//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: SerialDeviceConnection – Класс подключения по COM-порту (RS-485).
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid
{
    #region Using
    using System.IO.Ports;
    #endregion Using

    public interface IDeviceConnection
    {
        void Open();
        void Close();
        void Write(byte[] buffer, int offset, int count);
        byte[]? Read();
        bool Connected { get; }
    }

    public struct SerialPortSetting
    {
        public string Name { get; set; }
        public int BaudRate { get; set; }
        public int DataBits { get; set; }
        public StopBits StopBits { get; set; }
        public Parity Parity { get; set; }
        public int FlowControl { get; set; }
        public string Interface { get; set; }
        public bool Fifo { get; set; }

        public override string ToString()
        {
            return $"Baud rate:{BaudRate}; Stop bits:{StopBits}";
        }
    }

    public class SerialDeviceConnection : IDeviceConnection
    {
        const int BUFSIZE = 256;
        byte[] _input = new byte[BUFSIZE];

        SerialPortSetting _setting;
        SerialPort _serial;

        public bool Connected => _serial?.IsOpen ?? false;

        public SerialDeviceConnection(SerialPortSetting setting)
        {
            _setting = setting;
        }

        /// <summary> Открыть соединение.</summary>
        public void Open()
        {
            Close();
            _serial = new SerialPort()
            {
                PortName = _setting.Name,
                BaudRate = _setting.BaudRate,
                Parity = _setting.Parity,
                DataBits = _setting.DataBits,
                StopBits = _setting.StopBits,
                Handshake = Handshake.None,
                ReadTimeout = 100,
                WriteTimeout = 100
            };
            _serial.Open();
        }

        /// <summary> Закрыть соединение.</summary>
        public void Close()
        {
            if (_serial != null)
            {
                _serial.Close();
                _serial.Dispose();
                _serial = null;
            }
        }

        public void Write(byte[] buffer, int offset, int count) =>
            _serial.Write(buffer, offset, count);

        public byte[]? Read()
        {
            var pos = 0;
            try
            {
                while (pos < BUFSIZE)
                    pos += _serial.Read(_input, pos, BUFSIZE - pos);
            }
            catch (TimeoutException)
            {
            }
            catch
            {
                throw;
            }
            return pos == 0 ? null : _input[0..pos];
        }
    }
}
