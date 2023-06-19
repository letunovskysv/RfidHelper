//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TagBase – Базовый тэг данных.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Data
{
    #region Using
    using System;
    using System.ComponentModel;
    #endregion Using

    public interface IDevice
    {
        /// <summary> Униальный 64-разрядный идентификатор устройства в Системе.</summary>
        long Id { get; set; }
        /// <summary> Код (шифр) устройства.</summary>
        string Code { get; set; }
        /// <summary> Наименование устройства.</summary>
        string Name { get; set; }
        /// <summary> Описание устройства (примечание, комментарий).</summary>
        string? Descript { get; set; }
        /// <summary> Состояние устройства.</summary>
        DeviceState State { get; set; }
    }

    public enum DeviceState
    {
        /// <summary> Не инициализировано.</summary>
        [Description("Не инициализировано")]
        None,
        /// <summary> Выполняется инициализация, чтение настроек.</summary>
        [Description("Инициализация")]
        Init,
        /// <summary> Устройство в состояние ожидания, пауза.</summary>
        [Description("Ожидание")]
        Wait,
        /// <summary> Устройство готово.</summary>
        [Description("Готово")]
        Ready,
        /// <summary> Ошибка устройства.</summary>
        [Description("Ошибка")]
        Fault
    }
}
