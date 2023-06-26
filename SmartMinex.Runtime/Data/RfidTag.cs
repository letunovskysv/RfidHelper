// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidTag –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    using System;

    public struct RfidTag
    {
        /// <summary> Идентификатор метки.</summary>
        public int Code { get; set; }
        /// <summary> Флаги телеметрии метки.</summary>
        public RfidTagFlags Flags { get; set; }
        /// <summary> Напряжение батареи метки.</summary>
        /// <remarks> 0x00, то это означает, что считыватель еще не получил информацию о напряжении;<br/>0xFF, то это означает, что батарея неисправна(вздулась).</remarks>
        public float Battery { get; set; }

        /// <summary> Признак неисправности аккумулятора (вздулась).</summary>
        public readonly bool BatteryFault => Battery == -1f;

        /// <summary> Признак, что считыватель еще не получил информацию о напряжении.</summary>
        public readonly bool BatteryWait => Battery == 0f;

        public RfidTag(int code, int flags, float power)
        {
            Code = code;
            Flags = (RfidTagFlags)flags;
            Battery = power;
        }

        public override string ToString() =>
            $"{Code,7},{(BatteryFault ? "неиспр." : (BatteryWait ? "???" : Battery.ToString("0.0")) + " В"),7},{Convert.ToString((int)Flags, 2).PadLeft(8, '0'),9} {(RfidTagFlags)((int)Flags & 0x80)}";
    }

    /// <summary> Флаги телеметрии метки.</summary>
    [Flags]
    public enum RfidTagFlags
    {
        None,
        /// <summary> Заряд(1) / Разряд(0) </summary>
        Charge = 0x80
    }
}
