//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: IOServer – Интерфейс сервера ввода/вывода.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    #region Using
    using SmartMinex.Data;
    #endregion Using

    /// <summary> Интерфейс службы сбора данных, реестр оборудования. Сервер ввода/вывода.</summary>
    /// <remarks> В Системе должен быть только один экземпляр.</remarks>
    public interface IOServer
    {
        /// <summary> Реестр оборудования.</summary>
        /// <remarks> Возвращает копию реестра.</remarks>
        List<IDevice> Devices { get; }
    }
}
