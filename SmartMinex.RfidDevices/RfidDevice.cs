//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidDevice – Класс устройства.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid
{
    #region Using
    using System;
    using SmartMinex.Data;
    #endregion Using

    public class RfidDevice : IDevice
    {
        #region Declarations


        #endregion Declarations

        #region Properties

        public long Id { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Code { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string Name { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public string? Descript { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public byte Address { get; }

        #endregion Properties

        public RfidDevice(int address)
        {
            Address = (byte)address;
        }
    }
}
