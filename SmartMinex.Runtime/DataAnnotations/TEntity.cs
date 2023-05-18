//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TEntity – Базоовые классы метаданных.
//--------------------------------------------------------------------------------------------------
#pragma warning disable CS8618
namespace SmartMinex.DataAnnotations
{
    #region Using
    using System;
    #endregion Using

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public abstract class TEntity : Attribute
    {
        /// <summary> Уникальный 64-разрядный идентификатор в Системе.</summary>
        [TColumn("Идентификатор", IsKey = true)]
        public long Id { get; set; }
        /// <summary> Родитель, тип.</summary>
        [TColumn("Родитель")]
        public long Parent { get; set; }
        /// <summary> Родитель, тип.</summary>
        /// <remarks> TType </remarks>
        [TColumn("Тип")]
        public long Type { get; set; }
        /// <summary> Уникальный код объекта конфигурации.</summary>
        [TColumn("Код", Length = 32)]
        public string Code { get; set; }
        /// <summary> Наименование объекта конфигурации.</summary>
        [TColumn("Наименование", Length = 64)]
        public string Name { get; set; }
        /// <summary> Источник значения (таблица, поле, файл и т.д.).</summary>
        [TColumn("Источник", Length = 256, Nullable = true)]
        public string? Source { get; set; }
        [TColumn("Порядок")]
        public int Ordinal { get; set; } = int.MaxValue;
        /// <summary> Описание объекта конфигурации.</summary>
        [TColumn("Описание", Length = 1024, Nullable = true)]
        public string? Description { get; set; }
    }
}
#pragma warning restore CS8618
