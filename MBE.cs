using DSCS_MBE_Tool;
using DSCSTools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
{
    
}

namespace DSCSTools
{
    public class MBE : EXPA
    {
        // Change the accessibility of the Header struct to public to match the property accessibility  
        public struct Header
        {
            public uint MagicValue;
            public uint Count;
        }

        public class MbeFile
        {
            public Type? StructureType { get; set; }
            public EXPAHeader ExpaHeader { get; set; }

            public List<EXPATable> ExpaTables { get; set; } = new List<EXPATable>();
            public CHNKHeader ChnkHeader { get; set; }
            public List<MbeDataTable>? Tables { get; set; }
        }

        public class MbeDataTable
        {
            required
            public string Name { get; set; }
            public List<IMBEClass> Entries { get; set; } = new List<IMBEClass>();
        }

        public class  MbeReader 
        {
            private byte[] _data;
            private uint _offset = 0;
            private string _source;

            public MbeReader(byte[] data)
            {
                _data = data;
            }

            public MbeReader(string source)
            {
                _source = source;
                _data = File.ReadAllBytes(source);
            }

            public MbeFile Read()
            {
                if (string.IsNullOrEmpty(_source))
                {
                    throw new ArgumentException("Source file path cannot be null or empty.");
                }
                if (_data == null || _data.Length < 8)
                {
                    throw new ArgumentException("Data is null or too short to read MBE file.");
                }
                MbeFile mbeFile = new MbeFile();

                mbeFile.StructureType = GetStructureType(_source);

                mbeFile.ExpaHeader = ReadExpaHeader(_data);

                _offset += 8; 

                mbeFile.ChnkHeader = ReadChnkHeader(_data, _offset);

                _offset += 8;

                ProcessCHNKEntries(_data, ref _offset, mbeFile.ChnkHeader);

                mbeFile.Tables = PopulateMbeTable(_data, mbeFile.ExpaTables, mbeFile.StructureType);

                return mbeFile;

            }

            public MbeFile Read(string source)
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

            private static List<MbeDataTable> PopulateMbeTable(byte[] data, List<EXPATable> tables, Type structureType)
            {
                var mbeTableList = new List<MbeDataTable>();


                // Process each table
                foreach (EXPATable table in tables)
                {
                    MbeDataTable mbeTable = new MbeDataTable() { Name = table.Name() };
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

        public interface IMBEClass
        {
            // Add common properties/methods if needed  
            // (can be empty if you just need type marking)  
        }

        public class MBETable
        {
            public Dictionary<string, List<IMBEClass>> Entries { get; set; } = new();

            public List<IMBEClass> AddEmptyEntry(string name)
            {
                List<IMBEClass> list = new();
                Entries.Add(name, list);
                return list;
            }

            // Optional: Add strongly-typed retrieval method
            public List<T> GetEntries<T>(string name) where T : IMBEClass
            {
                return Entries.TryGetValue(name, out var list)
                    ? list.OfType<T>().ToList()
                    : new List<T>();
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

            public static string ToYaml(MbeFile mbeFile, string source)
            {
                
                return serializer.Serialize(mbeFile);
            }
        }

    }
}
