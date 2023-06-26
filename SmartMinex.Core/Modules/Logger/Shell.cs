//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: Shell – Выполнение внешней консольной утилиты ОС.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion Using

    /// <summary> Выполнение внешней консольной утилиты ОС.</summary>
    public class Shell : IDisposable
    {
        #region Declarations

        Process _process;

        public string CommandString =>
            string.Concat(_process.StartInfo.FileName, " ", _process.StartInfo.Arguments);

        public Action<string> OutputData;

        //
        // Сводка:
        //     Occurs when a process exits.
        public Action Exited;

        public bool IsClosed => _process.HasExited;

        #endregion Declarations

        /// <summary> Выполнение внешней консольной утилиты ОС.</summary>
        public Shell(string filename, params object[] args)
        {
            _process = new Process();
            _process.StartInfo.FileName = filename;
            _process.StartInfo.Arguments = string.Join(" ", args);
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardInput = true;
            _process.EnableRaisingEvents = true;
        }

        /// <summary> Выполнить и ждать окончание работы.</summary>
        public void RunWaitForExit()
        {
            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);
            _process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    outputWaitHandle.Set();
                else
                    OutputData?.Invoke(e.Data);
            };
            _process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    errorWaitHandle.Set();
                else
                    OutputData?.Invoke(e.Data);
            };
            _process.Start();

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _process.WaitForExit();
            outputWaitHandle.WaitOne();
            errorWaitHandle.WaitOne();

            _process.Close();
        }

        /// <summary> Выполнить и ждать окончание работы.</summary>
        public async Task RunWaitForExitAsync(CancellationToken cancellationToken = default)
        {
            using var outputWaitHandle = new AutoResetEvent(false);
            using var errorWaitHandle = new AutoResetEvent(false);
            _process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    outputWaitHandle.Set();
                else
                    OutputData?.Invoke(e.Data);
            };
            _process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    errorWaitHandle.Set();
                else
                    OutputData?.Invoke(e.Data);
            };

            _process.Start();

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            cancellationToken.Register(() => Kill());

            await _process.WaitForExitAsync(cancellationToken);

            outputWaitHandle.WaitOne();
            errorWaitHandle.WaitOne();

            _process.Close();

            Exited?.Invoke();
        }

        public void Kill()
        {
            _process?.Kill();
        }

        public void Dispose()
        {
            _process.Dispose();
            _process = null;
            GC.SuppressFinalize(this);
        }

        /// <summary> Проверка существования папки на диске и создание при необходимости.</summary>
        public static void UseDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static async Task Run(string commandLine)
        {
            var proc = new Process();
            proc.StartInfo.FileName = commandLine;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.EnableRaisingEvents = false;
            proc.Start();
        }
    }
}
