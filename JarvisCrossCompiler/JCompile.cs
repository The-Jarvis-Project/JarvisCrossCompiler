using JCC.Java;
using JCC.Kotlin;

namespace JCC
{
    public enum CompileTarget
    {
        Java, Kotlin
    }

    public struct JCCOutput
    {
        public string output;
        public bool failed;
        public string[] debug;
        public double time;

        public string DebugAsString()
        {
            string debugString = string.Empty;
            for (int i = 0; i < debug.Length; i++)
                debugString += debug[i] + Environment.NewLine;
            return debugString;
        }

        public string TimeDebug() => "Time: " + time + " ms";
        
        public override string ToString() => output;
    }

    public static class JCompile
    {
        public static JCCOutput Compile(string inPath, string outPath,
            CompileTarget target, bool enforceConventions = true)
        {
            if (target == CompileTarget.Java)
            {
                JavaCompiler compiler = new JavaCompiler(inPath, enforceConventions);
                compiler.Run();
                File.WriteAllText(outPath, compiler.OutputText);
                return new JCCOutput
                {
                    output = compiler.OutputText,
                    debug = compiler.Debug.ToArray(),
                    time = compiler.Time.TotalMinutes
                };
            }
            else if (target == CompileTarget.Kotlin)
            {
                KotlinCompiler compiler = new KotlinCompiler(inPath, enforceConventions);
                compiler.Run();
                File.WriteAllText(outPath, compiler.OutputText);
                return new JCCOutput
                {
                    output = compiler.OutputText,
                    debug = compiler.Debug.ToArray(),
                    time = compiler.Time.TotalMinutes
                };
            }
            return new JCCOutput
            {
                failed = true
            };
        }
    }
}