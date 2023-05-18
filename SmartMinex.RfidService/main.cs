//--------------------------------------------------------------------------------------------------
// (C) 2017-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: Служба Windows или демон Linux.
//--------------------------------------------------------------------------------------------------
using System.Reflection;
using System.Text;
using SmartMinex.RFID;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // fix error: No data is available for encoding 1251

await Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSystemd()
    .UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
    .ConfigureAppConfiguration((ctx, cfg) =>
    {
        cfg.AddCommandLine(args, new Dictionary<string, string>());
        cfg.AddJsonFile(Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location) + ".runtimeconfig.json", true);
    })
    .ConfigureServices(srv =>
    {
        srv.AddHostedService<RfidMonitorService>();
    })
    .Build()
    .RunAsync();
