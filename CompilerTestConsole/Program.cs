using JCC;

string outPath = @"C:\Jarvis\TestJava.java";

string[] filePaths = new[] {
    //@"D:\Visual Studio\Projects\Jarvis\Jarvis\Behaviors\CommandLineBehavior.cs",
    //@"D:\Visual Studio\Projects\Jarvis\Jarvis\Behaviors\HappinessBehavior.cs",
    //@"D:\Visual Studio\Projects\Jarvis\Jarvis\Behaviors\TestBehavior.cs",
    //@"D:\Visual Studio\Projects\JarvisLinker\JarvisLinker\Controllers\JarvisRequestsController.cs",
    //@"D:\Visual Studio\Projects\JarvisCrossCompiler\JarvisCrossCompiler\JCompile.cs",
    @"D:\Unity\Projects\Purgatory\Assets\Scripts\Generation\ChunkGenerator.cs",
};

for (int i = 0; i < filePaths.Length; i++)
{
    JCCOutput output = JCompile.Compile(filePaths[i], outPath, CompileTarget.Java, true);

    Console.WriteLine(output.TimeDebug());
    Console.WriteLine();
    if (output.debug.Length > 0)
        Console.Write(output.DebugAsString() + "\n\n");

    Console.Write(output);
    Console.WriteLine();
    Console.WriteLine();
    Console.WriteLine();
}
