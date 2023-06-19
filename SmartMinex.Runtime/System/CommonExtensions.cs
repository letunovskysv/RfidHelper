//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TColor – Различные константы для системной консоли (терминала телнет).
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Runtime
{
    using System.ComponentModel;
    using System.Reflection;

    public static class CommonExtensions
    {
        /// <summary> Возвращает описание значения перечисления, атрибут Description.</summary>
        public static string ToDescription(this object instance)
        {
            if (instance == null) return string.Empty;

            var vals = instance.GetType().GetMember(instance.ToString());
            if (vals != null && vals.Length > 0)
            {
                var attr = vals[0].GetCustomAttributes<DescriptionAttribute>(false);
                if (attr != null && attr.Any())
                    return attr.First().Description;
            }
            return instance.ToString();
        }
    }
}
