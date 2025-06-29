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

namespace DSCSTools
{
    public class EXPA
    {
        protected const byte PADDING_BYTE = 0x00;

        private const string MESSAGES_REGEX = @"(^[>m|s][0-9]{2,3}_.{3,4}_\d{4})|(battle_(\d{4}|colosseum))|[d|t]\d{3,5}(_add|_\d\d)?$|(keyword_[d|t]\d{3,5}_.*)|(broken_nabit|after_evt|(kyoko|mirei)_help(_add)?)|((hm_|emblem_)?quest($|_(?!text|para)(.{3,8}(vent)?)))|(.*(?<!battle_info|^common|help|info|yes_no)_message(_add)?$)|(field_te.*)";
        private const string TEXT_REGEX = @"(tournament_name|.*(battle_info|^common|help|info)_message(_add)?$)|(^col.*?_(event_battle_.*|free.*|item_.*|text))|(custom_.*_(bgm|scene)(?!_para))|(digi?(?(_)_farm|(farm|line|mon)(_food)?_(text(_add)?|book.*|type)))|(hackers?_(battle_m.*|r.*))|((hacking_|support_)?skill(_content|_target)?_(c.*n_exp|e.*|n.*))|((char|field|equip_|item_|k.*d_|map.*|medal_)name)|([bmqs](?!i|ul|el).*_text(?!_para)(_add)?)|.*(_e.*n$)|(^bgm$|elem.*|^[eg].*tion$|mai.*u|^personality$|scen.*ect|st.*ress)";
        private const string TEXT_PARA_REGEX = @"(tut.*title|yes.no.*|(eden|mi|mu).*_text$)";
        internal protected const UInt32 EXPA_MAGIC = 0x41505845; // "EXPA"
        internal protected const UInt32 CHNK_MAGIC = 0x4B4E4843; // "CHNK"
        internal protected struct EXPAHeader
        {
            public uint MagicValue = EXPA_MAGIC;
            public uint NumTables;

            public EXPAHeader()
            {
            }
        }

        internal protected class EXPATable
        {
            public required byte[] TablePtr;
            public int Offset;

            public uint NameSize() => BitConverter.ToUInt32(TablePtr, Offset);
            public string Name() => Encoding.UTF8.GetString(TablePtr, Offset + 4, (int)NameSize()).TrimEnd('\0');
            public uint EntrySize() => BitConverter.ToUInt32(TablePtr, Offset + (int)NameSize() + 4);
            public uint EntryCount() => BitConverter.ToUInt32(TablePtr, Offset + (int)NameSize() + 8);
        }

        internal protected struct CHNKHeader
        {
            public uint MagicValue = CHNK_MAGIC;
            public uint NumEntry;

            public CHNKHeader()
            {
            }
        }

        protected static uint Align(long offset, long value)
        {
            return (uint)((value - (offset % value)) % value);
        }
        internal protected static uint GetEntrySize(string type, uint currentSize)
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

        protected static Type GetStructureType(string sourcePath)
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
        protected static EXPAHeader ReadExpaHeader(byte[] data)
        {
            if (BitConverter.ToUInt32(data, 0) != EXPA_MAGIC) // "EXPA"
                throw new InvalidDataException($"File is not in EXPA structureType.");

            return new EXPAHeader
            {
                MagicValue = BitConverter.ToUInt32(data, 0),
                NumTables = BitConverter.ToUInt32(data, 4)
            };
        }

        protected static CHNKHeader ReadChnkHeader(byte[] data, uint offset)
        {
            if (BitConverter.ToUInt32(data, (int)offset) != CHNK_MAGIC) // "CHNK"
                throw new InvalidDataException($"CHNK is missing or not where expected.");

            return new CHNKHeader
            {
                MagicValue = BitConverter.ToUInt32(data, (int)offset),
                NumEntry = BitConverter.ToUInt32(data, (int)offset + 4)
            };
        }

        protected static List<EXPATable> ReadExpaTables(byte[] data,ref uint offset, EXPAHeader header)
        {
            List<EXPATable> tables = [];

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
            return tables;
        }


        protected static string ReadEXPAEntry(byte[] data, ref int offset, string type)
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
    }
}