using CommandLine;
using DSCS_MBE_Tool;
using DSCS_MBE_Tool.Strucs;
using DSCSTools;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;


namespace DSCSTools.MBE
{
    public class File : EXPA
    {
        public Type? StructureType { get; set; }
        internal protected EXPAHeader ExpaHeader { get; set; }

        internal protected List<EXPATable> ExpaTables { get; set; } = [];
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
            return [.. input.Select(converter)];
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
            File mbeFile = new File
            {
                StructureType = GetStructureType(_source),

                ExpaHeader = ReadExpaHeader(_data)
            };

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

    public class Writer : EXPA
    {
        private File _mbeFile;

        private List<(string Type, string Data, uint Offset, string ErrorMsg)> _chnkData = [];

        private byte[] _data = [];

        private uint _offset = 0;

        private string _source = "";
        private string _outpath = "";
        public Writer(File mbeFile, string source)
        {
            if (mbeFile == null || mbeFile.Tables == null || mbeFile.Tables.Count() == 0)
                throw new ArgumentException("Tables cannot be null or empty for patch conversion.");

            if (string.IsNullOrEmpty(source))
                throw new ArgumentException("Source path cannot be null or empty.");
            _source = source;
            _mbeFile = mbeFile;
        }
        public void Write(string outpath)
        {

            if (string.IsNullOrEmpty(outpath))
                throw new ArgumentException("Output path cannot be null or empty.");

            _outpath = outpath;

            WriteExpaHeader();

            WriteExpaTables();

            WriteChunkHeader();

            WriteChunkData();

            WriteFile();
        }

        private void WriteFile()
        {
            if (!Path.HasExtension(_outpath))
            {
                _outpath += "\\" + Path.GetFileNameWithoutExtension(_source) + ".mbe";
            }

            using (var output = new BinaryWriter(System.IO.File.Open(_outpath, FileMode.Create)))
                output.Write(_data);
        }

        private void WriteChunkData()
        {
            foreach (var entry in _chnkData)
            {
                _data = [.. _data, .. BitConverter.GetBytes(entry.Offset)];
                _offset = (uint)_data.Length;
                switch (entry.Type)
                {
                    case "string":
                        var strSize = ((Encoding.UTF8.GetByteCount(entry.Data) + 5) / 4) * 4; // +1 for null terminator
                        byte[] strBytes = new byte[strSize];
                        Encoding.UTF8.GetBytes(entry.Data).CopyTo(strBytes,0);
                        _data = [.. _data,.. BitConverter.GetBytes(strSize), .. strBytes];
                        break;
                    case "int array":
                        var intArray = entry.Data.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => Convert.ToInt32(s)).ToArray();
                        _data = [.. _data, .. BitConverter.GetBytes((uint)intArray.Length), .. (new byte[4])]; // Padding
                        foreach (var number in intArray)
                        {
                            _data = [.. _data, .. BitConverter.GetBytes(number)];
                        }
                        break;
                }
            }
        }

        private void WriteChunkHeader() {_data = [.. _data, .. BitConverter.GetBytes(CHNK_MAGIC), .. BitConverter.GetBytes((uint)_chnkData.Count)]; _offset = (uint)_data.Length;}

        private void WriteExpaTables()
        {
            foreach (DataTable table in _mbeFile.Tables!)
            {
                //Write Table Header
                var nameSize = ((table.Name.Length + 4) / 4) * 4;
                var namePadding = new byte[nameSize];
                Encoding.UTF8.GetBytes(table.Name).CopyTo(namePadding, 0);

                uint entrySize = CalculateEntrySize();

                _data = [.. _data, .. BitConverter.GetBytes(nameSize), .. namePadding, .. BitConverter.GetBytes(entrySize)];
                _offset = (uint)_data.Length;

                // Write Entries
                foreach (var entry in table.Entries)
                {
                    uint curEntrySize = 0;
                    foreach (var prop in _mbeFile.StructureType!.GetProperties())
                    {
                        string type = Utils.NormalizeType(prop.PropertyType);
                        var value = prop.GetValue(entry) ?? "";
                        WriteEXPAEntry(type, value?.ToString() ?? "", ref curEntrySize);
                    }
                    WritePadding(ref curEntrySize, 8);

                    _offset = (uint)_data.Length;
                }
            }
        }

        private void WriteEXPAEntry(string type, string value, ref uint currentEntrySize)
        {

            try
            {
                switch (type)
                {
                    case "byte":
                        _data = [.. _data, Convert.ToByte(value)];
                        currentEntrySize += 1;
                        _offset += 1;
                        break;

                    case "short":
                        WritePadding(ref currentEntrySize, 2);
                        _data = [.. _data, .. BitConverter.GetBytes(Convert.ToInt16(value))];
                        currentEntrySize += 2;
                        _offset += 2;
                        break;

                    case "int":
                        WritePadding(ref currentEntrySize, 4);
                        _data = [.. _data, .. BitConverter.GetBytes(Convert.ToInt32(value))];
                        currentEntrySize += 4;
                        _offset += 4;
                        break;

                    case "float":
                        WritePadding(ref currentEntrySize, 4);
                        _data = [.. _data, .. BitConverter.GetBytes(Convert.ToSingle(value, CultureInfo.InvariantCulture))];
                        currentEntrySize += 4;
                        _offset += 4;
                        break;

                    case "string":
                        WritePadding(ref currentEntrySize, 8);
                        if (!string.IsNullOrEmpty((string)value))
                        {
                            _chnkData.Add((
                                type,
                                (string)value,
                                _offset,
                                ""
                            ));
                        }
                        _data = [.. _data, .. BitConverter.GetBytes((long)0)]; // Placeholder for string pointer
                        currentEntrySize += 8;
                        _offset += 8;
                        break;

                    case "int array":
                        WritePadding(ref currentEntrySize, 8);
                        if (!string.IsNullOrEmpty((string)value))
                        {
                            _chnkData.Add((
                                type,
                                (string)value,
                                _offset + 8,
                                $""
                            ));
                        }

                        var arraySize = !string.IsNullOrEmpty((string)value)
                            ? ((string)value).Split(' ').Length
                            : 0;

                        _data = [.. _data, .. BitConverter.GetBytes(arraySize)];
                        _data = [.. _data, ..  (new byte[4])]; // Padding
                        _data = [.. _data, .. BitConverter.GetBytes((long)0)]; // Placeholder for array pointer
                        currentEntrySize += 16;
                        _offset += 16;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Value '{value}' cannot be converted to '{type}' \n" + ex);
            }
        }

        private void WritePadding(ref uint currentEntrySize, int alignment)
        {
            var paddingSize = Align(currentEntrySize, alignment);
            if (paddingSize > 0)
            {
                var padding = new byte[paddingSize];
                Array.Fill(padding, PADDING_BYTE);
                _data = [.._data, .. padding];
                currentEntrySize += paddingSize;
                _offset += paddingSize;
            }
        }

        private uint CalculateEntrySize()
        {
            uint size = 0;
            foreach (var prop in _mbeFile.StructureType!.GetProperties())
            {
                string type = Utils.NormalizeType(prop.PropertyType);
                size += GetEntrySize(type, size);
            }
            return size += Align(size, 8);
        }

        private void WriteExpaHeader()
        {
            _data = BitConverter.GetBytes(EXPA_MAGIC);
            _data = [.. _data, ..BitConverter.GetBytes(_mbeFile.ExpaHeader.NumTables)];
            _offset += 8;
        }
    }


    public partial class Converter : EXPA
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

            if (isPatch && (mbeFile.StructureType == typeof(Message) || mbeFile.StructureType == typeof(Text)))
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
            else if(isPatch)
                throw new NotImplementedException($"Patch conversion is not implemented for structure type {mbeFile.StructureType?.Name ?? "STRUCTURETYPE NULL"}.");
            else
            {
                var dict = mbeFile.Tables.ToDictionary(t => t.Name, t => t.Entries);
                return serializer.Serialize(dict);
            }
        }

        public static File FromYamlFile(string source, bool isPatch = true)
        { 
            string yaml = System.IO.File.ReadAllText(source);

            return FromYaml(source, yaml, isPatch);
        }

        public static File FromYaml(string source, string yaml, bool isPatch = true)
        {
            Type? structureType = GetStructureType(source);
            if (string.IsNullOrEmpty(yaml))
                throw new ArgumentException("YAML content cannot be null or empty.");
            if (structureType == null)
                throw new ArgumentNullException(nameof(structureType), "Structure type cannot be null.");


            var mbeFile = new File();
            mbeFile.StructureType = structureType;

            if (isPatch && (structureType == typeof(Message) || structureType == typeof(Text)))
            {
                if (structureType == typeof(Message))
                {
                    if (yaml.Contains("entries:\r\n") || yaml.Contains("Sheet1:\r\n"))
                    {
                        yaml = Sheet1Regex().Replace(yaml, "");

                    }

                    mbeFile.Tables =
                    [
                        new DataTable
                        {
                            Name = "Sheet1",
                            Entries = DataTable.ConvertList<PatchMessage, IMBEClass>(
                                deserializer.Deserialize<List<PatchMessage>>(yaml),
                                x => (IMBEClass)((Message)x)
                            )
                        },
                    ];
                }
                else if (structureType == typeof(Text))
                {
                    string? tableName = null;
                    if (yaml.Contains("entries:\r\n") || yaml.Contains("Sheet1:\r\n") || yaml.Contains("event:\r\n"))
                    {
                        tableName = Sheet1Regex().Match(yaml).Groups[1].Value;
                        yaml = Sheet1Regex().Replace(yaml, "");

                    }
                    if (string.IsNullOrEmpty(tableName))
                    {

                        tableName = source.Contains("tournament_name")? "event" : "Sheet1"; // Check if the source file contains "tournament_name" to determine the table name
                    }

                    mbeFile.Tables =
                    [
                        new DataTable
                        {
                            Name = tableName,
                            Entries = DataTable.ConvertList<PatchText, IMBEClass>(
                                deserializer.Deserialize<List<PatchText>>(yaml),
                                x => (IMBEClass)((Text)x)
                            )
                        },
                    ];

                }
                else
                    throw new NotImplementedException($"Patch conversion is not implemented for structure type {structureType.Name}.");

            } else
            {
                throw new NotImplementedException($"Conversion is not implemented for file {Path.GetFileName(source)} yet.");


            }

            if (mbeFile.Tables == null || mbeFile.Tables.Count == 0)
                throw new InvalidOperationException("No tables found in the YAML content.");

            // Set the ExpaHeader and ChnkHeader to default values
            mbeFile.ExpaHeader = new EXPAHeader()
            {
                MagicValue = EXPA_MAGIC,
                NumTables = (uint)mbeFile.Tables.Count
            };
            mbeFile.ChnkHeader = new CHNKHeader()
            {
                MagicValue = CHNK_MAGIC,
                NumEntry = 0 // Set to 0 initially, will be updated later
            };

            return mbeFile;
        }

        [GeneratedRegex(@"(?:entries:\r\n *)?(Sheet1|event):\r\n")]
        private static partial Regex Sheet1Regex();
    }


}
