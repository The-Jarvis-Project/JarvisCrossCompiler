using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace JCC
{
    internal class Compiler
    {
        public readonly bool enforceConventions;
        protected readonly string srcPath, inputText, newL;
        protected readonly SyntaxTree tree;
        protected readonly CompilationUnitSyntax root;
        protected readonly CSharpCompilation comp;
        protected readonly SemanticModel sModel;
        protected readonly Stopwatch stopwatch;
        protected int discards;

        private static Compiler? Current { get; set; }
        public static bool UseConvention => Current != null && Current.enforceConventions;

        public string OutputText { get; protected set; }
        public List<string> Debug { get; protected set; }
        public TimeSpan Time { get; protected set; }

        private readonly string[] asmFiles = new[]
        {
            typeof(string).Assembly.Location,
            typeof(Compiler).Assembly.Location
        };

        public Compiler(string srcPath, CSharpParseOptions parseOptions, bool enforceConventions)
        {
            this.srcPath = srcPath;
            this.enforceConventions = enforceConventions;
            Current = this;
            inputText = File.ReadAllText(srcPath);
            tree = CSharpSyntaxTree.ParseText(inputText, parseOptions, srcPath);
            root = tree.GetCompilationUnitRoot();
            MetadataReference[] refs = new MetadataReference[asmFiles.Length];
            for (int i = 0; i < asmFiles.Length; i++)
                refs[i] = MetadataReference.CreateFromFile(asmFiles[i]);
            comp = CSharpCompilation.Create("_JCC_CS_Comp").AddReferences(refs).AddSyntaxTrees(tree);
            sModel = comp.GetSemanticModel(tree);
            newL = Environment.NewLine;
            OutputText = string.Empty;
            Debug = new List<string>();
            stopwatch = new Stopwatch();
            discards = 0;
        }

        public virtual void Run()
        {
            OutputText = string.Empty;
            stopwatch.Start();
        }

        public virtual void Finish()
        {
            stopwatch.Stop();
            Time = stopwatch.Elapsed;
            stopwatch.Reset();
        }
    }
}
