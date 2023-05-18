//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TagBase – Базовый тэг данных.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Data
{
    #region Using
    using System;
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
    }
}
