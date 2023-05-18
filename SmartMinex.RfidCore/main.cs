//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: Служба Windows или демон Linux.
//--------------------------------------------------------------------------------------------------
using System.Reflection;
using System.Text;
using SmartMinex.Runtime;
using SmartMinex.RFID;
using System.Text.RegularExpressions;

var _connName = "datasource";
Type? _connType = null;
string? _connStr = null;

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

IDatabase? CreateDatabaseConnection(IServiceProvider services, string connectionName)
{
    if (_connType == null)
    {
        var cfg = services.GetService<IConfiguration>();
        var providers = cfg?.GetSection("runtimeOptions:providers")?.GetChildren().ToDictionary(sect => cfg[sect.Path + ":name"], sect => cfg[sect.Path + ":type"]);
        if (providers != null)
        {
            _connStr = cfg.GetSection("runtimeOptions:" + connectionName).Value;
            if (_connStr != null)
            {
                var provider = Regex.Match(_connStr, "(?<=Provider=).*?(?=;)").Value;
                if (!providers.ContainsKey(provider)) return null;
                _connStr = Regex.Replace(_connStr, @"Provider=[^;.]*;", string.Empty);
                if (!_connStr.EndsWith(";")) _connStr += ";";
                _connType = Type.GetType(providers.TryGetValue(provider, out var connstr) ? connstr : string.Empty);
            }
        }
    }
    return _connType == null ? null : (IDatabase?)Activator.CreateInstance(_connType, _connStr);
}
