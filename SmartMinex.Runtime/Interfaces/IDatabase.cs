//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: IDatabase –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using System.Data;
    #endregion Using

    public interface IDatabase
    {
        /// <summary> Наименование приложения.</summary>
        string? ApplicationName { get; set; }
        /// <summary> Схема по-умолчанию.</summary>
        string? DefaultScheme { get; }
        /// <summary> Возвращает наименование базы данных.</summary>
        string? DatabaseName { get; }
        /// <summary> Возвращает версию базы данных.</summary>
        string? Version { get; }

        /// <summary> Открыть соединение с базой данных.</summary>
        IDatabase Open();
        /// <summary> Закрыть соединение с базой данных.</summary>
        void Close();
        /// <summary> Возвращает таблицу из базы данных.</summary>
        DataTable Query(string statement, params object[] args);
        /// <summary> Возвращает список указанного типа с маппингом из базы данных.</summary>
        IEnumerable<T> Query<T>(string statement, params object[] args);
        /// <summary> Возвращает список указанного типа с маппингом из базы данных.</summary>
        IEnumerable<T>? Query<T>();
        /// <summary> Возвращает список указанного типа с маппингом из базы данных.</summary>
        IEnumerable<dynamic>? Query(Type type);
        /// <summary> Возвращает список указанного типа с маппингом из базы данных.</summary>
        IEnumerable<object>? Query(Type type, string statement, params object[] args);
        /// <summary> Возвращает список указанного типа с маппингом из базы данных.</summary>
        Task<IEnumerable<dynamic>?> QueryAsync(Type type);
        /// <summary> Возвращает единственное значение из БД. Первую колонку первой записи.</summary>
        object Scalar(string statement, params object[] args);
        /// <summary> Возвращает единственное значение из БД указанного типа. Первую колонку первой записи.</summary>
        T Scalar<T>(string statement, params object[] args);
        /// <summary> Выполнить инструкцию базы данных.</summary>
        void Exec(string statement, params object[] args);
        /// <summary> Вставить данные в БД.</summary>
        object? Insert(object item);
        /// <summary> Обновить данные в БД.</summary>
        object? Update(object item);
        /// <summary> Вставить или обновить данные в БД.</summary>
        object? InsertOrUpdate(object item);

        /// <summary> Возвращает список схем данных в текущей базе данных.</summary>
        List<string> Schemata();
        /// <summary> Возвращает список таблиц в текущей базе данных.</summary>
        List<DbTable> Tables();
    }

    public interface IDatabaseFactory // For isolation
    {
        /// <summary> Создаёт новую базу данных.</summary>
        void CreateDatabase(IMetadata entities, Action<string> message);
        /// <summary> Временно.</summary>
        void UpdateDatabase(IMetadata entities, Action<string> message);
    }

    /// <summary> Описание таблицы базы данных.</summary>
    public class DbTable
    {
        public string Name { get; }
        public List<DbColumn> Columns { get; }

        public DbTable(string name, List<DbColumn> columns)
        {
            Name = name;
            Columns = columns;
        }
    }

    /// <summary> Описание колонки таблицы базы данных.</summary>
    public class DbColumn
    {
        public string Name { get; }
        public string Type { get; }

        public DbColumn(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}
