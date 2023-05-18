//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: Класс потока, хранилищем которого является память с операциями Чтение/Запись.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Data
{
    #region Using
    using System;
    using System.IO;
    using System.Text;
    #endregion Using

    public sealed class TMemoryStream : Stream
    {
        #region Declarations

        static readonly Encoding _encoding = Encoding.UTF8;

        byte[] _buffer;
        byte[] _value = new byte[16];
        int _capacity = 0;
        int _length = 0;
        int _pos = 0;

        #endregion Declarations

        #region Constructor

        public TMemoryStream() : this(1024) { }

        public TMemoryStream(int capacity)
        {
            _buffer = new byte[_capacity = capacity];
        }

        public TMemoryStream(byte[] buffer)
        {
            _buffer = buffer;
            _capacity = _length = _buffer.Length;
        }

        #endregion Constructor

        public override bool CanRead { get { return false; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return true; } }

        public override long Length { get { return _length; } }

        public override long Position
        {
            get { return _pos; }
            set { _pos = (int)value; }
        }

        public override void Flush()
        { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public int Peek()
        {
            if (Position == Length - 1) return -1;
            int res = ReadByte();
            _pos--;
            return res;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        { }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_pos + count > _capacity)
                EnsureCapacity(_pos + count);

            if (count <= 8)
            {
                int i = count;
                while (--i >= 0)
                    _buffer[_pos + i] = buffer[offset + i];
            }
            else
                Buffer.BlockCopy(buffer, offset, _buffer, _pos, count);

            _pos += count;
            if (_pos > _length) _length = _pos;
        }

        void EnsureCapacity(int capacity)
        {
            byte[] newbuffer = new byte[_capacity = _capacity * 2 < capacity ? capacity : _capacity * 2];
            Buffer.BlockCopy(_buffer, 0, newbuffer, 0, _length);
            _buffer = newbuffer;
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[_length];
            Buffer.BlockCopy(_buffer, 0, result, 0, _length);
            return result;
        }

        #region Binary Writer

        void WriteLength7Bit(int value)
        {
            uint num = (uint)value;
            while (num >= 128U)
            {
                WriteByte((byte)(num | 128U));
                num >>= 7;
            }
            WriteByte((byte)num);
        }

        public override void WriteByte(byte value)
        {
            if (_pos + 1 > _capacity)
                EnsureCapacity(_pos + 1);

            _buffer[_pos++] = value;
            if (_pos > _length) _length = _pos;
        }

        public void Write(short value)
        {
            _value[0] = (byte)value;
            _value[1] = (byte)(value >> 8);
            Write(_value, 0, 2);
        }

        public void Write(int value)
        {
            _value[0] = (byte)value;
            _value[1] = (byte)(value >> 8);
            _value[2] = (byte)(value >> 16);
            _value[3] = (byte)(value >> 24);
            Write(_value, 0, 4);
        }

        public void Write(long value)
        {
            _value[0] = (byte)value;
            _value[1] = (byte)(value >> 8);
            _value[2] = (byte)(value >> 16);
            _value[3] = (byte)(value >> 24);
            _value[4] = (byte)(value >> 32);
            _value[5] = (byte)(value >> 40);
            _value[6] = (byte)(value >> 48);
            _value[7] = (byte)(value >> 56);
            Write(_value, 0, 8);
        }

        public unsafe void Write(float value)
        {
            uint _value = *(uint*)&value;
            this._value[0] = (byte)_value;
            this._value[1] = (byte)(_value >> 8);
            this._value[2] = (byte)(_value >> 16);
            this._value[3] = (byte)(_value >> 24);
            Write(this._value, 0, 4);
        }

        public unsafe void Write(double value)
        {
            ulong _value = *(ulong*)&value;
            this._value[0] = (byte)_value;
            this._value[1] = (byte)(_value >> 8);
            this._value[2] = (byte)(_value >> 16);
            this._value[3] = (byte)(_value >> 24);
            this._value[4] = (byte)(_value >> 32);
            this._value[5] = (byte)(_value >> 40);
            this._value[6] = (byte)(_value >> 48);
            this._value[7] = (byte)(_value >> 56);
            Write(this._value, 0, 8);
        }

        public void Write(decimal value)
        {
            Write(Convert.ToDouble(value));
            /*byte[] _value = new byte[16];
            int[] bits = decimal.GetBits(value);
            int lo = bits[0];
            int mid = bits[1];
            int hi = bits[2];
            int flags = bits[3];
            _value[0] = (byte)lo;
            _value[1] = (byte)(lo >> 8);
            _value[2] = (byte)(lo >> 16);
            _value[3] = (byte)(lo >> 24);
            _value[4] = (byte)mid;
            _value[5] = (byte)(mid >> 8);
            _value[6] = (byte)(mid >> 16);
            _value[7] = (byte)(mid >> 24);
            _value[8] = (byte)hi;
            _value[9] = (byte)(hi >> 8);
            _value[10] = (byte)(hi >> 16);
            _value[11] = (byte)(hi >> 24);
            _value[12] = (byte)flags;
            _value[13] = (byte)(flags >> 8);
            _value[14] = (byte)(flags >> 16);
            _value[15] = (byte)(flags >> 24);
            Write(_value);*/
        }

        public void Write(bool value)
        {
            _value[0] = (byte)(value ? 1 : 0);
            Write(_value, 0, 1);
        }

        public unsafe void Write(DateTime value)
        {
            Write(value.ToBinary());
        }

        public void Write(TimeSpan value)
        {
            Write(value.Ticks);
        }

        public void Write(string value)
        {
            if (value == null)
                Write(BitConverter.GetBytes(-1), 0, 4);
            else
            {
                var buf = _encoding.GetBytes(value);
                WriteLength7Bit(buf.Length);
                Write(buf, 0, buf.Length);
            }
        }

        void WriteUnicode(string value)
        {
            if (value == null)
                Write(BitConverter.GetBytes(-1), 0, 4);
            else
            {
                WriteLength7Bit(value.Length);
                int count = value.Length * sizeof(char);
                if (_pos + count > _capacity)
                    EnsureCapacity(_pos + count);

                Buffer.BlockCopy(value.ToCharArray(), 0, _buffer, _pos, count);
                _pos += count;
                if (_pos > _length) _length = _pos;
            }
        }

        public void Write(byte[] value)
        {
            WriteLength7Bit(value.Length);
            Write(value, 0, value.Length);
        }

        public void WriteVariable(long value)
        {
            int count = (int)Math.Ceiling(Math.Ceiling(Math.Log(value, 2d)) / 7);
            if (_pos + count > _capacity)
                EnsureCapacity(_pos + count);

            int i = 0;
            do
            {
                _buffer[_pos++] = (byte)((value & 127) | 128);
                value >>= 7;
            }
            while (++i < count);
            _buffer[_pos - 1] &= 127;
            _length += count;
        }

        #endregion Binary Writer

        #region Binary Reader

        int ReadLength7Bit()
        {
            int result = 0;
            int delta = 0;
            while (delta != 35)
            {
                byte num = _buffer[_pos++];
                result |= ((int)num & (int)sbyte.MaxValue) << delta;
                delta += 7;
                if (((int)num & 128) == 0)
                    return result;
            }
            return -1;
        }

        public override int ReadByte()
        {
            return _buffer[_pos++];
        }

        public short ReadInt16()
        {
            return (short)(_buffer[_pos++] | _buffer[_pos++] << 8);
        }

        public int ReadInt32()
        {
            return (int)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
        }

        public long ReadInt64()
        {
            uint lo = (uint)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            uint hi = (uint)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }

        public unsafe float ReadFloat()
        {
            uint value = (uint)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            return *((float*)&value);
        }

        public unsafe double ReadDouble()
        {
            uint lo = (uint)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            uint hi = (uint)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            ulong value = ((ulong)hi) << 32 | lo;
            return *((double*)&value);
        }

        public decimal ReadDecimal()
        {
            return (decimal)ReadDouble();
            /*int[] bits = new int[4];
            bits[0] = (int)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            bits[1] = (int)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            bits[2] = (int)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            bits[3] = (int)(_buffer[_pos++] | _buffer[_pos++] << 8 | _buffer[_pos++] << 16 | _buffer[_pos++] << 24);
            return new decimal(bits);*/

        }

        public bool ReadBoolean()
        {
            return _buffer[_pos++] != 0;
        }

        public DateTime ReadDateTime()
        {
            return DateTime.FromBinary(ReadInt64());
        }

        public TimeSpan ReadTimeSpan()
        {
            return TimeSpan.FromTicks(ReadInt64());
        }

        public string ReadString()
        {
            int count = ReadLength7Bit();
            if (count == 0) return string.Empty;
            int i = _pos;
            _pos += count;
            return _encoding.GetString(_buffer, i, count);
        }

        string ReadUnicode()
        {
            int size = ReadLength7Bit();
            if (size == 0) return string.Empty;
            int count = size * sizeof(char);
            char[] buffer = new char[size];
            Buffer.BlockCopy(_buffer, _pos, buffer, 0, count);
            _pos += count;
            return new string(buffer);
        }

        public byte[] ReadBinary()
        {
            int count = ReadLength7Bit();
            byte[] value = new byte[count];
            Buffer.BlockCopy(_buffer, _pos, value, 0, count);
            _pos += count;
            return value;
        }

        public long ReadVarLong()
        {
            long res = 0;
            int num;
            int n = 0;
            do
            {
                num = ReadByte();
                res += (num & 127) << n;
                n += 7;
            }
            while ((num & 128) == 128);
            return res;
        }

        public int ReadVarInt()
        {
            int res = 0;
            int num;
            int n = 0;
            do
            {
                num = ReadByte();
                res += (num & 127) << n;
                n += 7;
            }
            while ((num & 128) == 128);
            return res;
        }

        #endregion Binary Reader
    }
}
