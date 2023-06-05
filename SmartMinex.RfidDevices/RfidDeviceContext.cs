//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidDeviceContext – Класс устройства.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid
{
    #region Using
    using SmartMinex.Data;
    using System;
    using System.Text;
    using static System.Runtime.InteropServices.JavaScript.JSType;
    #endregion Using

    public class RfidDeviceContext : IDevice
    {
        #region Declarations

        IDeviceConnection _connection;
        byte _address;

        #endregion Declarations

        #region Properties

        public long Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Code { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string? Descript { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        #endregion Properties

        public RfidDeviceContext(string portName, int address)
        {
            _connection = new SerialDeviceConnection(new SerialPortSetting()
            {
                Name = portName,
                BaudRate = 38400,
                Parity = System.IO.Ports.Parity.Even,
                StopBits = System.IO.Ports.StopBits.One,
                DataBits = 8
            });
            _address = (byte)address;
        }

        public void Open()
        {
            _connection.Open();
        }

        public void Close()
        {
            _connection.Close();
        }

        /// <summary> Проверка доступности устройства.</summary>
        public bool TryGetName(int address, out string? name)
        {
            var resp = Request(address, 0x43, 0x01, Encoding.ASCII.GetBytes("\xfe\x0dGetDeviceName"));
            if (resp != null && resp.Length > 6)
            {
                name = string.Join(' ', resp.Select(n => n.ToString("X2"))); // Encoding.ASCII.GetString(resp, 6, resp[3] - 2);
                return true;
            }
            name = null;
            return false;
        }

        #region Команды операций с буферами данных и сообщений

        /// <summary> Прямая запись в последовательный порт. Добавляется контрольная сумма CRC16.</summary>
        /// <remarks>
        /// ADDR: адрес Modbus [1 … 247];<br/>
        /// 0x42:определяемая пользователем функция — операция с буфером данных;<br/>
        /// SF: код операции;<br/>
        /// DID: идентификатор буфера или очереди, старший байт первый;<br/>
        /// DATA: поле данных 0...249;<br/>
        /// CRC16: контрольная сумма ModbusRTU
        /// </remarks>
        public void Send(byte[] data) => _connection.Write(CRC16(data), 0, data.Length + 2);

        public byte[]? Request(int address, int command, int operation, byte[] data)
        {
            Send(new byte[] { (byte)address, (byte)command, (byte)operation, (byte)data.Length }.Concat(data).ToArray());
            Task.Delay(1000);
            return Receive();
        }

        /// <summary> Чтение данных из порта устройства.</summary>
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
        public byte[]? Receive()
        {
            var buf = _connection.Read();
            if (buf == null) return null;

            var crc = CRC16(buf[0..^2])[^2..];
            if (crc[0] != buf[^2] || crc[1] != buf[^1])
                throw new Exception("Ошибка контрольной суммы.");

            return buf[0..^2];
        }

        /// <summary> Запись данных в порт устройства. Запрос данных.</summary>
        /// <param name="address"> адрес Modbus [1 … 247] </param>
        public void Write(int operation, int idBuffer, byte[] data)
        {
            var buf = new byte[] { _address, 0x42, (byte)operation, (byte)(idBuffer >> 8), (byte)idBuffer }.Concat(data).ToArray();
            Send(buf);
        }

        /// <summary> Чтение данных из очереди (SF=0x07).</summary>
        public RfidTag[] ReadTagsFromBuffer()
        {
            List<RfidTag>? res = null;
            int idBuffer = 0;
            int cnt = 0;
            do
            {
                Send(new byte[] { _address, 0x42, 0x07, (byte)(idBuffer >> 8), (byte)idBuffer, 0xFF }.ToArray());
                Task.Delay(50);
                var resp = Receive();
                if (resp != null)
                {
                    res = new List<RfidTag>();
                    cnt = resp.Length;
                    for (int i = 6; i < cnt;)
                        res.Add(new RfidTag(
                            (resp[i++] << 8) + resp[i++], // TagID (2 байта, big endian)
                            resp[i++],
                            resp[i++] / 10f
                        ));
                }
            }
            while (cnt > 253);
            return res?.ToArray() ?? Array.Empty<RfidTag>();
        }

        /// <summary> Добавление контрольной суммы ModbusRTU к массиву данных.</summary>
        static byte[] CRC16(byte[] data)
        {
            int crc = 0xFFFF;
            for (int pos = 0; pos < data.Length; pos++)
            {
                crc ^= data[pos];
                for (int i = 8; i != 0; i--)
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                        crc >>= 1;
            }
            return data.Concat(new byte[] { (byte)crc, (byte)(crc >> 8) }).ToArray();
        }

        #endregion Команды операций с буферами данных и сообщений
    }

    public struct RfidTag
    {
        /// <summary> Идентификатор метки.</summary>
        public int TagId;
        /// <summary> Флаги телеметрии метки.</summary>
        public RfidTagFlags Flags;
        /// <summary> Напряжение батареи метки.</summary>
        public float Battery;

        /// <summary> Батарея неисправна (вздулась).</summary>
        public bool BatteryFailed => Battery == 0xff;

        public RfidTag(int tagid, int flags, float power)
        {
            TagId = tagid;
            Flags = (RfidTagFlags)flags;
            Battery = power;
        }

        public override string ToString()
        {
            return $"{TagId}, {Battery} В";
        }
    }

    /// <summary> Флаги телеметрии метки.</summary>
    [Flags]
    public enum RfidTagFlags
    {
        None,
        /// <summary> Заряд(1) / Разряд(0) </summary>
        Charge
    }
}
