//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidAnchor – Класс устройства.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Rfid
{
    #region Using
    using System;
    using SmartMinex.Data;
    #endregion Using

    public class RfidAnchor : IDevice
    {
        #region Declarations


        #endregion Declarations

        #region Properties

        public long Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string? Descript { get; set; }

        public byte Address { get; }
        public string Uid { get; set; }
        public string Serial { get; set; }
        public string HW { get; set; }
        public string AppName { get; set; }
        public string AppType { get; set; }
        public string AppVersion { get; set; }
        public string AppGitHash { get; set; }
        public string AppGitTick { get; set; }
        public string AppGitStamp { get; set; }
        public string AppGitTag { get; set; }

        public string BootVersion { get; set; }

        public string TagAnqVpl { get; set; }
        public string SrvAnqVpl { get; set; }
        public string ModbusVersion { get; set; }
        public string AnchorCRC { get; set; }

        public DateTime? Started { get; set; }
        public int? OperatingTimeStarted { get; set; }
        public int? OperatingTimeGeneral { get; set; }

        #endregion Properties

        public RfidAnchor(int address)
        {
            Address = (byte)address;
        }
    }
}
