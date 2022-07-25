namespace JCC
{
    internal class TypeMap
    {
        public enum Context
        {
            Function, Body
        }

        public struct NonExistProp
        {
            public string functionReturn, bodyType;

            public static NonExistProp Default => new NonExistProp("void", "var");

            public NonExistProp(string funcReturn = "void", string bodyType = "var")
            {
                functionReturn = funcReturn;
                this.bodyType = bodyType;
            }
        }

        private readonly Dictionary<string, string> map;
        private readonly HashSet<string> dontExist;
        private readonly NonExistProp nonExist;

        public TypeMap()
        {
            map = new Dictionary<string, string>();
            dontExist = new HashSet<string>();
            nonExist = new NonExistProp("void", "var");
        }

        public TypeMap(params (string, string)[] collection)
        {
            map = new Dictionary<string, string>();
            dontExist = new HashSet<string>();
            nonExist = new NonExistProp("void", "var");
            for (int i = 0; i < collection.Length; i++)
                Add(collection[i].Item1, collection[i].Item2);
        }

        public TypeMap(NonExistProp prop, params (string, string)[] collection)
        {
            map = new Dictionary<string, string>();
            dontExist = new HashSet<string>();
            nonExist = prop;
            for (int i = 0; i < collection.Length; i++)
                Add(collection[i].Item1, collection[i].Item2);
        }

        /// <summary>
        /// Adds a parsed value and a mapped value to the type map.
        /// If the parsed value has no analogous value then set the mapped value to "!"
        /// </summary>
        /// <param name="parsed">The parsed original value</param>
        /// <param name="mapped">The type string to map the parsed value to</param>
        public void Add(string parsed, string mapped)
        {
            if (mapped.Contains('!')) dontExist.Add(parsed);
            else map.Add(parsed, mapped);
        }

        public bool Remove(string parsed)
        {
            bool valE = dontExist.Remove(parsed), valM = map.Remove(parsed);
            return valE || valM;
        }

        public string Map(string parsed, Context context = Context.Body)
        {
            if (parsed.Contains('<'))
            {
                string mapped = string.Empty;
                bool allDontExist = true;
                int endTags = parsed.Count(f => f == '<');
                string[] genericSplits = parsed.Split(new[] { '<', '>' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                for (int i = 0; i < genericSplits.Length; i++)
                {
                    string split = genericSplits[i];
                    if (map.ContainsKey(split))
                    {
                        allDontExist = false;
                        mapped += map[split];
                        if (i != genericSplits.Length - 1) mapped += "<";
                    }
                    else if (!dontExist.Contains(split))
                    {
                        allDontExist = false;
                        mapped += split;
                        if (i != genericSplits.Length - 1) mapped += "<";
                    }
                    else endTags--;
                }

                if (allDontExist)
                {
                    if (context == Context.Body) mapped += nonExist.bodyType;
                    else mapped += nonExist.functionReturn;
                }
                for (int i = 0; i < endTags; i++) mapped += ">";
                return mapped;
            }
            else if (parsed.Contains('['))
            {
                string mapped = string.Empty, type = parsed[..parsed.IndexOf('[')];

                if (map.ContainsKey(type)) mapped = map[type];
                else if (dontExist.Contains(type)) mapped = nonExist.bodyType;
                else mapped = type;

                mapped += parsed[parsed.IndexOf('[')..];
                return mapped;
            }
            else
            {
                if (map.ContainsKey(parsed)) return map[parsed];
                else if (dontExist.Contains(parsed))
                {
                    if (context == Context.Body) return nonExist.bodyType;
                    else return nonExist.functionReturn;
                }
                return parsed;
            }
        }
    }
}
