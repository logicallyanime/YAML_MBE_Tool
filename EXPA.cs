using DSCS_MBE_Tool;
using DSCS_MBE_Tool.Strucs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static DSCS_MBE_Tool.NameDB;

namespace DSCSTools.MBE
{
    public class EXPA
    {
        private const byte PADDING_BYTE = 0x00;

        private const string MESSAGES_REGEX = @"(^[>m|s][0-9]{2,3}_.{3,4}_\d{4})|(battle_(\d{4}|colosseum))|[d|t]\d{3,5}(_add|_\d\d)?$|(keyword_[d|t]\d{3,5}_.*)|(broken_nabit|after_evt|(kyoko|mirei)_help(_add)?)|((hm_|emblem_)?quest($|_(?!text|para)(.{3,8}(vent)?)))|(.*(?<!battle_info|^common|help|info|yes_no)_message(_add)?$)|(field_te.*)";
        private const string TEXT_REGEX = @"(.*(battle_info|^common|help|info)_message(_add)?$)|(^col.*?_(event_battle_.*|free.*|item_.*|text))|(custom_.*_(bgm|scene)(?!_para))|(digi?(?(_)_farm|(farm|line|mon)(_food)?_(text(_add)?|book.*|type)))|(hackers?_(battle_m.*|r.*))|((hacking_|support_)?skill(_content|_target)?_(c.*n_exp|e.*|n.*))|((char|field|equip_|item_|k.*d_|map.*|medal_)name)|([bmqs](?!i|ul|el).*_text(?!_para)(_add)?)|.*(_e.*n$)|(^bgm$|elem.*|^[eg].*tion$|mai.*u|^personality$|scen.*ect|st.*ress)";
        private const string TEXT_PARA_REGEX = @"(tut.*title|yes.no.*|(eden|mi|mu).*_text$)";
        private struct EXPAHeader
        {
            public uint MagicValue;
            public uint NumTables;
        }

        private class EXPATable
        {
            public required byte[] TablePtr;
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

        private static Type GetStructureType(string sourcePath)
        {
            string filename = Path.GetFileNameWithoutExtension(sourcePath);
            RegexOptions options = RegexOptions.ExplicitCapture;
            if (Regex.IsMatch(filename, TEXT_PARA_REGEX, options))
            {
                return typeof(Text);
            }
            if (Regex.IsMatch(filename, TEXT_REGEX, options))
            {
                return typeof(Text);
            }
            if (Regex.IsMatch(filename, MESSAGES_REGEX, options)){
                return typeof(Message);
            }

            var thisAssembly = Assembly.GetExecutingAssembly();
            using (var stream = thisAssembly.GetManifestResourceStream("DSCS_MBE_Tool.structure.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var structure = JObject.Parse(json);
                    string formatFile = "";
                    foreach (var property in structure.Properties())
                    {
                        if (Regex.IsMatch(filename, property.Name))
                        {
                            formatFile = property.Value.ToString();
                            throw new NotImplementedException($"Error: Structure matching for {filename} is not implemented yet.");
                        }
                    }
                }
            }



            throw new Exception($"Error: No fitting structure file found for {sourcePath}");

        }

        private static uint GetEntrySize(string type, uint currentSize)
        {
            switch (type)
            {
                case "byte": return 1;
                case "short": return 2 + Align(currentSize, 2);
                case "int": return 4 + Align(currentSize, 4);
                case "float": return 4 + Align(currentSize, 4);
                case "string": return 8 + Align(currentSize, 8);
                case "int array": return 16 + Align(currentSize, 8);
                default: throw new ArgumentException($"Error: Type \"{type}\" not included in GetEntrySize cases");
            }
        }

        public static void ExtractMBE(string sourcePath, string targetPath)
        {
            // Use WriteVerbose extension with color instead of direct Console calls.
            $"[INFO] Input Path: {sourcePath}".WriteVerbose(ConsoleColor.Yellow);

            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
                throw new ArgumentException($"Error: input path \"{sourcePath}\" does not exist.");

            $"[STEP 1] Validating input path... {sourcePath}".WriteVerbose(ConsoleColor.Green);

            if (Path.GetFullPath(sourcePath) == Path.GetFullPath(targetPath))
                throw new ArgumentException("Error: input and output paths must be different!");

            $"[STEP 2] Paths validated successfully.".WriteVerbose(ConsoleColor.Green);

            if (Directory.Exists(sourcePath))
            {
                $"[DIRECTORY] Processing directory: {sourcePath}".WriteVerbose(ConsoleColor.Blue);
                string[] files = Directory.GetFiles(sourcePath);
                ConsoleProgress.ProgressBar progressBar = new(files.Length);
                int processedCount = 0;
                if (Global.Multithreading)
                {
                    Parallel.ForEach(files, file =>
                    {
                        // Use a local variable to avoid race conditions
                        MBETable localTable = EXPA.ParseYAML(file);
                        EXPA.PackMBETable(localTable, file, targetPath);
                        $"[FILE] Processed file: {file}".WriteVerbose(ConsoleColor.Magenta);
                        int count = Interlocked.Increment(ref processedCount);
                        progressBar.Report(count);
                    });
                }
                else
                {
                    foreach (var file in files)
                    {
                        SaveYamlObj(file, targetPath);
                        $"[FILE] Processed file: {file}".WriteVerbose(ConsoleColor.Magenta);
                        processedCount++;
                        progressBar.Report(processedCount);
                    }
                }
            }
            else if (File.Exists(sourcePath))
            {
                $"[FILE] Processing single file: {sourcePath}".WriteVerbose(ConsoleColor.Blue);
                SaveYamlObj(sourcePath, targetPath);
            }
            else
            {
                $"[ERROR] The source path is neither a directory nor a file: {sourcePath}".WriteVerbose(ConsoleColor.Red);
                throw new ArgumentException("Error: input is neither directory nor file.");
            }
        }
        public static void SaveYamlObj(string sourcePath, string targetPath)
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            // var outputPath = Path.Combine(targetPath,Path.GetFileName(sourcePath));
            var outputPath = targetPath;
            outputPath = Path.Combine(outputPath, $"./{Path.GetFileNameWithoutExtension(sourcePath)}.yaml");

            // Process MBE file and get extracted data
            MBETable extractedTable = ProcessMBE(sourcePath) ?? throw new Exception($"Error: No data extracted from {sourcePath}");
            string? yaml;
            if (Global.IsPatch)
            {
                IMBEClass? firstEntry = extractedTable.Entries.FirstOrDefault().Value.FirstOrDefault();
                if (firstEntry is Message or Text)
                {
                    MBETable tempTable = new MBETable();
                    foreach (var table in extractedTable.Entries)
                    {
                        var messages = tempTable.AddEmptyEntry(table.Key);
                        foreach (var entry in table.Value)
                        {
                            if (entry is Message message)
                            {
                                messages.Add(message.ToPatch(Path.GetFileName(sourcePath)));
                            }
                            else if (entry is Text text)
                            {
                                messages.Add(text.ToPatch(Path.GetFileName(sourcePath)));
                            }
                            else
                            {
                                throw new Exception($"Error: Entry in table {table.Key} is not of type Message.");
                            }
                        }
                    }

                    extractedTable = tempTable;

                }   
                yaml = serializer.Serialize(extractedTable.Entries.First().Value);
            }
            else
            {
                yaml = serializer.Serialize(extractedTable.Entries);
            }
            using var output = new StreamWriter(outputPath);
            output.Write(yaml);
        }

        public static MBETable ProcessMBE(string sourcePath)
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

                // Get structure 
                Type format = GetStructureType(sourcePath);
                string filename = Path.GetFileName(sourcePath);
                // Instead of: Dictionary<string, List<format>> extractedTable = [];
                // Use Activator.CreateInstance and IList for dynamic type creation

                var extractedTable = new MBETable();


                // Process each table
                foreach (EXPATable table in tables)
                {
                    List<IMBEClass> classList = extractedTable.AddEmptyEntry(table.Name());
                    uint tableHeaderSize = 0x0C + table.NameSize() + Align(table.NameSize() + 4, 8);
                    PropertyInfo[] formatProperties = format.GetProperties();

                    // Read data
                    for (uint i = 0; i < table.EntryCount(); i++)
                    {
                        // Dynamically create an instance of the type represented by 'format'
                        var instance = Activator.CreateInstance(format) ?? throw new Exception(filename + " - Error: Unable to create an instance of the format type.");
                        if (instance is not IMBEClass mbeClassInstance)
                            throw new Exception($"Error: Instance of {format.Name} does not implement IMBEClass interface.");
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
                return extractedTable;
                
            }
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

        public static MBETable ParseYAML(string sourcePath, bool isPatch = true)
        {
            // Input validation
            if (!File.Exists(sourcePath))
            {
                                throw new ArgumentException($"Error: input path \"{sourcePath}\" does not exist.");
            }
            // Deserialize the YAML patch file
            var deserializer = new DeserializerBuilder()
                                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                .Build();

            var yamlContent = File.ReadAllText(sourcePath);
            MBETable mbeTable = new MBETable();

            Type structureType = GetStructureType(sourcePath);

            if(structureType.Name is "Message")
            {
                if (Global.IsPatch)
                {
                    if(yamlContent.Contains("entries:\r\n " ) || yamlContent.Contains("Sheet1:\r\n "))
                    {
                        yamlContent = yamlContent.Replace("entries:\r\n ", "").Replace("Sheet1:\r\n ", "");
                    }
                    // Deserialize the YAML content into a list of PatchMessage objects
                    List <PatchMessage> mbePatch = deserializer.Deserialize<List<PatchMessage>>(yamlContent);

                    var entryList = mbeTable.AddEmptyEntry("Sheet1");

                    foreach(PatchMessage entry in mbePatch)
                    {
                        entryList.Add(entry.ToMessage());
                    }
                }
                else
                {
                    // Deserialize the YAML content into a list of Message objects
                    Dictionary<string, List<Message>> mbePatch = deserializer.Deserialize<Dictionary<string, List<Message>>>(yamlContent);
                    foreach (var table in mbePatch)
                    {
                        List<IMBEClass> entryList = mbeTable.AddEmptyEntry(table.Key);

                        foreach (Message entry in table.Value)
                        {
                            entryList.Add(entry);
                        }
                    }
                }

            }
            else if (structureType is Text)
            {
                if(Global.IsPatch)
                {
                    List<PatchText> mbePatch = deserializer.Deserialize<List<PatchText>>(yamlContent);
                    var entryList = mbeTable.AddEmptyEntry("Sheet1");
                    foreach (var entry in mbePatch)
                    {
                        entryList.Add(entry.ToText());
                    }
                }
                else
                {
                    Dictionary<string, List<Text>> mbePatch = deserializer.Deserialize<Dictionary<string, List<Text>>>(yamlContent);
                    foreach (var table in mbePatch)
                    {
                        List<IMBEClass> entryList = mbeTable.AddEmptyEntry(table.Key);
                        foreach (Text entry in table.Value)
                        {
                            entryList.Add(entry);
                        }
                    }
                }
            }
                return mbeTable;
        }
        public static void PackMBETable(MBETable mbeTable, string sourcePath, string targetPath)
        {
            // Input validation
            if (!File.Exists(sourcePath))
                throw new ArgumentException($"Error: input path \"{sourcePath}\" does not exist.");

            if (Path.GetFullPath(sourcePath) == Path.GetFullPath(targetPath))
                throw new ArgumentException("Error: input and output path must be different!");
            if (!Path.HasExtension(targetPath))
            {
                targetPath += "\\" + Path.GetFileNameWithoutExtension(sourcePath) + ".mbe";
            }

            // Create target directory if it doesn't exist

            using (var output = new BinaryWriter(File.Open(targetPath, FileMode.Create)))
            {
                Type type = GetStructureType(sourcePath);

                // Write EXPA Header
                output.Write(Encoding.UTF8.GetBytes("EXPA"));
                long numTablesPosition = output.BaseStream.Position;
                output.Write((uint)0); // Placeholder for numTables

                // This will store our string and array data that needs to be written in the CHNK section
                var chnkData = new List<(string Type, string Data, uint Offset, string ErrorMsg)>();

                uint numTables = 0;



                foreach (var table in mbeTable.Entries)
                {
                    numTables++;
                    // Write EXPA Table header
                    var nameSize = ((table.Key.Length + 4) / 4) * 4;
                    var namePadding = new byte[nameSize];
                    Encoding.UTF8.GetBytes(table.Key).CopyTo(namePadding, 0);

                    // Calculate entry size
                    uint entrySize = 0;
                    foreach (PropertyInfo prop in type.GetProperties())
                    {
                        entrySize += GetEntrySize(Utils.NormalizeType(prop.PropertyType), entrySize);
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
                    foreach (System.Object obj in table.Value)
                    {
                        uint currentEntrySize = 0;

                        foreach (PropertyInfo prop in type.GetProperties())
                        {
                            var propName = prop.Name;
                            var propType = Utils.NormalizeType(prop.PropertyType);
                            var propValue = prop.GetValue(obj) ?? "";

                            try
                            {
                                switch (propType)
                                {
                                    case "byte":
                                        output.Write(Convert.ToByte(propValue));
                                        currentEntrySize += 1;
                                        break;

                                    case "short":
                                        WritePadding(output, ref currentEntrySize, 2);
                                        output.Write(Convert.ToInt16(propValue));
                                        currentEntrySize += 2;
                                        break;

                                    case "int":
                                        WritePadding(output, ref currentEntrySize, 4);
                                        output.Write(Convert.ToInt32(propValue));
                                        currentEntrySize += 4;
                                        break;

                                    case "float":
                                        WritePadding(output, ref currentEntrySize, 4);
                                        output.Write(Convert.ToSingle(propValue, CultureInfo.InvariantCulture));
                                        currentEntrySize += 4;
                                        break;

                                    case "string":
                                        WritePadding(output, ref currentEntrySize, 8);
                                        if (!string.IsNullOrEmpty((string)propValue))
                                        {
                                            chnkData.Add((
                                                propType,
                                                (string)propValue,
                                                (uint)output.BaseStream.Position,
                                                ""
                                            ));
                                        }
                                        output.Write((long)0); // Placeholder for string pointer
                                        currentEntrySize += 8;
                                        break;

                                    case "int array":
                                        WritePadding(output, ref currentEntrySize, 8);
                                        if (!string.IsNullOrEmpty((string)propValue))
                                        {
                                            chnkData.Add((
                                                propType,
                                                (string)propValue,
                                                (uint)output.BaseStream.Position + 8,
                                                $"Error packing {Path.GetFileName(sourcePath)}/{Path.GetFileName(sourcePath)}: Value '{propValue}' cannot be converted to 'int array'"
                                            ));
                                        }

                                        var arraySize = !string.IsNullOrEmpty((string)propValue)
                                            ? ((string)propValue).Split(' ').Length
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
                                    $"Error packing {Path.GetFileName(sourcePath)}/{Path.GetFileName(sourcePath)}: " +
                                    $"Value '{propValue}' cannot be converted to '{propType}' " +
                                    $"in column '{propName}'\n" + ex);
                            }
                        }

                        // Write padding after entry if needed
                        WritePadding(output, ref currentEntrySize, 8);
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
                            var stringSize = ((Encoding.UTF8.GetBytes(entry.Data).Length + 5) / 4) * 4;
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
    }
}