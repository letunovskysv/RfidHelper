using SmartMinex.Runtime;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SmartMinex.Web
{
    public class Dispatcher
    {
        static volatile int _count;
        readonly IRuntime _rtm;
        readonly ConcurrentDictionary<long, Envelope> _queue = new();

        public Dispatcher(IRuntime runtime)
        {
            _rtm = runtime;
        }

        public async Task<RfidTag[]> ReadTagsAsync()
        {
            var seqid = _count++;
            _rtm.Send(MSG.ReadTags, seqid);
            var tkn = new Envelope(3000);
            _queue.TryAdd(seqid, tkn);
            while (tkn.Next) await Task.Delay(50);
            if (tkn.Data is RfidTag[] result)
                return result;

            return null;
        }

        public async Task OnMessageReceivedAsync(TMessage m) => await Task.Run(() =>
        {
            if (m.Msg == MSG.ReadTagsData && _queue.TryRemove(m.LParam, out var tkn))
                tkn.Receive(m.Data);
        });
    }

    class Envelope
    {
        readonly CancellationTokenSource _token;
        readonly Stopwatch _timer;
        readonly int _delay;
        public object Data;

        public bool Next => !_token.IsCancellationRequested && _timer.ElapsedMilliseconds <= _delay;

        public Envelope(int delay)
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
}
