using CommandLine;
using DSCS_MBE_Tool;
using DSCS_MBE_Tool.Strucs;
using DSCSTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace DSCSTools.MBE
{
    public class File : EXPA
    {
        public Type? StructureType { get; set; }
        internal protected EXPAHeader ExpaHeader { get; set; }

        internal protected List<EXPATable> ExpaTables { get; set; } = new List<EXPATable>();
        internal protected CHNKHeader ChnkHeader { get; set; }
        public List<DataTable>? Tables { get; set; }
    }

    public class DataTable
    {
        required
        public string Name { get; set; }
        public List<IMBEClass> Entries { get; set; } = new List<IMBEClass>();

        public static List<TOut> ConvertList<TIn, TOut>(IEnumerable<TIn> input, Func<TIn, TOut> converter)
        {
            return input.Select(converter).ToList();
        }
    }

    public class  Reader : EXPA
    {
        private byte[] _data;
        private uint _offset = 0;
        private string _source;

        public Reader(byte[] data)
        {
            _data = data;
        }

        public Reader(string source)
        {
            _source = source;
        _data = System.IO.File.ReadAllBytes(source);
        }

        public File Read()
        {
            if (string.IsNullOrEmpty(_source))
            {
                throw new ArgumentException("Source file path cannot be null or empty.");
            }
            if (_data == null || _data.Length < 8)
            {
                throw new ArgumentException("Data is null or too short to read MBE file.");
            }
            File mbeFile = new File();

            mbeFile.StructureType = GetStructureType(_source);

            mbeFile.ExpaHeader = ReadExpaHeader(_data);

            _offset += 8;

            mbeFile.ExpaTables = ReadExpaTables(_data, ref _offset, mbeFile.ExpaHeader);

            mbeFile.ChnkHeader = ReadChnkHeader(_data, _offset);

            _offset += 8;

            ProcessCHNKEntries(_data, ref _offset, mbeFile.ChnkHeader);

            mbeFile.Tables = PopulateTable(_data, mbeFile.ExpaTables, mbeFile.StructureType);

            return mbeFile;

        }

        public File Read(string source)
        {
            _source = source;
            return Read();
        }

        private static void ProcessCHNKEntries(byte[] data, ref uint offset, CHNKHeader chunkHeader)
        {
            // Process CHNK entries
            for (uint i = 0; i < chunkHeader.NumEntry; i++)
            {
                uint dataOffset = BitConverter.ToUInt32(data, (int)offset);
                uint size = BitConverter.ToUInt32(data, (int)offset + 4);
                ulong ptr = (ulong)offset;

                Buffer.BlockCopy(BitConverter.GetBytes(ptr), 0, data, (int)dataOffset, 8);
                offset += (size + 8);
            }
        }

        private static List<DataTable> PopulateTable(byte[] data, List<EXPATable> tables, Type structureType)
        {
            var mbeTableList = new List<DataTable>();


            // Process each table
            foreach (EXPATable table in tables)
            {
                DataTable mbeTable = new DataTable() { Name = table.Name() };
                List<IMBEClass> classList = mbeTable.Entries;
                mbeTableList.Add(mbeTable);
                uint tableHeaderSize = 0x0C + table.NameSize() + Align(table.NameSize() + 4, 8);
                PropertyInfo[] formatProperties = structureType.GetProperties();

                // Read data
                for (uint i = 0; i < table.EntryCount(); i++)
                {
                    // Dynamically create an instance of the type represented by 'structureType'
                    var instance = Activator.CreateInstance(structureType) ?? throw new InvalidOperationException("Error: Unable to create an instance of the structureType type.");
                    if (instance is not IMBEClass mbeClassInstance)
                        throw new Exception($"Error: Instance of {structureType.Name} does not implement IMBEClass interface.");
                    classList.Add(mbeClassInstance);
                    int localOffset = (int)(table.Offset + i * (table.EntrySize() + Align(table.EntrySize(), 8)) + tableHeaderSize);

                    foreach (var prop in formatProperties)
                    {
                        string type = Utils.NormalizeType(prop.PropertyType);
                        string value = ReadEXPAEntry(data, ref localOffset, type);
                        switch (type)
                        {
                            case "string":
                                // Handle string type
                                prop.SetValue(instance, value); // Trim null terminators
                                break;
                            case "float":
                                // Convert string to float
                                prop.SetValue(instance, float.Parse(value, CultureInfo.InvariantCulture));
                                break;
                            case "int":
                                // Convert string to int
                                prop.SetValue(instance, int.Parse(value));
                                break;
                            case "byte":
                                // Convert string to byte
                                prop.SetValue(instance, byte.Parse(value));
                                break;
                            case "short":
                                // Convert string to short
                                prop.SetValue(instance, short.Parse(value));
                                break;
                            default:
                                prop.SetValue(instance, value);
                                break;
                        }
                    }
                }
            }
            return mbeTableList;
        }

    }


    public class Converter
    {

        private static readonly ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        private static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        public static string ToYaml(File mbeFile, string source = "", bool isPatch = true, List<string>? langOpts = null)
        {
            langOpts ??= ["jp", "en"];

            if (mbeFile == null || mbeFile.Tables == null || mbeFile.Tables.Count() == 0)
                throw new ArgumentException("Tables cannot be null or empty for patch conversion.");

            if (isPatch)
            {
                if (mbeFile.StructureType == typeof(Message))
                {
                    var mbeTable = mbeFile.Tables.ToDictionary(
                        t => t.Name,
                        t => DataTable.ConvertList(t.Entries, x => (PatchMessage)x.ToPatch(langOpts, source)
                        ));
                    return serializer.Serialize(mbeTable);

                } 
                else if(mbeFile.StructureType == typeof(Text))
                {
                    var mbeTable = mbeFile.Tables.ToDictionary(
                        t => t.Name, 
                        t => DataTable.ConvertList(t.Entries, x => (PatchText)x.ToPatch(langOpts)
                        ));
                    return serializer.Serialize(mbeTable);
                    
                } 
                else 
                    throw new NotImplementedException($"Patch conversion is not implemented for structure type {mbeFile.StructureType?.Name ?? "STRUCTURETYPE NULL"}.");

            }
            else
            {
                var dict = mbeFile.Tables.ToDictionary(t => t.Name, t => t.Entries);
                return serializer.Serialize(dict);
            }
        }
    }


}
