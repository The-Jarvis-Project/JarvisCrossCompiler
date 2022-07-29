using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace JCC
{
    /// <summary>
    /// Base class for all S2S compilers.
    /// </summary>
    internal class Compiler
    {
        /// <summary>
        /// Whether or not to enforce language conventions.
        /// </summary>
        public readonly bool enforceConventions;
        protected readonly string srcPath, inputText, newL;
        protected readonly SyntaxTree tree;
        protected readonly CompilationUnitSyntax root;
        protected readonly CSharpCompilation comp;
        protected readonly SemanticModel sModel;
        protected readonly Stopwatch stopwatch;
        protected int discards;

        private static Compiler? Current { get; set; }

        /// <summary>
        /// Whether or not compilers enforce language conventions.
        /// </summary>
        public static bool UseConvention => Current != null && Current.enforceConventions;

        /// <summary>
        /// The compiled source text.
        /// </summary>
        public string OutputText { get; protected set; }

        /// <summary>
        /// The list of debug logs from compilation.
        /// </summary>
        public List<string> Debug { get; protected set; }

        /// <summary>
        /// The time the compilation took.
        /// </summary>
        public TimeSpan Time { get; protected set; }

        private readonly string[] asmFiles = new[]
        {
            typeof(string).Assembly.Location,
            typeof(Compiler).Assembly.Location
        };

        /// <summary>
        /// Creates a new compiler instance.
        /// </summary>
        /// <param name="srcPath">The path of the source file to be compiled</param>
        /// <param name="parseOptions">The options the compiler uses to parse code</param>
        /// <param name="enforceConventions">
        /// Whether or not the compiler should enforce language conventions</param>
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

        /// <summary>
        /// Runs the compiler.
        /// </summary>
        public virtual void Run()
        {
            OutputText = string.Empty;
            stopwatch.Start();
        }

        /// <summary>
        /// Called right before the compiler finishes translating source code.
        /// </summary>
        public virtual void Finish()
        {
            stopwatch.Stop();
            Time = stopwatch.Elapsed;
            stopwatch.Reset();
        }
    }
}
