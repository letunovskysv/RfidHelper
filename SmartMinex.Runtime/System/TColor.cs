//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TColor – Различные константы для системной консоли (терминала телнет).
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    /// <summary> Различные константы для системной консоли (терминала телнет).</summary>
    public static class TColor
    {
        /// <summary> Захват уникального вывода в терминал процессом LParam. Все остальные выводы игнорируются.</summary>
        public const int HOLD = 0x484F4C44;
        /// <summary> Освобождение уникального вывода в терминал.</summary>
        public const int FREE = 0x46524545;
        /// <summary> Обращение ко всем узлам.</summary>
        public const long ALL = 0xC2D1A8;

        public const string BLACK = "\x1b[30m";         // normal colors
        public const string DARKGREY = "\x1b[30m;1m";
        public const string RED = "\x1b[31;1m";
        public const string GREEN = "\x1b[32;1m";
        public const string YELLOW = "\x1b[33;1m";
        public const string BLUE = "\x1b[34;1m";
        public const string MAGENTA = "\x1b[35;1m";
        public const string CYAN = "\x1b[36;1m";
        public const string WHITE = "\x1b[37;1m";
        public const string WHITESMOKE = "\x1b[37;0m";
        public const string DEFAULT_BASE = "\x1b[39;49m";
        public const string DEFAULT = WHITESMOKE;

        public const string BACK_BLACK = "\x1B[0;40m";  // background colors
        public const string BACK_RED = "\x1B[0;41m";
        public const string BACK_GREEN = "\x1B[0;42m";
        public const string BACK_YELLOW = "\x1B[0;43m";
        public const string BACK_BLUE = "\x1B[0;44m";
        public const string BACK_MAGENTA = "\x1B[0;45m";
        public const string BACK_CYAN = "\x1B[0;46m";
        public const string BACK_WHITE = "\x1B[0;47m";

        public const string BLINK = "\x1B[5m";          // blinking
        public const string RESET = "\x1B[0m";          // reset all colors
        public const string BELL = "\b";

        /// <summary> Перемещает курсор в начало текущей строки. Позволяет изменять значения в терминале.</summary>
        public const string STARTLINE = "\x1b[2K\x1b[G";

        public static string INFO(string text) => string.Concat(CYAN, text, DEFAULT);
        public static string GOOD(string text) => string.Concat(GREEN, text, DEFAULT);
        public static string WARN(string text) => string.Concat(YELLOW, text, DEFAULT);
        public static string FAIL(string text) => string.Concat(RED, text, DEFAULT);
    }
}
