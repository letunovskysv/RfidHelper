//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidAnchorReader – Класс опроса устройств.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid
{
    #region Using
    using SmartMinex.Data;
    using SmartMinex.Rfid.Modules;
    using SmartMinex.Runtime;
    using System;
    using System.Text;
    using System.Xml.Linq;
    using static System.Runtime.InteropServices.JavaScript.JSType;
    #endregion Using

    public class RfidAnchorReader
    {
        #region Declarations

        static readonly byte[] CMD_UID = new byte[] { 0x43, 0x01, 0x08, 0xfe, 0x06 }.Concat(Encoding.ASCII.GetBytes("GetUID")).ToArray();
        static readonly byte[] CMD_DEVICENAME = new byte[] { 0x43, 0x01, 0x0f, 0xfe, 0x0d }.Concat(Encoding.ASCII.GetBytes("GetDeviceName")).ToArray();
        static readonly byte[] CMD_HW = new byte[] { 0x43, 0x01, 0x04, 0xfe, 0x02 }.Concat(Encoding.ASCII.GetBytes("Hw")).ToArray();
        static readonly byte[] CMD_SERIAL = new byte[] { 0x43, 0x01, 0x07, 0xfe, 0x05 }.Concat(Encoding.ASCII.GetBytes("GetSN")).ToArray();
        static readonly byte[] CMD_BOOT_SW_VER = new byte[] { 0x43, 0x01, 0x0e, 0xfe, 0x0c }.Concat(Encoding.ASCII.GetBytes("GetBootSWVer")).ToArray();
        static readonly byte[] CMD_APP = new byte[] { 0x43, 0x01, 0x04, 0xfe, 0x02 }.Concat(Encoding.ASCII.GetBytes("Wh")).ToArray();
        static readonly byte[] CMD_APP_SW_TYPE = new byte[] { 0x43, 0x01, 0x0b, 0xfe, 0x09 }.Concat(Encoding.ASCII.GetBytes("GetSWType")).ToArray();
        static readonly byte[] CMD_APP_SW_VER = new byte[] { 0x43, 0x01, 0x05, 0xfe, 0x03 }.Concat(Encoding.ASCII.GetBytes("Ver")).ToArray();
        static readonly byte[] CMD_APP_GIT_HASH = new byte[] { 0x43, 0x01, 0x0c, 0xfe, 0x0a }.Concat(Encoding.ASCII.GetBytes("GetGitHash")).ToArray();
        static readonly byte[] CMD_APP_GIT_TICK = new byte[] { 0x43, 0x01, 0x0c, 0xfe, 0x0a }.Concat(Encoding.ASCII.GetBytes("GetGitTick")).ToArray();
        static readonly byte[] CMD_APP_GIT_STAMP = new byte[] { 0x43, 0x01, 0x0d, 0xfe, 0x0b }.Concat(Encoding.ASCII.GetBytes("GetGitStamp")).ToArray();
        static readonly byte[] CMD_APP_GIT_TAG = new byte[] { 0x43, 0x01, 0x0b, 0xfe, 0x09 }.Concat(Encoding.ASCII.GetBytes("GetGitTag")).ToArray();

        static readonly byte[] CMD_TAG_ANQ_VPL = new byte[] { 0x04, 0x00, 0x07, 0x00, 0x01 };
        static readonly byte[] CMD_SRV_ANQ_VPL = new byte[] { 0x04, 0x00, 0x08, 0x00, 0x01 };
        static readonly byte[] CMD_MODBUS_VER = new byte[] { 0x04, 0x00, 0x09, 0x00, 0x01 };
        static readonly byte[] CMD_ANCHOR_CRC = new byte[] { 0x04, 0x00, 0x0a, 0x00, 0x01 };

        static readonly byte[] CMD_STARTED_YEAR_MONTH = new byte[] { 0x04, 0x00, 0x0b, 0x00, 0x01 };
        static readonly byte[] CMD_STARTED_DAY_HOUR = new byte[] { 0x04, 0x00, 0x0c, 0x00, 0x01 };
        static readonly byte[] CMD_STARTED_MIN_SEC = new byte[] { 0x04, 0x00, 0x0d, 0x00, 0x01 };
        static readonly byte[] CMD_OPERTIME_STARTED = new byte[] { 0x04, 0x00, 0x0e, 0x00, 0x02 };
        static readonly byte[] CMD_OPERTIME_GENERAL = new byte[] { 0x04, 0x00, 0x10, 0x00, 0x02 };

        /// <summary> Идентификатор используемого буффера (256 байт).</summary>
        const int BufferId = 0x16;

        readonly object _syncRoot = new();
        readonly IxLogger _logger;
        IDeviceConnection _connection;

        #endregion Declarations

        #region Properties

        /// <summary> Список устройств на линии.</summary>
        public List<IDevice> Devices { get; } = new();

        public bool Connected => _connection.Connected;

        public Exception LastError { get; private set; }

        #endregion Properties

        public RfidAnchorReader(SerialPortSetting serialSetting, IxLogger logger, TDevice[] devices)
        {
            _connection = new SerialDeviceConnection(serialSetting);
            _logger = logger;
            devices.ToList().ForEach(dev => AddDevice(dev.Address));
        }

        public void AddDevice(int address) =>
            Devices.Add(new RfidAnchor(address));

        public bool Open()
        {
            try
            {
                _connection.Open();
                InitAsync().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex;
            }
            return false;
        }

        public void Close()
        {
            _connection.Close();
        }

        async Task InitAsync() => await Task.Run(() =>
        {
            lock (_syncRoot)
                Devices.ForEach(d => ReadInfo(((RfidAnchor)d).Address));
        });

        /// <summary> Возвращает наименование устройства. Проверка доступности устройства.</summary>
        public bool TryGetName(int address, out string? name)
        {
            name = GetValue(address, CMD_DEVICENAME);
            return name != null;
        }

        public RfidAnchor? ReadInfo(int address)
        {
            if (Devices.Cast<RfidAnchor>().FirstOrDefault(d => d.Address == address) is RfidAnchor dev && TryGetName(dev.Address, out var name))
            {
                dev.Name = name ?? string.Empty;
                dev.Uid = GetValue(address, CMD_UID) ?? string.Empty;
                dev.HW = GetValue(address, CMD_HW) ?? string.Empty;
                dev.Serial = GetValue(address, CMD_SERIAL) ?? string.Empty;
                dev.AppName = GetValue(address, CMD_APP) ?? string.Empty;
                dev.AppType = GetValue(address, CMD_APP_SW_TYPE) ?? string.Empty;
                dev.AppVersion = GetValue(address, CMD_APP_SW_VER) ?? string.Empty;
                dev.AppGitHash = GetValue(address, CMD_APP_GIT_HASH) ?? string.Empty;
                dev.AppGitTick = GetValue(address, CMD_APP_GIT_TICK) ?? string.Empty;
                dev.AppGitStamp = GetValue(address, CMD_APP_GIT_STAMP) ?? string.Empty;
                dev.AppGitTag = GetValue(address, CMD_APP_GIT_TAG) ?? string.Empty;
                dev.BootVersion = GetValue(address, CMD_BOOT_SW_VER) ?? string.Empty;
                dev.TagAnqVpl = GetValueHex(address, CMD_TAG_ANQ_VPL) ?? string.Empty;
                dev.SrvAnqVpl = GetValueHex(address, CMD_SRV_ANQ_VPL) ?? string.Empty;
                dev.ModbusVersion = GetValueHex(address, CMD_MODBUS_VER) ?? string.Empty;
                dev.AnchorCRC = GetValueHex(address, CMD_ANCHOR_CRC) ?? string.Empty;
                dev.OperatingTimeStarted = GetValueInt(address, CMD_OPERTIME_STARTED);
                dev.OperatingTimeGeneral = GetValueInt(address, CMD_OPERTIME_GENERAL);
                dev.RtlsMode = ReadHoldingRegister(address, 0x003a).Value + 1;

                var ym = GetValueHex(address, CMD_STARTED_YEAR_MONTH);
                var dh = GetValueHex(address, CMD_STARTED_DAY_HOUR);
                var ms = GetValueHex(address, CMD_STARTED_MIN_SEC);
                if (ym != null && dh != null && ms != null)
                    dev.Started = DateTime.TryParse(string.Concat(dh[2..4], ".", ym[4..6], ".20", ym[2..4], " ", dh[4..6], ":", ms[2..4], ":", ms[4..6]), out var dt) ? dt : null;

                dev.LastPolling = DateTime.Now;
                dev.State = DeviceState.Ready;
                return dev;
            }
            return null;
        }

        #region Команды операций с буферами данных и сообщений

        string? GetValue(int address, byte[] data)
        {
            var resp = Request(address, data);
            if (resp != null && resp.Length > 6)
                return Encoding.ASCII.GetString(resp, 6, resp[3] - 2);

            return null;
        }

        string? GetValueHex(int address, byte[] data)
        {
            var resp = Request(address, data);
            if (resp != null && resp.Length > 3)
                return string.Concat("0x", string.Concat(resp[3..].Select(n => n.ToString("X2"))));

            return null;
        }

        int? GetValueInt(int address, byte[] data)
        {
            var resp = Request(address, data);
            if (resp != null && resp.Length > 3)
                return BitConverter.ToInt32(resp[3..].Reverse().ToArray());

            return null;
        }

        /// <summary> Чтение 16-разрядного регистра Modbus.</summary>
        int? ReadHoldingRegister(int address, int start)
        {
            var resp = Request(address, new byte[] { 0x03, (byte)(start >> 8), (byte)start, 0x00, 0x01 });
            return resp == null ? null : (resp[3] << 8) + resp[4];
        }

        /// <summary> Чтение 16-разрядных регистров Modbus.</summary>
        IEnumerable<int>? ReadHoldingRegisters(int address, int start, int count)
        {
            var resp = Request(address, new byte[] { 0x03, (byte)(start >> 8), (byte)start, (byte)(count >> 8), (byte)count });
            if (resp != null)
                for (int i = 3; i < resp.Length; i += 2)
                    yield return (resp[i] << 8) + resp[i + 1];
        }

        /// <summary> Прямая запись в последовательный порт. Добавляется контрольная сумма CRC16.</summary>
        /// <remarks>
        /// ADDR: адрес Modbus [1 … 247];<br/>
        /// 0x42:определяемая пользователем функция — операция с буфером данных;<br/>
        /// SF: код операции;<br/>
        /// DID: идентификатор буфера или очереди, старший байт первый;<br/>
        /// DATA: поле данных 0...249;<br/>
        /// CRC16: контрольная сумма ModbusRTU
        /// </remarks>
        public void Send(byte[] data) => _connection.Write(Log("TX: ", CRC16(data)), 0, data.Length + 2);

        public byte[]? Request(int address, int command, int operation, byte[] data)
        {
            Send(new byte[] { (byte)address, (byte)command, (byte)operation, (byte)data.Length }.Concat(data).ToArray());
            Task.Delay(100);
            return Receive();
        }

        public byte[]? Request(int address, byte[] data)
        {
            Send(new byte[] { (byte)address }.Concat(data).ToArray());
            Task.Delay(100);
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
            var buf = Log("RX: ", _connection.Read());
            if (buf == null) return null;

            var crc = CRC16(buf[0..^2])[^2..];
            if (crc[0] != buf[^2] || crc[1] != buf[^1])
                throw new Exception("Ошибка контрольной суммы.");

            return buf[0..^2];
        }

        /// <summary> Запись данных в порт устройства. Запрос данных.</summary>
        /// <param name="address"> адрес Modbus [1 … 247] </param>
        public void Write(int address, int operation, int idBuffer, byte[] data)
        {
            var buf = new byte[] { (byte)address, 0x42, (byte)operation, (byte)(idBuffer >> 8), (byte)idBuffer }.Concat(data).ToArray();
            Send(buf);
        }

        /// <summary> Чтение данных из очереди (SF=0x07).</summary>
        public RfidTag[] ReadTagsFromBuffer()
        {
            List<RfidTag> res = new();
            int cnt = 0;
            byte sf = 0x07;
            foreach (var dev in Devices.Cast<RfidAnchor>())
            {
                do
                {
                    Send(new byte[] { dev.Address, 0x42, sf, BufferId >> 8, BufferId, 0xFF });
                    Task.Delay(50);
                    var resp = Receive();
                    if (resp != null)
                    {
                        cnt = resp.Length;
                        for (int i = 6; i < cnt;)
                            res.Add(new RfidTag(
                                (resp[i++] << 8) + resp[i++], // TagID (2 байта, big endian)
                                resp[i++],
                                resp[i++] == 0xff ? -1f : resp[i - 1] / 10f
                            ));
                    }
                    sf = 0x08;
                }
                while (cnt > 253);

                if (!AcceptTagsReaded(dev.Address))
                    throw new Exception("Ошибка подтверждения чтения меток (SF=0x06).");
            }
            return res.ToArray();
        }

        /// <summary> Подтверждение прочтения (SF=0x06).</summary>
        public bool AcceptTagsReaded(int address)
        {
            var data = new byte[] { (byte)address, 0x42, 0x06, BufferId >> 8, BufferId };
            Send(data);
            Task.Delay(50);
            var resp = Receive();
            return resp?.SequenceEqual(data) ?? false;
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

        byte[]? Log(string name, byte[]? data)
        {
            if (_logger != null)
            {
                _logger.Write(name);
                if (data == null)
                    _logger.WriteLine("<Нет данных>");
                else
                    _logger.WriteLine(string.Join(' ', data.Select(n => n.ToString("X2"))));
            }
            return data;
        }

        #endregion Команды операций с буферами данных и сообщений
    }
}
