//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: MSG – Константы, типы сообщенй передаваемых по шине.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    /// <summary> Константы. Сообщения системной очереди сообщений.</summary>
    public static class MSG
    {
        public static string ToString(int msg) =>
            typeof(MSG).GetFields().FirstOrDefault(f => (int)f.GetValue(null) == msg)?.Name ?? msg.ToString("X4");

        /// <summary> Выполняется после окончания запуска всех служб.</summary>
        public const int StartServer = 0x0001;
        /// <summary> Выполняется при остановке объектового сервере.</summary>
        public const int StopServer = 0x0002;
        /// <summary> Установить модуль/службу в Систему.</summary>
        public const int InstallModule = 0x0003;
        /// <summary> Удалить модуль/службу из Системы.</summary>
        public const int UninstallModule = 0x0004;
        /// <summary> Команда запустить модуль/службу в Системе.</summary>
        /// <remarks> LParam = Id процесса; Data = </remarks>
        public const int Start = 0x0005;
        /// <summary> Команда остановить модуль/службу из Системе.</summary>
        /// <remarks> LParam = Id процесса; Data = </remarks>
        public const int Stop = 0x0006;
        /// <summary> Выполняется после запуска главного процеса выполнения.</summary>
        public const int RuntimeStarted = 0x0009;

        /// <summary> LParam = Id процесса.</summary>
        public const int ErrorMessage = 0x000c;
        /// <summary> LParam = Id процесса.</summary>
        public const int CriticalMessage = 0x000d;
        /// <summary> LParam = Id процесса.</summary>
        public const int WarningMessage = 0x000e;
        /// <summary> LParam = Id процесса.</summary>
        public const int InformMessage = 0x000f;

        /// <summary> Приём консольной команды.</summary>
        /// <remarks> LParam = ИД терминальной сессии; HParam = ИД процесса (модуля).</remarks>
        public const int ConsoleCommand = 0x0011;
        /// <summary> Вывод в окно терминала по протоколу <em>Telnet</em> без форматирования. HParam = ИД session.</summary>
        /// <remarks> LParam = ИД процесса (модуля); HParam = ИД терминальной сессии; Data = text/int - 0x484F4C44 HOLD, 0x46524545 FREE.</remarks>
        public const int Terminal = 0x0014;
        /// <summary> Вывод в окно терминала по протоколу <em>Telnet</em>. HParam = ИД session.</summary>
        /// <remarks> LParam = ИД процесса (модуля); HParam = ИД терминальной сессии; Data = text/int - 0x484F4C44 HOLD, 0x46524545 FREE.</remarks>
        public const int TerminalLine = 0x0015;

        /// <summary> [Команда] Чтение параметра Poll Interval.</summary>
        /// <remarks> LParam = ИД процесса (модуля) отправителя; HParam = .</remarks>
        public const int GetPollInterval = 0xa000;
        /// <summary> [Команда] Запись параметра Poll Interval.</summary>
        /// <remarks> LParam = ИД процесса (модуля) отправителя; HParam = ; Data = значение.</remarks>
        public const int SetPollInterval = 0xa001;

        /// <summary> Признак обновления данных тэгов.</summary>
        public const int TagsUpdated = 0xa002;
        /// <summary> [Команда] Чтение меток с устройства. Последний запрос.</summary>
        /// <remarks> LParam = ИД процесса (модуля); HParam = Команда (запрос).</remarks>
        public const int ReadTagsRuntime = 0xa003;
        /// <summary> [Команда] Чтение меток с устройства. Накопительный запрос.</summary>
        /// <remarks> LParam = ИД процесса (модуля); HParam = Команда (запрос).</remarks>
        public const int ReadTagsHistorian = 0xa004;

        /// <summary> [Ответ] Возвращает данные чтения меток с устройства (команда ReadTags).</summary>
        /// <remarks> LParam = ИД процесса (модуля); HParam = -1 ошибка запроса.</remarks>
        public const int ReadTagsData = 0xa005;

        /// <summary> Все сообщения.</summary>
        public const int All = 0x55555555;
    }
}