//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TMessage –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    /// <summary> Системное сообщение.</summary>
    [System.Diagnostics.DebuggerDisplay("Msg={MSG.ToString(Msg)}, {LParam}, {HParam}, {Data}")]
    public struct TMessage
    {
        public static TMessage Empty = new TMessage(0);

        /// <summary> Тип сообщения </summary>
        public int Msg;
        /// <summary> Параметр 1 </summary>
        public long LParam;
        /// <summary> Параметр 2 </summary>
        public long HParam;
        /// <summary> Возврат - результат </summary>
        public long Result;
        /// <summary> Произвольные данные </summary>
        public object Data;

        public TMessage(int msg)
            : this(msg, 0, 0, null) { }

        public TMessage(int msg, object data)
            : this(msg, 0, 0, data) { }

        public TMessage(int msg, long lparam, object data)
            : this(msg, lparam, 0, data) { }

        public TMessage(int msg, long lparam, long hparam, long result, object data)
            : this(msg, lparam, hparam, data) => Result = result;

        public TMessage(int msg, long lparam, long hparam, object data)
        {
            Msg = msg;
            LParam = lparam;
            HParam = hparam;
            Result = 0;
            Data = data;
        }
    }
}