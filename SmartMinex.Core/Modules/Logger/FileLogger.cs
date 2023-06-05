//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: FileLogger – Ведение текстового лог-файла.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    #endregion Using

    public sealed class FileLogger : ILogger
    {
        #region Declarations

        const string DateTimeFormat = "yyyyMMdd";

        DateTime _bound_datetime = DateTime.MinValue;
        string _filename;
        bool _isdaily;
        FileStream _stream;

        #endregion Declarations

        #region Properties

        public string Filename => string.Format(_filename, _bound_datetime.ToString(DateTimeFormat));

        /// <summary> Размер файла.</summary>
        public long Length => _stream.Length;

        /// <summary> Глубина хранения логов, дней. По умолчанию 31 день.</summary>
        public int MaxArchiveDays { get; set; } = 31;

        #endregion Properties

        public FileLogger(string filename)
        {
            _isdaily = Regex.IsMatch(filename, @"\{\d+\}");
            _filename = filename;
            Check();
        }

        protected void CreateFileStream(string filename)
        {
            var idx = 2;
            var fn = Path.GetFileNameWithoutExtension(filename);
            var dir = Path.GetDirectoryName(filename);
            Shell.UseDirectory(dir);
            do
                try
                {
                    _stream = new FileStream(filename, FileMode.Append);
                }
                catch (Exception ex)
                {
                    if (ex.HResult == -2147024864) // The process cannot access the file ... because it is being used by another process.
                    {
                        filename = Path.Combine(Path.GetDirectoryName(filename),
                            string.Concat(fn, "_", idx++, ".", Path.GetExtension(filename)));
                    }
                    else throw;
                }
            while (_stream == null && idx < 100);
        }

        public void Write(params object[] args)
        {
            _write(string.Concat(args));
        }

        public void WriteLine(params object[] args)
        {
            _write(string.Join("", args) + Environment.NewLine);
            _stream.Flush();
        }

        public void WriteLineAsync(params object[] args)
        {
            _writeAsync(string.Join("", args) + Environment.NewLine);
            _stream.FlushAsync();
        }

        public void WriteTime(params object[] args)
        {
            _write(DateTime.Now.ToString("\r\ndd.MM.yy HH:mm:ss.fff ") + string.Concat(args));
        }

        public void WriteTimeAsync(params object[] args)
        {
            _writeAsync(DateTime.Now.ToString("\r\ndd.MM.yy HH:mm:ss.fff ") + string.Concat(args));
        }

        public void Flush()
        {
            _stream.Flush();
        }

        public void FlushAsync()
        {
            _stream.FlushAsync().ConfigureAwait(false);
            _stream.FlushAsync();
        }

        public void Close()
        {
            if (_stream != null)
                try
                {
                    _stream.Flush();
                    _stream.Close();
                }
                finally
                {
                    _stream = null;
                }
        }

        public static string Time(DateTime tick)
        {
            return (DateTime.Now - tick).TotalSeconds.ToString("0.000") + " sec";
        }

        void _write(string text)
        {
            Check();
            byte[] b = Encoding.Default.GetBytes(text);
            _stream.Write(b, 0, b.Length);
        }

        void _writeAsync(string text)
        {
            Check();
            byte[] b = Encoding.Default.GetBytes(text);
            _stream.WriteAsync(b, 0, b.Length).ConfigureAwait(false);
        }

        void Check()
        {
            if (DateTime.Now.Date > _bound_datetime)
            {
                _bound_datetime = DateTime.Now.Date;
                Close();
                CreateFileStream(Filename);
                DeleteOldArchivesAsync();
            }
        }

        async void DeleteOldArchivesAsync()
        {
            await Task.Run(() =>
            {
                var bound = DateTime.Now.Date.AddDays(-MaxArchiveDays);

                Directory.GetFiles(Path.GetDirectoryName(_filename), "*.log") // удаляем все логи в данной папке -->
                    .Select(f => new
                    {
                        filename = f,
                        datetime = DateTime.TryParseExact(Regex.Match(Path.GetFileNameWithoutExtension(f), @"\d+").Value,
                            DateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) ? dt : DateTime.MaxValue
                    })
                    .Where(f => f.datetime < bound)
                    .ToList().ForEach(f =>
                    {
                        try
                        {
                            File.Delete(f.filename);
                        }
                        catch { }
                    });
            });
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            throw new NotImplementedException();
    }
}