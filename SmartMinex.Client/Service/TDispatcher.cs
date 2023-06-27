﻿//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TDispatcher –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Web
{
    #region Using
    using SmartMinex.Rfid;
    using SmartMinex.Runtime;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    #endregion Using

    public class TDispatcher
    {
        static volatile int _count;
        readonly IRuntime _rtm;
        readonly ConcurrentDictionary<long, TEnvelope> _queue = new();
        internal readonly TSessionStorage Sessions = new();

        /// <summary> Период опроса меток.</summary>
        public int Interval { get; set; }
        /// <summary> Режим предстваления списка меток.</summary>
        public int ViewMode { get; set; }

        public TDispatcher(IRuntime runtime, int interval, int viewMode)
        {
            _rtm = runtime;
            Interval = interval;
            ViewMode = viewMode;
        }

        public async Task<RfidTag[]?> ReadTagsAsync()
        {
            var seqid = _count++;
            _rtm.Send(MSG.ReadTags, seqid);
            var tkn = new TEnvelope(3000);
            _queue.TryAdd(seqid, tkn);
            while (tkn.Next) await Task.Delay(50);
            if (tkn.Data is RfidTag[] result)
                return result;

            return null;
        }

        public IEnumerable<IDevice>? GetDevices()
        {
            return _rtm.GetService<IOServer>()?.Devices;
        }

        public async Task OnMessageReceivedAsync(TMessage m) => await Task.Run(() =>
        {
            if (m.Msg == MSG.ReadTagsData && _queue.TryRemove(m.LParam, out var tkn))
                tkn.Receive(m.Data);
        });
    }

    class TEnvelope
    {
        readonly CancellationTokenSource _token;
        readonly Stopwatch _timer;
        readonly int _delay;
        public object Data;

        public bool Next => !_token.IsCancellationRequested && _timer.ElapsedMilliseconds <= _delay;

        public TEnvelope(int delay)
        {
            _delay = delay;
            _token = new CancellationTokenSource();
            _timer = Stopwatch.StartNew();
        }

        public void Receive(object data)
        {
            Data = data;
            _timer.Stop();
            _token.Cancel();
        }
    }

    internal class TSession
    {
        public string Id { get; }
        public DateTime Modified { get; set; } = DateTime.Now;

        public IEnumerable<RfidTag>? TagsData;
        public int? ViewMode { get; set; }

        public TSession(string id)
        {
            Id = id;
        }
    }

    internal class TSessionStorage : ConcurrentDictionary<string, TSession>
    {
        public TSession Get(string key)
        {
            if (!TryGetValue(key, out var session))
                TryAdd(key, session = new TSession(key));

            session.Modified = DateTime.Now;
            this.Values.Where(s => (session.Modified - s.Modified).TotalMinutes > 60).ToList()
                .ForEach(s => this.TryRemove(s.Id, out _));

            return session;
        }
    }
}
