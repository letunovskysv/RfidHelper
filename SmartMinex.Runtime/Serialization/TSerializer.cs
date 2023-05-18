//--------------------------------------------------------------------------------------------------
// (C) 2023-2023 UralTehIS, LLC. UTIS Smart System Platform. Version 2.0. All rights reserved.
// Описание: TSerializer – Статический класс упаковки/распаковки данных в бинарный формат.
// 1 байт = 0 - Таблица (DataTable);
//          1 - Набор таблиц (DataSet);
//          3 - Таблица + текст.сообщение;
//          5 - Коллекция объектов;
//          10 - Объект в бинарном виде;
//        100 - Строка (string);
//        254 - Доступ закрыт;
//        255 - Ошибка выполнения в БД (SQL)
//--------------------------------------------------------------------------------------------------
namespace SmartMinex.Data
{
    #region Using
    using System.Data;
    using System.Runtime.InteropServices;
    #endregion Using

    public static class TSerializer
    {
        #region Declarations

        const byte DATATABLE = 0;
        const byte DATASET = 1;
        const byte OBJECT = 10;

        readonly static Dictionary<Type, int> m_types = new Dictionary<Type, int>() {
                { typeof(byte), 0 }, { typeof(short), 1 }, { typeof(int), 2 }, { typeof(long), 3 }, { typeof(double), 4 },
                { typeof(string), 5 },  { typeof(bool), 6 },  { typeof(DateTime), 7 },  { typeof(byte[]), 8 },  { typeof(decimal), 9 },
                { typeof(TimeSpan), 10 }
            };

        #endregion Declarations

        #region Serialization

        public static byte[] Serialize(DataSet graph)
        {
            using var ms = new TMemoryStream();
            ms.WriteByte(DATASET);
            ms.Write(graph.DataSetName);
            ms.Write(graph.Tables.Count);

            foreach (DataTable table in graph.Tables)
                SerializeDataTable(ms, table, null, false);

            return ms.ToArray();
        }

        public static byte[] Serialize(DataTable graph) =>
            Serialize(graph, false);

        public static byte[] Serialize(DataTable graph, bool changedOnly)
        {
            using var ms = new TMemoryStream();
            SerializeDataTable(ms, graph, null, changedOnly);
            return ms.ToArray();
        }

        public static byte[] Serialize(DataTable graph, string message)
        {
            if (string.IsNullOrEmpty(message))
                return Serialize(graph);

            using var ms = new TMemoryStream();
            ms.WriteByte(3);
            SerializeDataTable(ms, graph, null, false);
            ms.Write(message);
            return ms.ToArray();
        }

        public static byte[] Serialize(DataRow row)
        {
            using var ms = new TMemoryStream();
            SerializeDataTable(ms, row.Table, row, false);
            return ms.ToArray();
        }

        public static byte[] Serialize(string graph, byte type)
        {
            using var ms = new TMemoryStream();
            ms.WriteByte(type);
            ms.Write(graph);
            return ms.ToArray();
        }

        static void SerializeDataTable(TMemoryStream ms, DataTable table, DataRow rowonce, bool affectedOnly)
        {
            int colCount = table.Columns.Count;
            long rowCountPos;
            int[] coltypes = new int[colCount];
            bool[] nullable = new bool[colCount];
            bool isdeleted;

            List<int> added = new List<int>();
            List<int> modified = new List<int>();
            List<int> deleted = new List<int>();

            ms.WriteByte(DATATABLE);
            ms.Write(table.TableName);
            ms.Write(colCount);

            rowCountPos = ms.Position;
            ms.Write(new byte[4], 0, 4);

            for (int ic = 0; ic < colCount; ic++)
            {
                DataColumn col = table.Columns[ic];
                coltypes[ic] = m_types[col.DataType];
                nullable[ic] = col.AllowDBNull;

                ms.Write(col.ColumnName);
                ms.Write(col.DataType.ToString());
                if (coltypes[ic] == 3) ms.Write(col.MaxLength);
                ms.WriteByte(col.AllowDBNull ? (byte)1 : (byte)0);
            }
            int recid = 0;
            foreach (DataRow row in table.Rows)
            {
                if (rowonce == null || rowonce == row)
                {
                    isdeleted = false;
                    switch (row.RowState)
                    {
                        case DataRowState.Added:
                            added.Add(recid);
                            break;
                        case DataRowState.Modified:
                            modified.Add(recid);
                            break;
                        case DataRowState.Deleted:
                            isdeleted = true;
                            deleted.Add(recid);
                            break;
                        case DataRowState.Detached:
                            continue; // была добавлена и удалена
                        default:
                            if (affectedOnly) continue;
                            break;
                    }
                    for (int ic = 0; ic < colCount; ic++)
                    {
                        object value = isdeleted ? row[ic, DataRowVersion.Original] : row[ic];

                        if (nullable[ic])
                            if (value == DBNull.Value)
                            {
                                ms.WriteByte(1);
                                continue;
                            }
                            else ms.WriteByte(0);

                        switch (coltypes[ic])
                        {
                            case 0: ms.WriteByte((byte)value); break;
                            case 1: ms.Write((short)value); break;
                            case 2: ms.Write((int)value); break;
                            case 3: ms.Write((long)value); break;
                            case 4: ms.Write((double)value); break;
                            case 5: ms.Write(value.ToString()); break;
                            case 6: ms.Write((bool)value); break;
                            case 7: ms.Write((DateTime)value); break;
                            case 8: ms.Write((byte[])value); break;
                            case 9: ms.Write((decimal)value); break;
                            case 10: ms.Write((TimeSpan)value); break;
                        }
                    }
                    recid++;
                }
            }
            ms.Write(added.Count);
            for (int j = 0; j < added.Count; j++) ms.Write(added[j]);

            ms.Write(modified.Count);
            for (int j = 0; j < modified.Count; j++) ms.Write(modified[j]);

            ms.Write(deleted.Count);
            for (int j = 0; j < deleted.Count; j++) ms.Write(deleted[j]);

            ms.Position = rowCountPos;
            ms.Write(recid);
            ms.Position = ms.Length;
        }

        #endregion Serialization

        #region Deserialization

        static bool m_allowDBNull = false;
        public static string Message;
        public static bool AllowDBNull { get { return m_allowDBNull; } set { m_allowDBNull = value; } }

        public static object Deserialize(byte[] data)
        {
            return Deserialize(new DataTable(), data);
        }

        public static object Deserialize(DataTable table, byte[] data)
        {
            if (data != null)
            {
                Message = string.Empty;
                TMemoryStream ms = new TMemoryStream(data);
                int type = ms.ReadByte();

                if (type == DATATABLE) return DeserializeDataTable(table, ms);

                if (type == DATASET) return DeserializeDataSet(ms);

                if (type == 3)
                {
                    DataTable result = DeserializeDataTable(table, ms);
                    Message = ms.ReadString();
                    return result;
                }
                if (type == 100 || type > 200) return ms.ReadString();
            }
            return null;
        }

        public static object Deserialize(byte[] data, ref string message)
        {
            Message = null;
            object result = Deserialize(data);
            message = Message;
            return result;
        }

        static DataSet DeserializeDataSet(TMemoryStream ms)
        {
            DataSet result = new DataSet(ms.ReadString());
            int count = ms.ReadInt32();

            result.BeginInit();
            for (int i = 0; i < count; i++)
                if (ms.ReadByte() == DATATABLE)
                    result.Tables.Add(DeserializeDataTable(new DataTable(), ms));
            result.EndInit();
            return result;
        }

        public static DataTable DeserializeDataTable(DataTable table, TMemoryStream ms)
        {
            table.TableName = ms.ReadString();
            int colCount = ms.ReadInt32();
            int rowCount = ms.ReadInt32();
            int[] coltypes = new int[colCount];
            bool[] nullable = new bool[colCount];

            table.BeginInit();
            for (int i = 0; i < colCount; i++)
            {
                table.Columns.Add(new DataColumn(ms.ReadString(), Type.GetType(ms.ReadString())));

                coltypes[i] = m_types[table.Columns[i].DataType];
                if (coltypes[i] == 3) table.Columns[i].MaxLength = ms.ReadInt32();

                nullable[i] = ms.ReadByte() == 1;
                if (m_allowDBNull && !nullable[i]) table.Columns[i].AllowDBNull = false;
            }
            table.EndInit();

            table.BeginLoadData();
            object[] values = new object[colCount];
            for (int ir = 0; ir < rowCount; ir++)
            {
                for (int ic = 0; ic < colCount; ic++)
                {
                    if (nullable[ic] && ms.ReadByte() == 1)
                    {
                        values[ic] = DBNull.Value;
                        continue;
                    }
                    switch (coltypes[ic])
                    {
                        case 0: values[ic] = ms.ReadByte(); break;
                        case 1: values[ic] = ms.ReadInt16(); break;
                        case 2: values[ic] = ms.ReadInt32(); break;
                        case 3: values[ic] = ms.ReadInt64(); break;
                        case 4: values[ic] = ms.ReadDouble(); break;
                        case 5: values[ic] = ms.ReadString(); break;
                        case 6: values[ic] = ms.ReadBoolean(); break;
                        case 7: values[ic] = ms.ReadDateTime(); break;
                        case 8: values[ic] = ms.ReadBinary(); break;
                        case 9: values[ic] = ms.ReadDecimal(); break;
                        case 10: values[ic] = ms.ReadTimeSpan(); break;
                    }
                }
                table.Rows.Add(values);
            }
            table.AcceptChanges();

            int rows = ms.ReadInt32();
            for (int j = 0; j < rows; j++) table.Rows[ms.ReadInt32()].SetAdded();

            rows = ms.ReadInt32();
            for (int j = 0; j < rows; j++) table.Rows[ms.ReadInt32()].SetModified();

            rows = ms.ReadInt32();
            for (int j = 0; j < rows; j++) table.Rows[ms.ReadInt32()].Delete();

            table.EndLoadData();

            return table;
        }

        #endregion Deserialization

        #region Class deserialization

        public static byte[] SerializeStruct(object instance)
        {
            int size = Marshal.SizeOf(instance);
            byte[] result = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(instance, ptr, true);
            Marshal.Copy(ptr, result, 0, size);
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        public static T DeserializeStruct<T>(byte[] data) where T : struct
        {
            T result = new T();
            int size = Marshal.SizeOf(result);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(data, 0, ptr, size);
            result = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return result;
        }

        static T Fill<T>(this TMemoryStream ms, int[] types, bool[] nulls)
        {
            object value = default(T);
            for (int i = 0; i < types.Length; i++)
            {
                if (nulls[i] && ms.ReadByte() == 1)
                    continue;

                switch (types[i])
                {
                    case 0: value = ms.ReadByte(); break;
                    case 1: value = ms.ReadInt16(); break;
                    case 2: value = ms.ReadInt32(); break;
                    case 3: value = ms.ReadInt64(); break;
                    case 4: value = ms.ReadDouble(); break;
                    case 5: value = ms.ReadString(); break;
                    case 6: value = ms.ReadBoolean(); break;
                    case 7: value = ms.ReadDateTime(); break;
                    case 8: value = ms.ReadBinary(); break;
                    case 9: value = ms.ReadDecimal(); break;
                    case 10: value = ms.ReadTimeSpan(); break;
                }
            }
            return (T)value;
        }

        #endregion Class deserialization

        public static byte[] SerializeCollection(object collection)
        {
            return null;
        }
    }
}