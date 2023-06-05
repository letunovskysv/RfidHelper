//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: SmartLoggerService – Служба логирования.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    #endregion Using

    //sealed class SmartLoggerService : TModule//, IStartup
    //{
    //    #region Declarations

    //    readonly List<FileLogger> _loggers = new();
    //    string _logFolder => Path.Combine(Runtime.GetWorkDirectory(), "logs");

    //    #endregion Declarations

    //    #region Properties

    //    /// <summary> Глубина хранения логов, дней. По умолчанию 31 день.</summary>
    //    public int MaxArchiveDays { get; set; }

    //    #endregion Properties

    //    #region Constructor

    //    public SmartLoggerService(IRuntime runtime, int? maxArchiveDays) : base(runtime)
    //    {
    //        Name = "Служба логирования";
    //        MaxArchiveDays = maxArchiveDays ?? 31;

    //        Register("rfidmonitor");
    //    }

    //    #endregion Constructor

    //    protected override async Task ExecuteProcess()
    //    {
    //        Status = RuntimeStatus.Running;
    //        while (_sync.WaitOne() && (Status & RuntimeStatus.Loop) > 0)
    //        {
    //            while (_esb.TryDequeue(out TMessage m))
    //            {
    //                var logger = _loggers[m.HParam < _loggers.Count ? (int)m.HParam : 0];

    //                string text = string.Empty;
    //                foreach (var p in (object[])m.Data)
    //                {
    //                    if (p is string str)
    //                        text += str;
    //                    else if (p is byte[] bytes)
    //                        text += string.Join(" ", bytes.Select(b => b.ToString("X2")));
    //                }
    //                switch (m.LParam)
    //                {
    //                    case 1:
    //                        logger.Write(DateTime.Now.ToString("dd.MM.yy HH:mm:ss.fff "));
    //                        logger.WriteLine(text);
    //                        break;
    //                    default:
    //                        logger.Write(text);
    //                        break;
    //                }
    //            }
    //            _loggers.ForEach(l => l.Flush());
    //        }
    //        _loggers.ForEach(l => l.Flush());
    //        _loggers.ForEach(l => l.Close());

    //        await base.ExecuteProcess();
    //    }

    //    public ISmartLogger Register(string filePrefix)
    //    {
    //        var format = $"{filePrefix}_{{0}}.log";
    //        int logNo = _loggers.FindIndex(l => format.Equals(l.Filename));
    //        if (logNo == -1)
    //        {
    //            logNo = _loggers.Count;
    //            _loggers.Add(new FileLogger(Path.Combine(_logFolder, format)) { MaxArchiveDays = MaxArchiveDays });
    //        }
    //        return new SmartLoggerProxy { SmartLogger = this, LogNumber = logNo };
    //    }

    //    public void Write(params object[] args) => Write(0, args);

    //    public void WriteLine(params object[] args) => WriteLine(0, args);

    //    public void Write(int log, params object[] args)
    //    {
    //        _esb.Enqueue(new TMessage(MSG.Logger, 0, log, args));
    //        _sync.Set();
    //    }

    //    public void WriteLine(int log, params object[] args)
    //    {
    //        _esb.Enqueue(new TMessage(MSG.Logger, 1, log, args));
    //        _sync.Set();
    //    }
    //}
}