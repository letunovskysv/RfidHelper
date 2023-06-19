//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: RfidClientService – Веб-клиентский сервис.
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Web
{
    #region Using
    using System;
    using SmartMinex.Runtime;
    #endregion Using

    internal class RfidClientService : TModule
    {
        #region Declarations

        #endregion Declarations

        public RfidClientService(IRuntime runtime) : base(runtime)
        {
            Subscribe = new[] { MSG.ConsoleCommand };
        }

        protected override async Task ExecuteProcess()
        {
            Status = RuntimeStatus.Running;
            while (_sync.WaitOne() && (Status & RuntimeStatus.Loop) > 0)
                try
                {
                    while (_esb.TryDequeue(out TMessage m))
                    {
                        switch (m.Msg)
                        {
                            case MSG.ConsoleCommand:
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Runtime.Send(MSG.ErrorMessage, ProcessId, 0, ex);
                }

            await base.ExecuteProcess();
        }
    }
}
