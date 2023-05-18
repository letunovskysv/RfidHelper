//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TAttributes – Классы реквизитов метаданных.
//--------------------------------------------------------------------------------------------------
#pragma warning disable CS8618
namespace SmartMinex.DataAnnotations
{
    #region Using
    using System;
    using SmartMinex.Data;
    #endregion Using

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    [TObject("Реквизиты объекта конфигурации", "config.attributes", Ordinal = 2)]
    public sealed class TColumn : TEntity
    {
        #region Properties

        [TColumn("Длинна")]
        public int Length { get; set; }
        /// <summary> Признак возможности пустых значений.</summary>
        public bool Nullable { get; set; }
        /// <summary> Встроенный тип C#.</summary>
        public Type CType { get; set; }
        /// <summary> Различные флаги реквизита объекта конфигурации.</summary>
        [TColumn("Флаги")]
        public TAttributeFlags Flags { get; set; }
        /// <summary> Признак первичного ключа.</summary>
        public bool IsKey
        {
            get => (Flags & TAttributeFlags.Key) > 0;
            set => Flags = value ? Flags | TAttributeFlags.Key : Flags & (0x7FFFFFFF - TAttributeFlags.Key);
        }
        public bool IsCode => (Flags & TAttributeFlags.Code) > 0;
        public bool IsName => (Flags & TAttributeFlags.Name) > 0;
        public bool IsParent => (Flags & TAttributeFlags.Parent) > 0;
        /// <summary> Признак ссылочного типа.</summary>
        public bool IsReference => Type >= TType.TypeIterator;

        #endregion Properties

        /// <summary> Definition </summary>
        public string[]? PrimaryKey { get; set; }
        /// <summary> Definition </summary>
        public string[]? Indexes { get; set; }
        /// <summary> Ссылочная таблица.</summary>
        public string? Binding { get; set; }
        /// <summary> Наименование поля БД.</summary>
        public string Field => string.Concat('"', (Source ?? Code).ToLower(), '"');
        /// <summary> Наименование поля БД для отображения.</summary>
        public string DisplayField => Type > 10000000 ? string.Concat('"', Code.ToLower(), "_view", '"') : Field;
        /// <summary> Значение поля по умолчанию.</summary>
        public object? DefaultValue { get; set; }
        /// <summary> Видимость поля по умолчанию в клиенте.</summary>
        [TColumn("Видимость по умолчанию")]
        public bool Visible { get; set; } = true;

        public TColumn()
        {
        }

        public TColumn(string name)
        {
            Name = name;
        }

        public override string ToString() =>
            $"{Code} {CType.Name}";
    }

    public sealed class TAttributeCollection : List<TColumn>, ICloneable
    {
        public TColumn? IdField => this.FirstOrDefault(a => a.IsKey);
        public TColumn? CodeField => this.FirstOrDefault(a => a.IsCode);
        public string ViewField => this.FirstOrDefault(a => a.IsName)?.Field ?? "\"name\"";

        public bool TryGetAttribute(string name, out TColumn? attribute)
        {
            attribute = this.FirstOrDefault(ai => ai.Code.ToUpper() == name.ToUpper());
            return attribute != null;
        }

        public object Clone()
        {
            var res = new TAttributeCollection();
            res.AddRange(this);
            return res;
        }
    }

    [Flags]
    public enum TAttributeFlags
    {
        None = 0,
        Key = 1,
        Parent = 2,
        Dimension = 4,
        Code = 8,
        Name = 16,
        /// <summary> Признак обязательности поля.</summary>
        Required = 32
    }
}
#pragma warning restore CS8618
