//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TType – Типы объектов метаданных.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Data
{
    #region Using
    using System.Reflection;
    #endregion Using

    /// <summary> Типы объектов метаданных.</summary>
    public struct TType : IComparable
    {
        #region Constants

        /// <summary> Конфигурация.</summary>
        public const long Solution = 1;
        /// <summary> Компоненты.</summary>
        public const long Component = 2;
        /// <summary> Объекты конфигурации (метаданных).</summary>
        public const long Object = 3;
        /// <summary> Простой тип.</summary>
        public const long SimpleType = 4;
        /// <summary> Обработчик.</summary>
        public const long Handler = 5;
        /// <summary> Приложение.</summary>
        public const long Application = 6;
        /// <summary> Модуль.</summary>
        public const long Module = 7;
        /// <summary> Настройки.</summary>
        public const long Settings = 8;
        /// <summary> Константа.</summary>
        public const long Constant = 9;
        /// <summary> Перечисление.</summary>
        public const long Enum = 10;
        /// <summary> Справочник.</summary>
        public const long Catalog = 11;
        /// <summary> Документ.</summary>
        public const long Document = 12;
        /// <summary> Журнал.</summary>
        public const long Journal = 13;
        /// <summary> Отчёт.</summary>
        public const long Report = 14;
        /// <summary> Регистр.</summary>
        public const long Register = 15;
        /// <summary> Регистр учётный.</summary>
        public const long Account = 16;
        /// <summary> Измерения для регистров.</summary>
        public const long Dimension = 17;
        /// <summary> Табличная часть, детали.</summary>
        public const long Details = 18;
        /// <summary> Запрос.</summary>
        /// <remarks> Заранее продготовленные запросы данных, представления.</remarks>
        public const long Query = 19;
        /// <summary> Реквизит.</summary>
        public const long Attribute = 20;
        /// <summary> Меню.</summary>
        public const long Menu = 21;
        /// <summary> Панели инструментов.</summary>
        public const long Tool = 22;
        /// <summary> Словарь тэгов.</summary>
        public const long Tag = 32;

        public const long RefType = 65;
        public const long Byte = 65;
        public const long Int16 = 66;
        public const long Int32 = 67;
        public const long Int64 = 68;
        public const long Float = 69;
        public const long Double = 70;
        public const long Decimal = 71;
        public const long Numeric = 72;
        public const long Char = 73;
        public const long NChar = 74;
        public const long Varchar = 75;
        public const long NVarchar = 76;
        public const long DateTime = 77;
        public const long Date = 78;
        public const long Time = 79;
        public const long Boolean = 80;
        public const long Binary = 81;
        public const long VarBinary = 82;
        public const long Guid = 83;
        public const long TimeStamp = 84;
        public const long Array = 85;
        public const long Xml = 86;
        public const long Json = 87;

        /// <summary> Маска идентификатора объекта конфигурации.</summary>
        /// <remarks> 32767 возможных объекта конфигурации.</remarks>
        public const long TypeMask = 0x7FFF000000000000;
        /// <summary> Маска идентификатора строки.</summary>
        /// <remarks> 274 877 906 942 возможных записей в таблице для каждого объекта конфигурации.</remarks>
        public const long RecordMask = 0xFFFFFFFFFFFF;
        /// <summary> Маска идентификатора узла базы данных.</summary>
        /// <remarks> 1024 возможных узла баз данных в случае распределённой БД.</remarks>
        public const long NodeMask = 0x3FF;
        /// <summary> Идентификатор новой записи.</summary>
        public const long NewId = 0xFFFFFFFFFC00;

        public const long TypeIterator = 0x1000000000000;

        #endregion Constants

        long _value;

        TType(long type)
        {
            _value = type;
        }

        public static TType Parse(string name)
        {
            return new TType((long)(typeof(TType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .FirstOrDefault(f => f.Name.StartsWith(name, StringComparison.InvariantCultureIgnoreCase))?.GetValue(null) ?? 0));
        }

        public static implicit operator TType(long t) => new TType(t);

        public static implicit operator long(TType t) => t._value;

        public static bool operator ==(TType left, long right) => left._value == right;

        public static bool operator !=(TType left, long right) => left._value != right;

        public static bool operator ==(TType left, TType right) => left._value == right._value;

        public static bool operator !=(TType left, TType right) => left._value != right._value;

        public override bool Equals(object obj) => this == (TType)obj;

        public override int GetHashCode() => HashCode.Combine(_value);

        public int CompareTo(object obj)
        {
            long value = ((TType)obj)._value;
            return _value < value ? -1 : _value == value ? 0 : 1;
        }

        public override string ToString()
        {
            long value = _value;
            return typeof(TType).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .FirstOrDefault(f => f.GetValue(null).Equals(value))?.Name
                ?? string.Concat("Unknow", _value);
        }
    }
}
