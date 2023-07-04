//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: Различные ресурсы.
//--------------------------------------------------------------------------------------------------
using System.Reflection;

namespace SmartMinex.Web
{
    public class Resources
    {
        public static Version Version { get; } = Assembly.GetEntryAssembly().GetName().Version;
        public static string Title { get; } = "Блок опроса меток " + Version.ToString(2);
    }
}
