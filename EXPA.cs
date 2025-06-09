using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Globalization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace DSCSTools.MBE
{
    public class EXPA
    {
        private const byte PADDING_BYTE = 0xCC;

        private struct EXPAHeader
        {
            public uint MagicValue;
            public uint NumTables;
        }

        private class EXPATable
        {
            public byte[] TablePtr;
            public int Offset;

            public uint NameSize() => BitConverter.ToUInt32(TablePtr, Offset);
            public string Name() => Encoding.UTF8.GetString(TablePtr, Offset + 4, (int)NameSize()).TrimEnd('\0');
            public uint EntrySize() => BitConverter.ToUInt32(TablePtr, Offset + (int)NameSize() + 4);
            public uint EntryCount() => BitConverter.ToUInt32(TablePtr, Offset + (int)NameSize() + 8);
        }

        private struct CHNKHeader
        {
            public uint MagicValue;
            public uint NumEntry;
        }

        private static uint Align(long offset, long value)
        {
            return (uint)((value - (offset % value)) % value);
        }

        private static string WrapRegex(string input)
        {
            return $"^{input}$";
        }

        private static JObject GetStructureFile(string sourcePath)
        {
            var structure = JObject.Parse(File.ReadAllText("structures/structure.json"));
            string formatFile = "";

            foreach (var property in structure.Properties())
            {
                if (Regex.IsMatch(sourcePath, property.Name))
                {
                    formatFile = property.Value.ToString();
                    break;
                }
            }

            if (string.IsNullOrEmpty(formatFile))
                throw new Exception($"Error: No fitting structure file found for {sourcePath}");

            return JObject.Parse(File.ReadAllText($"structures/{formatFile}"));
        }

        private static uint GetEntrySize(string type, uint currentSize)
        {
            switch (type)
            {
                case "byte": return 1;
                case "short": return 2 + Align(currentSize, 2);
                case "int":
                case "float": return 4 + Align(currentSize, 4);
                case "string": return 8 + Align(currentSize, 8);
                case "int array": return 16 + Align(currentSize, 8);
                default: return 0;
            }
        }

        public static void ExtractMBE(string sourcePath, string targetPath)
        {
            Console.WriteLine($"sourcePath:{sourcePath}");
            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
                throw new ArgumentException($"Error: input path \"{sourcePath}\" does not exist.");

            Console.WriteLine($"1sourcePath:{sourcePath}");  
            if (Path.GetFullPath(sourcePath) == Path.GetFullPath(targetPath))
                throw new ArgumentException("Error: input and output path must be different!");
            Console.WriteLine($"2sourcePath:{sourcePath}");  

            if (Directory.Exists(sourcePath))
            {
                Console.WriteLine($"3sourcePath:{sourcePath}");  
                foreach (var file in Directory.GetFiles(sourcePath)){
                    SaveYamlObj(file, targetPath);
                    Console.WriteLine($"FILEPath:{sourcePath}");  

                }
                    
            }
            else if (File.Exists(sourcePath))
            {
                Console.WriteLine($"4sourcePath:{sourcePath}");  
                SaveYamlObj(sourcePath, targetPath);
            }
            else
            {
                Console.WriteLine($"5sourcePath:{sourcePath}");  
                throw new ArgumentException("Error: input is neither directory nor file.");
            }
        }
        public static void SaveYamlObj(string sourcePath, string targetPath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            Console.WriteLine($"sourcePath:{sourcePath}");
            Console.WriteLine($"targetPath:{targetPath}");

            // var outputPath = Path.Combine(targetPath,Path.GetFileName(sourcePath));
            var outputPath = targetPath;
            Directory.CreateDirectory(outputPath+"\\");
            outputPath = Path.Combine(outputPath, $"./{Path.GetFileNameWithoutExtension(sourcePath)}.yaml");
            Console.WriteLine($"outPath:{outputPath}");

            // Process MBE file and get extracted data
            Dictionary<string, List<Dictionary<string, string>>> extractedTable = ProcessMBE(sourcePath, targetPath);
            Dictionary<string, List<CustomSerializer>> extractedTablePatch = [];
            
            // Convert the extracted data to a list of CustomSerializer objects
            var customSerializedData = new List<CustomSerializer>();
            foreach (var table in extractedTable)
            {
                extractedTablePatch.Add(table.Key, []);
                foreach (var row in table.Value)
                {
                    var customSerializer = new CustomSerializer
                    {
                        msgID = int.Parse(row.GetValueOrDefault("ID", "0")),
                        jpn = row.GetValueOrDefault("Japanese", ""),
                        eng = row.GetValueOrDefault("English", ""),
                        engDub = row.GetValueOrDefault("EnglishCensored", ""),

                        // Add other properties as needed
                    };
                    extractedTablePatch[table.Key].Add(customSerializer);
                }
            }

            var yaml = serializer.Serialize(extractedTablePatch);
            using var output = new StreamWriter(outputPath);
            output.Write(yaml);
        }

        public static Dictionary<string, List<Dictionary<string, string>>> ProcessMBE(string sourcePath, string targetPath)
        {
            
            using (var input = new BinaryReader(File.OpenRead(sourcePath)))
            {
                var data = input.ReadBytes((int)input.BaseStream.Length);

                // Read EXPA Header
                EXPAHeader header = new()
                {
                    MagicValue = BitConverter.ToUInt32(data, 0),
                    NumTables = BitConverter.ToUInt32(data, 4)
                };

                if (header.MagicValue != 0x41505845) // "EXPA"
                    throw new InvalidOperationException($"Error: source file {Path.GetFileName(sourcePath)} is not in EXPA format.");

                List<EXPATable> tables = [];
                uint offset = 8;

                // Read table information
                for (uint i = 0; i < header.NumTables; i++)
                {
                    var table = new EXPATable { TablePtr = data, Offset = (int)offset };
                    tables.Add(table);

                    offset += table.NameSize() + 0x0C;

                    if (table.NameSize() % 8 == 0)
                        offset += 4;

                    offset += table.EntryCount() * (table.EntrySize() + Align(table.EntrySize(), 8));
                }

                // Read CHNK Header
                CHNKHeader chunkHeader = new()
                {
                    MagicValue = BitConverter.ToUInt32(data, (int)offset),
                    NumEntry = BitConverter.ToUInt32(data, (int)offset + 4)
                };
                offset += 8;

                // Process CHNK entries
                for (uint i = 0; i < chunkHeader.NumEntry; i++)
                {
                    uint dataOffset = BitConverter.ToUInt32(data, (int)offset);
                    uint size = BitConverter.ToUInt32(data, (int)offset + 4);
                    ulong ptr = (ulong)offset;

                    Buffer.BlockCopy(BitConverter.GetBytes(ptr), 0, data, (int)dataOffset, 8);
                    offset += (size + 8);
                }

                // Get structure file
                JObject format = GetStructureFile(sourcePath);
                //Type format = GetType(sourcePath);
                string filename = Path.GetFileName(sourcePath);
                Dictionary<string, List<Dictionary<string, string>>> extractedTable = [];

                // Process each table
                foreach (EXPATable table in tables)
                {
                    string tableName = table.Name();
                    DSCS_MBE_Tool.Strucs.Text text = new DSCS_MBE_Tool.Strucs.Text();
                    //text.GetType().GetProperties()
                    uint tableHeaderSize = 0x0C + table.NameSize() + Align(table.NameSize() + 4, 8);
                    JObject formatValue = MatchStructureName(format, tableName, filename);
                    extractedTable.Add(tableName, new List<Dictionary<string, string>>((int)table.EntryCount()));

                        // Read data
                        for (uint i = 0; i < table.EntryCount(); i++)
                        {
                            int localOffset = (int)(table.Offset + i * (table.EntrySize() + Align(table.EntrySize(), 8)) + tableHeaderSize);
                            extractedTable[tableName].Add([]);
                            foreach (var entry in formatValue.Properties())
                            {
                                string type = entry.Value.ToString();
                                string value = ReadEXPAEntry(data, ref localOffset, type);
                                extractedTable[tableName][(int)i][entry.Name] = value;
                            }
                        }
                }
                return extractedTable;
                
            }
        }

        private static Type GetType(string sourcePath)
        {
            var structure = GetStructureFile(sourcePath);
            string formatFile = structure.Properties().FirstOrDefault()?.Value.ToString() ?? "default.json";
            return Type.GetType($"DSCS_MBE_Tool.Strucs.{formatFile}", true);
        }

        private static JObject MatchStructureName(JObject format, string structureName, string sourceName)
        {
            var formatValue = format[structureName];
            if (formatValue == null)
            {
                // Scan all table definitions to find a matching regex expression, if any
                foreach (var property in format.Properties())
                {
                    if (Regex.IsMatch(structureName, WrapRegex(property.Name)))
                    {
                        formatValue = property.Value as JObject;
                        break;
                    }
                }
                if (formatValue == null)
                    throw new Exception($"Error: no definition for table {structureName} found. {sourceName}");
            }
            return formatValue as JObject;
        }

        private static string ReadEXPAEntry(byte[] data, ref int offset, string type)
        {
            try
            {
                switch (type)
                {
                    case "byte":
                        return data[offset++].ToString();
                    case "short":
                        offset += (int)Align(offset, 2);
                        short shortValue = BitConverter.ToInt16(data, offset);
                        offset += 2;
                        return shortValue.ToString();
                    case "int":
                        offset += (int)Align(offset, 4);
                        int intValue = BitConverter.ToInt32(data, offset);
                        offset += 4;
                        return intValue.ToString();
                    case "float":
                        offset += (int)Align(offset, 4);
                        float floatValue = BitConverter.ToSingle(data, offset);
                        offset += 4;
                        return floatValue.ToString(CultureInfo.InvariantCulture);
                    case "string":
                        offset += (int)Align(offset, 8);
                        long stringPtr = BitConverter.ToInt64(data, offset);
                        offset += 8;
                        if (stringPtr == 0) return "";
                        if (stringPtr < 0 || stringPtr + 8 >= data.Length) return ""; // Out of bounds

                        // Read the entire string until null terminator
                        int stringStart = (int)stringPtr + 8;
                        int stringEnd = Array.IndexOf(data, (byte)0, stringStart);
                        if (stringEnd == -1) stringEnd = data.Length; // If no null terminator, read to end of data

                        int actualLength = stringEnd - stringStart;
                        string result = Encoding.UTF8.GetString(data, stringStart, actualLength);
                        return result;
                    case "int array":
                        offset += (int)Align(offset, 8);
                        int elemCount = BitConverter.ToInt32(data, offset);
                        offset += 4;
                        offset += (int)Align(offset, 8);
                        long arrayPtr = BitConverter.ToInt64(data, offset);
                        offset += 8;
                        if (arrayPtr == 0) return "";
                        if (arrayPtr < 0 || arrayPtr + 8 >= data.Length) return ""; // Out of bounds
                        if (elemCount < 0 || arrayPtr + 8 + (elemCount * 4) > data.Length) return ""; // Out of bounds
                        var array = new int[elemCount];
                        Buffer.BlockCopy(data, (int)arrayPtr + 8, array, 0, elemCount * 4);
                        return string.Join(" ", array);
                    default:
                        throw new ArgumentException($"Unknown type: {type}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading entry of type {type} at offset {offset}: {ex.Message}");
                return "";
            }
        }

        public static void PatchMBE(string mbeSourcePath, string mbePatchPath, string outputPath)
        {
            // Process the source MBE file to get the original data
            var mbeSource = ProcessMBE(mbeSourcePath, mbeSourcePath);

            // Deserialize the YAML patch file
            var deserializer = new DeserializerBuilder()
                                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                .Build();

            var yamlContent = File.ReadAllText(mbePatchPath);
            var mbePatch = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(yamlContent);

            // Iterate through each table in the patch
            foreach (var table in mbePatch)
            {
                string tableName = table.Key;
                var patchEntries = table.Value;

                // Check if the table exists in the source
                if (mbeSource.ContainsKey(tableName))
                {
                    var sourceEntries = mbeSource[tableName];

                    // Iterate through each entry in the patch table
                    foreach (var patchEntry in patchEntries)
                    {
                        // Assume "ID" is the unique identifier for each entry
                        string patchId = patchEntry.GetValueOrDefault("msgID", "");

                        // Find the corresponding entry in the source table
                        var sourceEntry = sourceEntries.FirstOrDefault(e => e.GetValueOrDefault("ID", "") == patchId);

                        if (sourceEntry != null)
                        {
                            // If the patch entry has an "eng" field, replace the "English" field in the source entry
                            if (patchEntry.ContainsKey("eng"))
                            {
                                sourceEntry["English"] = patchEntry["eng"];
                            }
                        }
                        else
                        {
                            // If the entry doesn't exist in the source, add it
                            // sourceEntries.Add(patchEntry);
                        }
                    }
                }
                else
                {
                    // If the table doesn't exist in the source, add the entire table
                    mbeSource[tableName] = patchEntries;
                }
            }

            // Now pack the updated data into a new MBE file using the PackMBE logic
            PackMBEFromDictionary(mbeSource, outputPath);
        }

        private static void PackMBEFromDictionary(Dictionary<string, List<Dictionary<string, string>>> data, string outputPath)
        {
            // Input validation
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Error: output path cannot be null or empty.");

            // Create target directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            using (var output = new BinaryWriter(File.Open(outputPath, FileMode.Create)))
            {
                // Write EXPA Header
                output.Write(Encoding.UTF8.GetBytes("EXPA"));
                long numTablesPosition = output.BaseStream.Position;
                output.Write((uint)0); // Placeholder for numTables

                // This will store our string and array data that needs to be written in the CHNK section
                var chnkData = new List<(string Type, string Data, uint Offset, string ErrorMsg)>();

                uint numTables = 0;

                // Process each table in the data
                foreach (var table in data)
                {
                    string tableName = table.Key;
                    var tableEntries = table.Value;

                    // Calculate entry size based on the structure (assuming a fixed structure for simplicity)
                    uint entrySize = 0;
                    foreach (var entry in tableEntries.FirstOrDefault() ?? new Dictionary<string, string>())
                    {
                        entrySize += GetEntrySize("string", entrySize); // Assuming all fields are strings for simplicity
                    }
                    entrySize += Align(entrySize, 8);

                    // Write table header
                    var nameSize = ((tableName.Length + 4) / 4) * 4;
                    var namePadding = new byte[nameSize];
                    Encoding.UTF8.GetBytes(tableName).CopyTo(namePadding, 0);

                    output.Write((uint)nameSize);
                    output.Write(namePadding);
                    output.Write(entrySize);
                    output.Write((uint)tableEntries.Count);

                    // Write padding after header if needed
                    var headerPadding = new byte[Align(0x0C + nameSize, 8)];
                    Array.Fill(headerPadding, PADDING_BYTE);
                    output.Write(headerPadding);

                    // Process each row in the table
                    foreach (var row in tableEntries)
                    {
                        uint currentEntrySize = 0;

                        foreach (var entry in row)
                        {
                            var columnName = entry.Key;
                            var columnValue = entry.Value;

                            try
                            {
                                // Assuming all fields are strings for simplicity
                                WritePadding(output, ref currentEntrySize, 8);
                                if (!string.IsNullOrEmpty(columnValue))
                                {
                                    chnkData.Add((
                                        "string",
                                        columnValue,
                                        (uint)output.BaseStream.Position,
                                        ""
                                    ));
                                }
                                output.Write((long)0); // Placeholder for string pointer
                                currentEntrySize += 8;
                            }
                            catch (Exception ex)
                            {
                                throw new InvalidOperationException(
                                    $"Error packing {tableName}: " +
                                    $"Value '{columnValue}' cannot be written for column '{columnName}'");
                            }
                        }

                        // Write padding after entry if needed
                        WritePadding(output, ref currentEntrySize, 8);
                    }

                    numTables++;
                }

                // Write CHNK header
                output.Write(Encoding.UTF8.GetBytes("CHNK"));
                output.Write((uint)chnkData.Count);

                // Write CHNK data
                foreach (var entry in chnkData)
                {
                    output.Write(entry.Offset);

                    switch (entry.Type)
                    {
                        case "string":
                            var stringBytesLength = Encoding.UTF8.GetBytes(entry.Data).Length;
                            UInt32 paddingSize = (UInt32)((stringBytesLength + 5) / 4) * 4; // Calculate padded size
                            output.Write((uint)paddingSize);

                            // Create a byte array with the correct size and copy the string data
                            var stringData = new byte[paddingSize];
                            Encoding.UTF8.GetBytes(entry.Data).CopyTo(stringData, 0);
                            output.Write(stringData);
                            break;
                    }
                }

                // Update numTables
                output.Seek((int)numTablesPosition, SeekOrigin.Begin);
                output.Write(numTables);
            }
        }

        public static void PackMBE(string sourcePath, string targetPath)
        {
            // Input validation
            if (!Directory.Exists(sourcePath))
                throw new ArgumentException($"Error: input path \"{sourcePath}\" does not exist.");

            if (Path.GetFullPath(sourcePath) == Path.GetFullPath(targetPath))
                throw new ArgumentException("Error: input and output path must be different!");

            if (!Directory.Exists(sourcePath))
                throw new ArgumentException("Error: input path is not a directory.");

            // Create target directory if it doesn't exist
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

            using (var output = new BinaryWriter(File.Open(targetPath, FileMode.Create)))
            {
                var format = GetStructureFile(sourcePath);

                // Write EXPA Header
                output.Write(Encoding.UTF8.GetBytes("EXPA"));
                long numTablesPosition = output.BaseStream.Position;
                output.Write((uint)0); // Placeholder for numTables

                // This will store our string and array data that needs to be written in the CHNK section
                var chnkData = new List<(string Type, string Data, uint Offset, string ErrorMsg)>();

                // Find and sort YAML files
                var sortedFiles = new List<List<string>>();
                foreach (var formatProperty in format.Properties())
                {
                    var regex = new Regex(WrapRegex(formatProperty.Name));
                    var matchingFiles = Directory.GetFiles(sourcePath, "*.yaml")
                        .Where(f => regex.IsMatch(Path.GetFileNameWithoutExtension(f)))
                        .ToList();
                    matchingFiles.Sort();
                    sortedFiles.Add(matchingFiles);
                }

                uint numTables = 0;

                // Process each table format
                for (int i = 0; i < sortedFiles.Count; i++)
                {
                    var localFormat = format.Properties().ElementAt(i).Value as JObject;
                    var filelist = sortedFiles[i];

                    foreach (var file in filelist)
                    {
                        numTables++;
                        var filename = Path.GetFileNameWithoutExtension(file);

                        // Deserialize YAML file
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();

                        var yamlContent = File.ReadAllText(file);
                        var yamlData = deserializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(yamlContent);

                        foreach (var table in yamlData)
                        {
                            // Write EXPA Table header
                            var nameSize = ((table.Key.Length + 4) / 4) * 4;
                            var namePadding = new byte[nameSize];
                            Encoding.UTF8.GetBytes(table.Key).CopyTo(namePadding, 0);

                            // Calculate entry size
                            uint entrySize = 0;
                            foreach (var formatEntry in localFormat.Properties())
                            {
                                entrySize += GetEntrySize(formatEntry.Value.ToString(), entrySize);
                            }
                            entrySize += Align(entrySize, 8);

                            // Write table header
                            output.Write((uint)nameSize);
                            output.Write(namePadding);
                            output.Write(entrySize);
                            output.Write((uint)table.Value.Count);

                            // Write padding after header if needed
                            var headerPadding = new byte[Align(0x0C + nameSize, 8)];
                            Array.Fill(headerPadding, PADDING_BYTE);
                            output.Write(headerPadding);

                            // Process each row in the table
                            foreach (var row in table.Value)
                            {
                                uint currentEntrySize = 0;

                                foreach (var formatEntry in localFormat.Properties())
                                {
                                    var columnName = formatEntry.Name;
                                    var columnType = formatEntry.Value.ToString();
                                    var columnValue = row.GetValueOrDefault(columnName, "");

                                    try
                                    {
                                        switch (columnType)
                                        {
                                            case "byte":
                                                output.Write(Convert.ToByte(columnValue));
                                                currentEntrySize += 1;
                                                break;

                                            case "short":
                                                WritePadding(output, ref currentEntrySize, 2);
                                                output.Write(Convert.ToInt16(columnValue));
                                                currentEntrySize += 2;
                                                break;

                                            case "int":
                                                WritePadding(output, ref currentEntrySize, 4);
                                                output.Write(Convert.ToInt32(columnValue));
                                                currentEntrySize += 4;
                                                break;

                                            case "float":
                                                WritePadding(output, ref currentEntrySize, 4);
                                                output.Write(Convert.ToSingle(columnValue, CultureInfo.InvariantCulture));
                                                currentEntrySize += 4;
                                                break;

                                            case "string":
                                                WritePadding(output, ref currentEntrySize, 8);
                                                if (!string.IsNullOrEmpty(columnValue))
                                                {
                                                    chnkData.Add((
                                                        columnType,
                                                        columnValue,
                                                        (uint)output.BaseStream.Position,
                                                        ""
                                                    ));
                                                }
                                                output.Write((long)0); // Placeholder for string pointer
                                                currentEntrySize += 8;
                                                break;

                                            case "int array":
                                                WritePadding(output, ref currentEntrySize, 8);
                                                if (!string.IsNullOrEmpty(columnValue))
                                                {
                                                    chnkData.Add((
                                                        columnType,
                                                        columnValue,
                                                        (uint)output.BaseStream.Position + 8,
                                                        $"Error packing {Path.GetFileName(sourcePath)}/{filename}: Value '{columnValue}' cannot be converted to 'int array'"
                                                    ));
                                                }

                                                var arraySize = !string.IsNullOrEmpty(columnValue) 
                                                    ? columnValue.Split(' ').Length 
                                                    : 0;
                                                
                                                output.Write(arraySize);
                                                output.Write(new byte[4]); // Padding
                                                output.Write((long)0); // Placeholder for array pointer
                                                currentEntrySize += 16;
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new InvalidOperationException(
                                            $"Error packing {Path.GetFileName(sourcePath)}/{filename}: " +
                                            $"Value '{columnValue}' cannot be converted to '{columnType}' " +
                                            $"in column '{columnName}'");
                                    }
                                }

                                // Write padding after entry if needed
                                WritePadding(output, ref currentEntrySize, 8);
                            }
                        }
                    }
                }

                // Write CHNK header
                output.Write(Encoding.UTF8.GetBytes("CHNK"));
                output.Write((uint)chnkData.Count);

                // Write CHNK data
                foreach (var entry in chnkData)
                {
                    output.Write(entry.Offset);

                    switch (entry.Type)
                    {
                        case "string":
                            var stringSize = ((entry.Data.Length + 5) / 4) * 4;
                            output.Write((uint)stringSize);
                            var stringData = new byte[stringSize];
                            Encoding.UTF8.GetBytes(entry.Data).CopyTo(stringData, 0);
                            output.Write(stringData);
                            break;

                        case "int array":
                            var numbers = entry.Data.Split(' ').Select(n => Convert.ToInt32(n)).ToArray();
                            output.Write((uint)(numbers.Length * 4));
                            foreach (var number in numbers)
                            {
                                output.Write(number);
                            }
                            break;
                    }
                }

                // Update numTables
                output.Seek((int)numTablesPosition, SeekOrigin.Begin);
                output.Write(numTables);
            }
        }

        // Helper method to write padding bytes
        private static void WritePadding(BinaryWriter writer, ref uint currentSize, uint alignment)
        {
            var paddingSize = Align(currentSize, alignment);
            if (paddingSize > 0)
            {
                var padding = new byte[paddingSize];
                Array.Fill(padding, PADDING_BYTE);
                writer.Write(padding);
                currentSize += paddingSize;
            }
        }
    }
}