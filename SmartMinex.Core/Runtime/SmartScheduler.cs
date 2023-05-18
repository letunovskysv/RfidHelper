//--------------------------------------------------------------------------------------------------
// (C) 2017-2021 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Description: SmartScheduler – Планировщик заданий.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    #endregion Using

    public class SmartScheduler<T>
    {
        #region Declarations

        readonly Thread _thread = new((o) => ((SmartScheduler<T>)o).ScheduleThreadFunc()) { IsBackground = true };

        CancellationTokenSource _cts;

        readonly AutoResetEvent _event = new(false);
        readonly SortedDictionary<long, List<XScheduleItem>> _sch = new();

        int _itemCounter = 0;

        #endregion Declarations

        #region Properties

        public int Count => _sch.SelectMany(s => s.Value).Count();

        #endregion Properties

        #region Constructor

        public SmartScheduler() => Start();

        #endregion

        #region Public Methods

        public void Start()
        {
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();
            _thread.Start(this);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        public int? Add(T data, ScheduleParams pars)
        {
            if (GetFirstKey(pars) is not long key)
                return null;

            var item = new XScheduleItem { Id = Interlocked.Increment(ref _itemCounter), Data = data, Params = pars };

            lock (_sch)
                if (_sch.TryGetValue(key, out var list))
                    list.Add(item);
                else
                    _sch[key] = new List<XScheduleItem> { item };

            _event.Set();

            return item.Id;
        }

        public bool Modify(int id, T data, ScheduleParams pars) =>
            throw new NotImplementedException();

        public bool Remove(int id)
        {
            lock (_sch)
                if (_sch.FirstOrDefault(i => i.Value.Any(v => v.Id == id)) is { } sch && sch.Key != 0 && sch.Value != null)
                    if (sch.Value.Count > 1)
                        return sch.Value.RemoveAll(i => i.Id == id) > 0;
                    else
                        return _sch.Remove(sch.Key);
            return false;
        }

        public bool Reset(int id)
        {
            lock (_sch)
                if (_sch.FirstOrDefault(i => i.Value.Any(v => v.Id == id)) is { } sch && sch.Key != 0 && sch.Value != null)
                {
                    var item = sch.Value.First(i => i.Id == id);
                    if (sch.Value.Count > 1)
                        sch.Value.RemoveAll(i => i.Id == id);
                    else
                        _sch.Remove(sch.Key);

                    if (GetFirstKey(item.Params) is not long key)
                        throw new InvalidOperationException();

                    if (_sch.TryGetValue(key, out var list))
                        list.Add(item);
                    else
                        _sch[key] = new List<XScheduleItem> { item };

                    _event.Set();

                    return true;
                }

            return false;
        }

        #endregion Public Methods

        #region Public Events

        public event EventHandler<T> Fire;

        #endregion

        #region Private Methods

        static long? GetFirstKey(ScheduleParams pars, DateTime? since = null)
        {
            if (pars.DateTime != null)
                return pars.DateTime.Value.Ticks;

            var delay = pars.InitialDelay ?? pars.Period;
            return delay != null ? delay * TimeSpan.TicksPerMillisecond + (since ?? DateTime.Now).Ticks : null;
        }

        static long? GetNextKey(ScheduleParams pars, long prev, long bound) =>
          pars.DateTime == null && pars.Period != null
            ? prev + ((bound - prev) / (pars.Period * TimeSpan.TicksPerMillisecond) + 1) * pars.Period * TimeSpan.TicksPerMillisecond
            : null;

        void ScheduleThreadFunc()
        {
            if (_cts == null)
                return;

            var waitTimeout = -1;
            var waitHandles = new WaitHandle[] { _event, _cts.Token.WaitHandle };

            while (WaitHandle.WaitAny(waitHandles, waitTimeout) != 1)
            {
                var bound = DateTime.Now.Ticks;
                var items = new List<(long? Key, XScheduleItem Value)>();
                lock (_sch)
                {
                    items = _sch.TakeWhile(kvp => kvp.Key <= bound).SelectMany(kvp => kvp.Value.Select(v => ValueTuple.Create((long?)kvp.Key, v))).ToList();
                    items.Select(i => i.Key).Distinct().ToList().ForEach(key => _sch.Remove(key.Value));
                }

                items.ForEach(i => Fire?.Invoke(this, i.Value.Data));

                items = items.Select(i => ValueTuple.Create(GetNextKey(i.Value.Params, i.Key.Value, bound), i.Value)).Where(i => i.Item1 != null).ToList();

                if (items.Count > 0)
                    lock (_sch)
                        foreach (var (Key, Value) in items)
                            if (_sch.TryGetValue(Key.Value, out var list))
                                list.Add(Value);
                            else
                                _sch[Key.Value] = new List<XScheduleItem> { Value };

                lock (_sch)
                    waitTimeout = _sch.Count > 0 ? Math.Max((int)((_sch.First().Key - DateTime.Now.Ticks) / TimeSpan.TicksPerMillisecond), 0) : -1;
            };

        }

        #endregion

        #region Nested Types

        struct XScheduleItem
        {
            public int Id { get; init; }
            public T Data { get; init; }
            public ScheduleParams Params { get; init; }
        }

        #endregion Nested Types
    }

    public struct ScheduleParams
    {
        public long? InitialDelay { get; set; }
        public long? Period { get; set; }
        public DateTime? DateTime { get; set; }
    }
}