using JCC.Java;
using JCC.Kotlin;

namespace JCC
{
    /// <summary>
    /// The target platform to compile to.
    /// </summary>
    public enum CompileTarget
    {
        Java, Kotlin
    }

    /// <summary>
    /// Contains information on the translated source code.
    /// </summary>
    public struct JCCOutput
    {
        /// <summary>
        /// The output source code text.
        /// </summary>
        public string output;

        /// <summary>
        /// If the compilation failed.
        /// </summary>
        public bool failed;

        /// <summary>
        /// The debug logs for the compilation.
        /// </summary>
        public string[] debug;

        /// <summary>
        /// The time the compilation took in milliseconds.
        /// </summary>
        public double time;

        /// <summary>
        /// Returns a string of debug lines.
        /// </summary>
        /// <returns>The debug string</returns>
        public string DebugAsString()
        {
            string debugString = string.Empty;
            for (int i = 0; i < debug.Length; i++)
                debugString += debug[i] + Environment.NewLine;
            return debugString;
        }

        /// <summary>
        /// Returns the time the compilation took in milliseconds as a string.
        /// </summary>
        /// <returns>The time as a string (ms)</returns>
        public string TimeDebug() => "Time: " + time + " ms";
        
        /// <summary>
        /// Returns the output as a string.
        /// </summary>
        /// <returns>The output of the compilation</returns>
        public override string ToString() => output;
    }

    /// <summary>
    /// Used to Compile C# source code files into a different language.
    /// </summary>
    public static class JCompile
    {
        /// <summary>
        /// Compiles a C# source code file into another language.
        /// </summary>
        /// <param name="inPath">The file path of the C# source file</param>
        /// <param name="outPath">The file path of the output file</param>
        /// <param name="target">The target language to compile to</param>
        /// <param name="enforceConventions">Whether or not to enforce standard language conventions</param>
        /// <returns>A struct containing info on the resulting source code</returns>
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
                    time = compiler.Time.TotalMilliseconds
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
                    time = compiler.Time.TotalMilliseconds
                };
            }
            return new JCCOutput
            {
                failed = true
            };
        }
    }
}