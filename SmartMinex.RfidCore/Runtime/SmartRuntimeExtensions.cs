//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: SmartRuntimeExtensions –
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.RFID
{
    #region Using
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;
    using SmartMinex.Runtime;
    #endregion Using

    delegate void ProcessMessageEventHandler(ref TMessage m);

    public static class SmartRuntimeExtensions
    {
        static Type? _connType = null;
        static string? _connStr = null;

        public static IHostBuilder UseSmartSystemPlatform(this IHostBuilder hostBuilder, string connectionName)
        {
            hostBuilder.ConfigureServices(srv =>
            {
                srv.AddSingleton<DatabaseConnectionHandler>(srv => () => CreateDatabaseConnection(srv, connectionName) ?? throw new Exception("Не найдено подключение к базе данных!"));
                srv.AddHostedService<SmartRuntimeService>();
            });
            return hostBuilder;
        }

        static IDatabase? CreateDatabaseConnection(IServiceProvider services, string connectionName)
        {
            if (_connType == null && connectionName != null)
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
    }
}
