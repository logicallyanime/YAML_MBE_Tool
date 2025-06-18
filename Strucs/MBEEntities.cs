using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DSCS_MBE_Tool.Strucs
{

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
}
