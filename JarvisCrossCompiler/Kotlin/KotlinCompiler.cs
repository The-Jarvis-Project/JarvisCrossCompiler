using Microsoft.CodeAnalysis.CSharp;

namespace JCC.Kotlin
{
    internal class KotlinCompiler : Compiler
    {

        public KotlinCompiler(string srcPath, bool enforceConventions) :
            base(srcPath, CSharpParseOptions.Default, enforceConventions)
        {

        }

        public override void Run()
        {
            base.Run();

            BuildOutput();
            Finish();
        }

        private void BuildOutput()
        {
            OutputText = string.Empty;
        }

    }
}
