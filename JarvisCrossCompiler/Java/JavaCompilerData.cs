using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace JCC.Java
{
    public struct JavaStructData
    {
        public string structName, relatedClass;
        public List<string> mods;
    }

    public struct JavaClassData
    {
        public string className, inheritsFrom;
        public bool csMarkedStatic;
        public List<string> mods, interfaces;
        public List<JavaMethodData> methods;
        public List<JavaEnumData> enums;
        public List<JavaFieldData> fields;
        public List<JavaPropertyData> properties; 
    }

    public struct JavaPropertyData
    {
        public string propName, typeName, initVal;
        public bool getter, setter;
        public List<string> varMods, getMods, setMods;
    }

    public struct JavaFieldData
    {
        public string typeName;
        public List<string> mods;
        public string[] varNames;
        public string?[] varVals;
    }

    public struct JavaEnumData
    {
        public string enumName;
        public List<string> mods, enumVals;
    }

    public struct JavaMethodData
    {
        public string methodName, typeName;
        public List<string> mods;
        public List<(string, string)> parameters;
        public JavaBlockData? blockData;
    }

    public struct JavaBlockData
    {
        public string blockText;
        public BlockSyntax root;

        public string TabString => Recursion.Tabs(curTabs);
        public int curTabs;
    }

    public struct JavaExpressionData
    {
        public string expressionText;
        public ExpressionSyntax root;

        public string TabString => Recursion.Tabs(curTabs);
        public int curTabs;
    }
}
