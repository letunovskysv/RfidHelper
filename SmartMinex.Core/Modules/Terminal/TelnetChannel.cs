//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TelnetChannel –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    #endregion Using

    class TelnetChannel
    {
        #region Declarations

        const byte IAC = 0XFF;
        const byte WILL = 0XFB;
        const byte WONT = 0XFC;
        const byte SE = 0XF0;
        const byte SB = 0XFA;
        const byte NOP = 0XF1;
        const byte DM = 0XF2;
        const byte BRK = 0XF3;
        const byte IP = 0XF4;
        const byte AO = 0XF5;
        const byte AYT = 0XF6;
        const byte EC = 0XF7;
        const byte EL = 0XF8;
        const byte GA = 0XF9;
        const byte DO = 0XFD;
        const byte DONT = 0XFE;

        const byte O_ECHO = 0x01;
        const byte O_SUP_GA = 0x03;
        const byte O_TGL_FC = 0x21;
        const byte O_LINEMODE = 0x22;
        const byte O_CHARSET = 0x2A;

        const byte CC_NUL = 0x00;
        const byte CC_ETX = 0x03;
        const byte CC_EOT = 0x04;
        const byte CC_BSP = 0x08;
        const byte CC_HT = 0x09;
        const byte CC_LF = 0x0A;
        const byte CC_CR = 0x0D;
        const byte CC_ESC = 0x1B;
        const byte CC_CTRL = 0x4F;
        const byte CC_CSI = 0x5B;
        const byte CC_TRM = 0x7E;
        const byte CC_DEL = 0x7F;

        const byte CC_CUU = (byte)'A';
        const byte CC_CUD = (byte)'B';
        const byte CC_CUF = (byte)'C';
        const byte CC_CUB = (byte)'D';
        const byte CC_EL = (byte)'K';

        #endregion

        #region Actions and callbacks

        public Action<string> ExecuteCommand;
        public Action BreakExecution;
        public Action CloseRequested;

        public Action<byte[]> Send;


        public Func<LinkedList<string>> RequestHistory;
        //public Func<string, LinkedList<string>> RequestCommandCompletion;

        #endregion

        #region State

        int _optionIndex = 0;
        readonly byte[] _optionBuffer = new byte[24];

        int _controlIndex = 0;
        readonly byte[] _controlBuffer = new byte[16];

        bool _dropCrLf = false;
        int _caretIndex = 0;
        int _commandIndex = 0;
        Decoder _commandDecoder;
        readonly char[] _commandBuffer = new char[512];

        Encoding _consoleEncoding;

        bool _requestedHistory = false;
        LinkedListNode<string> _requestedListNode = null;

        #endregion

        #region Handlers

        readonly Dictionary<byte, Action> _twoBytesOptionHandlers = new Dictionary<byte, Action>();
        readonly Dictionary<byte, Func<byte, bool>> _threeBytesOptionHandlers = new Dictionary<byte, Func<byte, bool>>();
        readonly Dictionary<byte, Func<byte[], bool>> _subnOptionHandlers = new Dictionary<byte, Func<byte[], bool>>();

        #endregion

        #region Public methods and props

        public bool IsEchoSuppressed { get; set; }

        public Encoding ConsoleEncoding
        {
            get => _consoleEncoding; set
            {
                _consoleEncoding = value;
                _commandDecoder = value.GetDecoder();
            }
        }

        public TelnetChannel()
        {
            #region Telnet Option Handlers

            _twoBytesOptionHandlers.Add(IAC, () => { ReceiveData(IAC); });
            _twoBytesOptionHandlers.Add(EL, () => BreakExecution?.Invoke());
            _twoBytesOptionHandlers.Add(BRK, () => BreakExecution?.Invoke());
            _twoBytesOptionHandlers.Add(NOP, () => { });
            _twoBytesOptionHandlers.Add(DM, () => { });
            _twoBytesOptionHandlers.Add(IP, () => { });
            _twoBytesOptionHandlers.Add(AO, () => { });
            _twoBytesOptionHandlers.Add(AYT, () => { });
            _twoBytesOptionHandlers.Add(EC, () => { });
            _twoBytesOptionHandlers.Add(GA, () => { });

            _threeBytesOptionHandlers.Add(WILL, b =>
            {
                if (b == O_LINEMODE)
                    Send?.Invoke(new byte[] { IAC, SB, O_LINEMODE, 1, 0, IAC, SE });
                else if (b != O_ECHO && b != O_SUP_GA)
                    Send?.Invoke(new byte[] { IAC, DONT, _optionBuffer[2] });
                return true;
            });
            _threeBytesOptionHandlers.Add(DO, b =>
            {
                if (b != O_ECHO && b != O_SUP_GA)
                    Send?.Invoke(new byte[] { IAC, WONT, _optionBuffer[2] });
                return true;
            });
            _threeBytesOptionHandlers.Add(WONT, b => { return true; });
            _threeBytesOptionHandlers.Add(DONT, b => { return true; });
            _threeBytesOptionHandlers.Add(SB, b => { return false; });

            _subnOptionHandlers.Add(O_TGL_FC, b => { return true; });
            _subnOptionHandlers.Add(O_CHARSET, b => { return true; });

            #endregion
        }

        public void Connected()
        {
            _optionIndex = 0;
            _commandIndex = 0;
            _commandDecoder.Reset();

            Send?.Invoke(new byte[] { IAC, WILL, O_ECHO });
            Send?.Invoke(new byte[] { IAC, WILL, O_SUP_GA });
        }

        public void Received(byte[] data)
        {
            foreach (byte b in data)
                if (!ReceiveOption(b))
                    if (!ReceiveControl(b))
                        ReceiveData(b);
        }

        public void SendString(string s)
        {
            SendData(Encoding.Convert(Encoding.Default, ConsoleEncoding, Encoding.Default.GetBytes(s)));
        }

        #endregion

        #region Send

        public void SendData(byte[] data)
        {
            Send?.Invoke(EscapeTelnetIAC(data));
        }

        void SendBack(byte[] data)
        {
            if (IsEchoSuppressed)
                return;

            SendData(data);
        }
        void SendBack(char[] data)
        {
            SendBack(Encoding.Convert(Encoding.Default, ConsoleEncoding, Encoding.Default.GetBytes(data)));
        }


        void SendBackCSI(byte cc, byte[] pars = null)
        {
            SendBack(new byte[] { CC_ESC, CC_CSI });
            if (pars?.Length > 0)
                SendBack(pars);
            SendBack(new byte[] { cc });
        }

        void SendBackCSI(byte cc, int par)
        {
            SendBackCSI(cc, Encoding.ASCII.GetBytes(par.ToString()));
        }

        void SendBackRestOfData()
        {
            SendBackCSI(CC_EL);

            if (_caretIndex != _commandIndex)
            {
                SendBack(_commandBuffer.Skip(_caretIndex).Take(_commandIndex - _caretIndex).ToArray());
                SendBackCSI(CC_CUB, _commandIndex - _caretIndex);
            }
        }

        void SendBackNewData()
        {
            if (_caretIndex > 0)
                SendBackCSI(CC_CUB, _caretIndex);
            SendBackCSI(CC_EL);
            if (_commandIndex > 0)
                SendBack(_commandBuffer.Take(_commandIndex).ToArray());
            _caretIndex = _commandIndex;
        }

        static byte[] EscapeTelnetIAC(byte[] input)
        {
            if (input == null)
                return input;

            var escaped = new byte[input.Length * 2];

            var clen = 0;
            foreach (byte b in input)
            {
                escaped[clen++] = b;

                if (b == IAC)
                    escaped[clen++] = b;
            }

            Array.Resize(ref escaped, clen);
            return escaped;

            // Или вот так, но
            //
            //var ix = Array.FindIndex(input, b => b == IAC) + 1;
            //if (ix == 0)
            //  return input;

            //return input.Take(ix).Concat(new byte[] { IAC }).Concat(EscapeTelnetIAC(input.Skip(ix).ToArray())).ToArray();
        }

        #endregion

        #region Receive

        void ReceiveCommand(char c)
        {
            if (_caretIndex != _commandIndex)
            {
                for (int ix = _commandIndex; ix > _caretIndex; --ix)
                    _commandBuffer[ix] = _commandBuffer[ix - 1];
            }

            _commandBuffer[_caretIndex++] = c;

            if (++_commandIndex == _commandBuffer.Length)
            {
                _caretIndex = 0;
                _commandIndex = 0;
                throw new OverflowException("Длина команды превышает максимально допустимую");
            }

            SendBack(new[] { c });

            if (_caretIndex != _commandIndex)
            {
                SendBackCSI(CC_EL);
                SendBack(_commandBuffer.Skip(_caretIndex).Take(_commandIndex - _caretIndex).ToArray());
                SendBackCSI(CC_CUB, _commandIndex - _caretIndex);
            }
        }

        void ReceiveData(byte b)
        {
            var chars = new char[1];
            if (_commandDecoder.GetChars(new[] { b }, 0, 1, chars, 0, false) > 0)
                ReceiveCommand(chars[0]);
        }

        bool ReceiveControl(byte b)
        {
            var sendBack = false;
            var endOfControl = false;
            var controlReceived = true;
            var resetRequestedListNode = true;

            if (_controlIndex > 0)
            {
                _controlBuffer[_controlIndex++] = b;
                if (_controlIndex == _controlBuffer.Length)
                {
                    _controlIndex = 0;
                    throw new OverflowException("Длина ESC-последовательности превышает максимально допустимую");
                }

                if (_controlIndex > 2 && b >= 0x40 && b < 0x80)
                {
                    endOfControl = true;
                    switch (b)
                    {
                        case CC_CUU:
                        case CC_CUD:
                            resetRequestedListNode = false;

                            if (_requestedListNode == null || !_requestedHistory)
                            {
                                var list = RequestHistory?.Invoke();
                                list?.AddLast(new string(_commandBuffer.Take(_commandIndex).ToArray()));
                                _requestedListNode = list?.Last;
                                _requestedHistory = true;
                            }

                            if (_requestedListNode != null)
                            {
                                var nextNode = b == CC_CUU ? _requestedListNode.Previous : _requestedListNode.Next;
                                if (nextNode != null)
                                {
                                    _requestedListNode = nextNode;
                                    var len = Math.Min(_commandBuffer.Length - 1, _requestedListNode.Value.Length);
                                    _requestedListNode.Value.CopyTo(0, _commandBuffer, 0, len);
                                    _commandIndex = len;

                                    SendBackNewData();
                                }
                            }

                            break;

                        case CC_CUF:
                            if (_caretIndex != _commandIndex)
                                if (_controlBuffer[1] == CC_CSI)
                                {
                                    ++_caretIndex;
                                    sendBack = true;
                                }
                                else
                                {
                                    var nci = _caretIndex + 1;
                                    while (nci < _commandIndex && _commandBuffer[nci] != 0x20)
                                        ++nci;

                                    SendBackCSI(CC_CUF, nci - _caretIndex);
                                    _caretIndex = nci;
                                }
                            break;

                        case CC_CUB:
                            if (_caretIndex != 0)
                                if (_controlBuffer[1] == CC_CSI)
                                {
                                    --_caretIndex;
                                    sendBack = true;
                                }
                                else
                                {
                                    var nci = _caretIndex - 1;
                                    while (nci > 0 && _commandBuffer[nci] != 0x20)
                                        --nci;

                                    SendBackCSI(CC_CUB, _caretIndex - nci);
                                    _caretIndex = nci;
                                }
                            break;

                        case CC_TRM:
                            if (_controlIndex == 4)
                                switch (_controlBuffer[2])
                                {
                                    case 0x31:          // Home
                                        if (_caretIndex != 0)
                                        {
                                            SendBackCSI(CC_CUB, _caretIndex);
                                            _caretIndex = 0;
                                        }
                                        break;

                                    case 0x34:          // End
                                        if (_caretIndex != _commandIndex)
                                        {
                                            SendBackCSI(CC_CUF, _commandIndex - _caretIndex);
                                            _caretIndex = _commandIndex;
                                        }
                                        break;

                                    case 0x32:          // Insert
                                        break;

                                    case 0x33:          // Delete
                                        if (_caretIndex != _commandIndex)
                                        {
                                            for (int ix = _caretIndex; ix < _commandIndex - 1; ++ix)
                                                _commandBuffer[ix] = _commandBuffer[ix + 1];

                                            --_commandIndex;

                                            SendBackRestOfData();
                                        }
                                        break;

                                    default:
                                        sendBack = true;
                                        break;
                                }
                            break;

                        default:
                            break;
                    }

                }
                else if (_controlIndex == 2 && b != CC_CSI && b != CC_CTRL)
                    endOfControl = true;
            }
            else
            {
                endOfControl = true;
                switch (b)
                {
                    case 0:
                        break;

                    case CC_BSP:
                    case CC_DEL:
                        if (_caretIndex > 0)
                        {
                            if (_commandIndex != _caretIndex)
                                for (int ix = _caretIndex - 1; ix < _commandIndex - 1; ++ix)
                                    _commandBuffer[ix] = _commandBuffer[ix + 1];

                            --_caretIndex;
                            --_commandIndex;

                            SendBack(new[] { b });
                            SendBackRestOfData();
                        }
                        break;

                    case CC_LF:
                    case CC_CR:
                        if (!(_dropCrLf = !_dropCrLf))
                            break;

                        SendBack(new byte[] { 0x0D, 0x0A });

                        ExecuteCommand.Invoke(new string(_commandBuffer.Take(_commandIndex).ToArray()));
                        _commandIndex = 0;
                        _caretIndex = 0;
                        break;

                    case CC_HT:

                        break;

                    case CC_ETX:
                        BreakExecution?.Invoke();
                        break;

                    case CC_EOT:
                        CloseRequested?.Invoke();
                        break;

                    case CC_ESC:
                        endOfControl = false;
                        _controlBuffer[_controlIndex++] = b;
                        break;

                    default:
                        _dropCrLf = false;
                        controlReceived = false;
                        break;
                }
            }

            if (endOfControl)
            {
                if (sendBack)
                    SendBack(_controlBuffer.Take(_controlIndex).ToArray());
                _controlIndex = 0;

                if (resetRequestedListNode)
                    _requestedListNode = null;
            }

            return !endOfControl || controlReceived;
        }

        bool ReceiveOption(byte b)
        {
            if (_optionIndex == 0 && b != IAC)
                return false;

            _optionBuffer[_optionIndex++] = b;

            if (_optionIndex == 2)
            {
                if (_twoBytesOptionHandlers.TryGetValue(_optionBuffer[1], out var handler))
                {
                    handler?.Invoke();
                    _optionIndex = 0;
                }
            }
            else if (_optionIndex == 3)
            {
                if (_threeBytesOptionHandlers.TryGetValue(_optionBuffer[1], out var handler) && handler?.Invoke(_optionBuffer[2]) != false)
                    _optionIndex = 0;
            }
            else if (_optionIndex > 3 && _optionBuffer[_optionIndex - 2] == IAC && _optionBuffer[_optionIndex - 1] == SE)
            {
                if (_subnOptionHandlers.TryGetValue(_optionBuffer[2], out var handler))
                    handler.Invoke(_optionBuffer.Skip(3).Take(_optionIndex - 5).ToArray());

                _optionIndex = 0;
            }


            if (_optionIndex == _optionBuffer.Length)
            {
                _optionIndex = 0;
                throw new FormatException("Ошибка обработки потока согласования опций Telnet");
            }

            return true;
        }

        #endregion
    }
}
