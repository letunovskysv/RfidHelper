//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// ��������: ������ Windows ��� ����� Linux.
//--------------------------------------------------------------------------------------------------
using System.Reflection;
using System.Text;
using SmartMinex.Runtime;

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
    .UseSmartSystemPlatform("datasource")
    .Build()
    .RunAsync();
