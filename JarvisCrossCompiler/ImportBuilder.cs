namespace JCC
{
    internal class ImportBuilder
    {
        private readonly Dictionary<string, string> map;
        private readonly HashSet<string> addedTypes;
        private string[] defaultImports;

        public ImportBuilder()
        {
            map = new Dictionary<string, string>();
            addedTypes = new HashSet<string>();
            defaultImports = new string[0];
        }

        public ImportBuilder(params (string, string)[] collection)
        {
            map = new Dictionary<string, string>();
            addedTypes = new HashSet<string>();
            defaultImports = new string[0];
            for (int i = 0; i < collection.Length; i++)
                map.Add(collection[i].Item1, collection[i].Item2);
        }

        public void SetDefaultImports(params string[] init) => defaultImports = init;

        public void AddType(string type)
        {
            if (!addedTypes.Contains(type)) addedTypes.Add(type);
        }

        public void AddType(string[] subTypes)
        {
            for (int i = 0; i < subTypes.Length; i++) AddType(subTypes[i]);
        }

        public bool Remove(string type) => addedTypes.Remove(type);

        public string[] GetImports()
        {
            List<string> imports = new List<string>();
            for (int i = 0; i < defaultImports.Length; i++)
                if (!imports.Contains(defaultImports[i]))
                    imports.Add(defaultImports[i]);
            foreach (string type in addedTypes)
                if (map.ContainsKey(type) && !imports.Contains(map[type]))
                    imports.Add(map[type]);
            return imports.ToArray();
        }

        public void ClearTypes() => addedTypes.Clear();
    }
}
