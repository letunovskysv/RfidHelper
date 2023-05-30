//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidReader – Класс устройства.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid
{
    #region Using
    using SmartMinex.Data;
    using System;
    #endregion Using

    public class RfidReader : IDevice
    {
        #region Declarations

        IDeviceConnection _connection;

        #endregion Declarations

        #region Properties

        public long Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Code { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string? Descript { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion Properties

        public RfidReader()
        {
            _connection = new SerialDeviceConnection(new SerialPortSetting()
            {
                BaudRate = 38400,
                Parity = System.IO.Ports.Parity.Even,
                StopBits = System.IO.Ports.StopBits.One
            });
        }

        #region Команды операций с буферами данных и сообщений

        /// <summary> Запись данных в порт устройства. Запрос данных.</summary>
        /// <param name="address"> адрес Modbus [1 … 247] </param>
        /// <remarks>
        /// ADDR: адрес Modbus [1 … 247];<br/>
        /// 0x42:определяемая пользователем функция — операция с буфером данных;<br/>
        /// SF: код операции;<br/>
        /// DID: идентификатор буфера или очереди, старший байт первый;<br/>
        /// DATA: поле данных 0...249;<br/>
        /// CRC16: контрольная сумма ModbusRTU
        /// </remarks>
        public void Write(int address, int operation, int idBuffer)
        {
            byte[] buf = new byte[] { (byte)address, 0x42, (byte)operation, (byte)(idBuffer >> 8), (byte)idBuffer };
            _serial.Write(buf, 0, buf.Length);
            return _serial.BytesToWrite;

        }

        /// <summary> Чтение данных из порта устройства.</summary>
        /// <param name="address"> адрес Modbus [1 … 247] </param>
        /// <remarks>
        /// ADDR: адрес Modbus [1 … 247];<br/>
        /// 0x42:определяемая пользователем функция — операция с буфером данных;<br/>
        /// 0x07: код операции;<br/>
        /// DID: идентификатор буфера;<br/>
        /// N: количество запрашиваемых байт (N = 0 .. 0xFF);<br/>
        /// Варианты параметра N:<br/>
        ///     N=0 — проверка команды<br/>
        ///     N=0xFF — автоматически обрежется при обработке запроса до 0xF9<br/>
        ///     , где<br/>
        ///     N — длина поля DATA N = 0..249;<br/>
        ///     DATA — запрашиваемые данные из очереди;<br/>
        ///     Если N в запросе меньше, чем минимальный набор неделимых данных, то вернется 0;<br/>
        /// CRС16: контрольная сумма ModbusRTU<br/>
        /// </remarks>
        public void Read(int address)
        {

        }

        #endregion Команды операций с буферами данных и сообщений
    }
}
