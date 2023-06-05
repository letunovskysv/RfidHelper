﻿//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: ISmartLogger –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    public interface ISmartLogger
    {
        void Write(params object[] args);
        /// <summary> В начале добавляется Дата/время.</summary>
        void WriteLine(params object[] args);
    }
}