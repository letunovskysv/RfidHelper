//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TelnetSession – Терминальная клиентская сессия Telnet.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Data;
    using System.Text.RegularExpressions;
    #endregion Using

    /// <summary> [Системный модуль] Терминальная клиентская сессия Telnet.</summary>
    sealed partial class TelnetSession : TModule
    {
        #region Declarations

        const string LOGO = "***********************\r\n*     RFID Monitor    *\r\n***********************";
        const string USER_PROMPT = "USER> ";
        const string PASS_PROMPT = "PASS> ";
        const string BREAK = "BREAK";
        const string EXIT = "EXIT";
        const string NEWLINE = "\r\n";

        static int _count = 1;

        Socket _client;
        string _username;
        int _pass_attempt = 3;
        long _proccessHolder;
        readonly TelnetChannel _channel = new();

        readonly LinkedList<string> _history = new();
        /// <summary> Обоаботчики команд. Ключ команды задан регулярным выражением.</summary>
        readonly Dictionary<string, Action<StringBuilder, string, string[]>> _handlers;

        #endregion Declarations

        #region State

        enum SessionState
        {
            Initial,
            Password,
            Normal,
            Script
        }

        SessionState _state;
        SessionState State
        {
            get => _state;
            set
            {
                //if(value == SessionState.Password)
                //  Print("\x1b[D*");

                _state = value;
                _channel.IsEchoSuppressed = _state == SessionState.Password;
            }
        }

        #endregion State

        #region Constructor

        public TelnetSession(IRuntime runtime, Socket client) : base(runtime)
        {
            Subscribe = new[] { MSG.TerminalLine, MSG.Terminal,
                MSG.InformMessage, MSG.WarningMessage, MSG.ErrorMessage, MSG.CriticalMessage };

            Name = "Терминальная сессия #" + _count++ + " " + ((IPEndPoint)client.RemoteEndPoint).Address.ToString();

            _client = client;
            _channel.Send = buffer => _client?.Send(buffer);
            _channel.ExecuteCommand = ExecuteCommand;
            _channel.BreakExecution = () => ExecuteCommand(BREAK);
            _channel.CloseRequested = () => ExecuteCommand(EXIT);

            _channel.RequestHistory = () => new LinkedList<string>(_history);

            _channel.ConsoleEncoding = Encoding.UTF8;

            _handlers = new()
            {
                { "^WHO$", ShowModules },
                { "^MOD\\s*$", ShowModules },
                { "^#\\d+", DoModuleCommand },
                { "^MOD\\s*\\d+$", GetModuleProperties },
                { "^MOD\\s*\\d+", DoModuleCommand },
                { "^SYSTEMINFO$", ShowSystemInfo },
                { "^TEST$", Test },
                { "EXIT", (a, b, c) => Stop() },
                { "QUIT", (a, b, c) => Stop() }
            };
            GetCpuUsage();
        }

        #endregion Constructor

        protected override async Task ExecuteProcess()
        {
            Status = RuntimeStatus.Running;
            _channel.Connected();

            PrintLine(LOGO);
            Print(USER_PROMPT);
            while ((Status & RuntimeStatus.Loop) > 0)
                try
                {
                    if (_client.Poll(1, SelectMode.SelectRead) && _client.Available == 0)
                        break;

                    var input = Receive();
                    if (input.Length > 0)
                    {
                        _channel.Received(input);
                    }
                    else if (State == SessionState.Normal && !_esb.IsEmpty)
                    {
                        while (_esb.TryDequeue(out TMessage m))
                        {
                            if (_proccessHolder == 0 || _proccessHolder == m.LParam) // При захвате консоли всё прочее игнорируем -->
                                switch (m.Msg)
                                {
                                    case MSG.TerminalLine:
                                    case MSG.Terminal:
                                        if (m.HParam == 0 || m.HParam == ProcessId)
                                            if (m.Data is DataTable dt)
                                            {
                                                var o = new StringBuilder(NEWLINE);
                                                PrintTable(o, dt);
                                                _channel.SendString(o.Append(NEWLINE).ToString());
                                            }
                                            else if (m.Data is Dictionary<string, string> dict)
                                            {
                                                var o = new StringBuilder(NEWLINE);
                                                PrintDictionary(o, dict);
                                                _channel.SendString(o.Append(NEWLINE).ToString());
                                            }
                                            else if (m.Data is string line)
                                                _channel.SendString(m.Msg == MSG.Terminal ? line : line + NEWLINE);
                                        break;

                                    case MSG.InformMessage:
                                        {
                                            _channel.SendString(string.Concat(TColor.INFO("info"), ": ", m.Data is Exception data ? StringException(data) : m.Data?.ToString()));
                                        }
                                        break;
                                    case MSG.WarningMessage:
                                        {
                                            _channel.SendString(string.Concat(TColor.WARN("warn"), ": ", m.Data is Exception data ? StringException(data) : m.Data?.ToString()));
                                        }
                                        break;
                                    case MSG.CriticalMessage:
                                    case MSG.ErrorMessage:
                                        {
                                            _channel.SendString(string.Concat(TColor.FAIL("fail"), ": ", m.Data is Exception data ? StringException(data) : m.Data?.ToString()));
                                        }
                                        break;
                                }
                        }
                    }
                    else await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    PrintLine("ERROR: " + ex.Message + NEWLINE + ex.StackTrace);
                }

            if (_client?.Connected ?? false)
            {
                _client.Shutdown(SocketShutdown.Both);
                _client.Disconnect(false);
            }
            _client.Close(0);
            _client = null;
            await base.ExecuteProcess();

            ((SmartRuntimeService)Runtime).Modules.Remove(this);
        }

        string StringException(Exception? ex)
        {
            var res = new StringBuilder();
            var comma = string.Empty;
            while (ex != null)
            {
                res.Append(comma).Append(ex.Message).Append(NEWLINE);
                if (ex.StackTrace != null) res.Append(ex.StackTrace.Replace("\n", NEWLINE));
                ex = ex?.InnerException;
                comma = NEWLINE + " >>> ";
            }
            return res.Append(NEWLINE).ToString();
        }

        #region Private methods

        byte[] Receive()
        {
            var buf = new byte[512];
            int cnt;
            if (_client.Available == 0) return Array.Empty<byte>();

            while ((cnt = _client.Receive(buf, buf.Length, SocketFlags.None)) > 0)
                if (_client.Available == 0) break;

            if (cnt == 0) return Array.Empty<byte>();

            var res = new byte[cnt];
            Array.Copy(buf, res, cnt);
            return res;
        }

        void ExecuteCommand(string input)
        {
            input = input.Trim();
            var output = new StringBuilder();
            var prompt = NEWLINE;
            var handler = _handlers.FirstOrDefault(h => Regex.IsMatch(input, h.Key, RegexOptions.IgnoreCase));
            string cmd = handler.Key == null
                ? Regex.Match(input, @".*?(?=\s|$)").Value.ToUpper()
                : Regex.Match(input, handler.Key, RegexOptions.IgnoreCase).Value.ToUpper();

            string[] args = input[cmd.Length..].SplitArguments();
            bool isloop = false;
            bool handled = true;

            switch (State)
            {
                case SessionState.Initial:
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        _username = input;
                        prompt = PASS_PROMPT;
                        State = SessionState.Password;
                    }
                    else prompt = USER_PROMPT;
                    break;

                case SessionState.Password:
#if !DEBUG
                    var authenticated = ValidateUser(_username, input);
#else
                    var authenticated = true;
#endif
                    if (!authenticated)
                    {
                        if (--_pass_attempt == 0)
                            Stop();

                        prompt = "Не верное имя пользователя или пароль!" + NEWLINE + PASS_PROMPT;
                    }
                    else
                    {
                        State = SessionState.Normal;
                        output.Append("OK");
                    }
                    break;

                default:
                    if (string.IsNullOrWhiteSpace(input)) return;

                    StoreHistoryCommand(input);
                    if (handler.Value == null)
                        Runtime.Send(MSG.ConsoleCommand, ProcessId, 0, input.Trim().SplitArguments());
                    else
                        handler.Value?.Invoke(output, cmd, args.ToArray());
                    break;
            }

            void StoreHistoryCommand(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return;

                _history.Remove(input);
                _history.AddLast(input);
            }

            if (State == SessionState.Normal)
            {
                if (!handled)
                    output.Append($"\"{input}\" не является внутренней или внешней командой.");
                if (!isloop)
                    output.Append(prompt).Append("\r\n> ");

                Print(output.ToString());
            }
            else
                Print(prompt);
        }

        /// <summary> Стандартный консольный вывод.</summary>
        void Print(string prompt) =>
            _channel.SendString(prompt);

        /// <summary> Стандартный консольный вывод.</summary>
        void PrintLine(string prompt) =>
            Print(prompt + NEWLINE);

        static string CellValue(object val) => val == DBNull.Value ? "\x2500" : val is long xref ? xref.ToString("X") : val is bool xbool ? xbool ? "1" : "0" : val.ToString();

        static void PrintTable(StringBuilder output, DataTable data)
        {
            string line = string.Empty, endline = string.Empty;
            var w = new int[data.Columns.Count];
            foreach (DataColumn c in data.Columns)
                foreach (DataRow r in data.Rows)
                    w[c.Ordinal] = Math.Max(w[c.Ordinal], CellValue(r[c.Ordinal]).Length);

            foreach (DataColumn c in data.Columns)
            {
                bool islast = data.Columns.Count == c.Ordinal + 1;
                line += new string('\x2500', w[c.Ordinal] + 2) + (islast ? "\x2524" : "\x253C");
                endline += new string('\x2500', w[c.Ordinal] + 2) + (islast ? "\x2518" : "\x2534");
                output.Append(" " + c.ColumnName.PadRight(w[c.Ordinal] + 1)[..(w[c.Ordinal] + 1)]);
                output.Append("\x2502");
            }
            output.Append(NEWLINE).Append(line).Append(NEWLINE);
            foreach (DataRow row in data.Rows)
            {
                output.Append(' ');
                output.Append(string.Join(" \x2502 ", data.Columns.Cast<DataColumn>()
                    .Select(c => CellValue(row[c.Ordinal]).PadRight(w[c.Ordinal]))));

                output.Append(" \x2502").Append(NEWLINE);
            }
            output.Append(endline);
        }

        bool ValidateUser(string username, string password)
        {
            if (username == "admin" && password == "4rfvgy7") return true; // TODO: Временное решение

            return false;
        }

        static void PrintDictionary(StringBuilder output, Dictionary<string, string> data, string separator = " | ")
        {
            var w = new int[2];
            foreach (var c in data)
            {
                w[0] = Math.Max(w[0], c.Key.Length);
                w[1] = Math.Max(w[1], c.Value.Length);
            }
            foreach (var c in data)
                output.Append("  ")
                    .Append(c.Key.PadRight(w[0]))
                    .Append(separator)
                    .Append(c.Value.PadRight(w[1]))
                    .Append(NEWLINE);
        }

        void Test(StringBuilder output, string command, string[] args)
        {
            try
            {
            }
            catch (Exception ex)
            {
                output.AppendLine(TColor.FAIL("ОШИБКА: ") + ex.Message);
            }
        }

        #endregion Private methods
    }
}