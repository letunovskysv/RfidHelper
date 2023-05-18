//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: IMetadata – Метаданные.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    using System.Data;
    using SmartMinex.DataAnnotations;

    public interface IMetadata
    {
        TObjectCollection Entities { get; }

        /// <summary> Возвращает сведения об объекте конфигурации.</summary>
        TObject? GetObject(long id);

        /// <summary> Возвращает сведения об объекте конфигурации.</summary>
        TObject? GetObject(string id);

        /// <summary> Возвращает список указанного типа с маппингом из базы данных.</summary>
        IEnumerable<T>? GetData<T>();

        /// <summary> Возвращает данные объекта конфигурации.</summary>
        Task<IEnumerable<object>?> GetDataAsync(string id);

        /// <summary> Возвращает табличные данные объекта конфигурации.</summary>
        Task<DataTable?> GetDataTableAsync(string id);
        /// <summary> Возвращает табличные данные объекта конфигурации для списков.</summary>
        Task<DataTable?> GetReferenceData(long id);

        /// <summary> Обновляет данные объекта конфигурации.</summary>
        object? UpdateData(object? item);

        /// <summary> Возвращает новую запись для объекта конфигурации.</summary>
        object? NewItem(object? id);
    }
}