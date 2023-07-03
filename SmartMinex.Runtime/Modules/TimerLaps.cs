//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TimerLaps –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    #endregion Using

    /// <summary> Таймер разбивки времени на равные промежутки.</summary>
    public sealed class TimerLaps
    {
        readonly Stopwatch _timer;
        readonly int _timeout;
        readonly CancellationToken _cancellationToken;
        long _cycle;

        /// <summary> Задержка процесса в миллисекундах с учётом затраченного на цикл времени.</summary>
        public TimerLaps(int timeout, CancellationToken cancellationToken)
        {
            _timeout = timeout;
            _timer = new Stopwatch();
            _cancellationToken = cancellationToken;
        }

        /// <summary> Задержка процесса в миллисекундах с учётом затраченного на цикл времени.</summary>
        public void DoEvents()
        {
            var delay = Math.Max(10, _timeout - (int)(_timer.ElapsedMilliseconds - _cycle));
            _timer.Stop();
            var tsk = Task.Delay(delay);
            try
            {
                tsk.Wait(_cancellationToken);
            }
            catch { }
            _cycle = _timer.ElapsedMilliseconds;
            _timer.Start();
        }
    }
}