// int N = new DirectoryInfo("neutralization tests").GetFiles().Length;

// for (int i = 1; i <= N; i++)
// {
//     string filename = $"neutralization tests/test{i}.txt";
//     IOModule iomodule = new IOModule(filename);
//     LexicalAnalyzer lex_analyzer = new LexicalAnalyzer(iomodule);
//     try
//     {
//         Analyzer analyzer = new Analyzer(iomodule, lex_analyzer);
//         analyzer.Program();
//     }
//     catch (Exception e)
//     {
//         Console.WriteLine(e.Message);
//     }
//     Console.WriteLine($"Test {filename}");
//     iomodule.PrintErrors(Console.WriteLine);
// }

string filename = $"error3.txt";
IOModule iomodule = new IOModule(filename);
LexicalAnalyzer lex_analyzer = new LexicalAnalyzer(iomodule);
Analyzer analyzer = new Analyzer(iomodule, lex_analyzer);
try
{
    analyzer.Program();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}
Console.WriteLine($"Test {filename}");
iomodule.PrintErrors(Console.WriteLine);
Console.WriteLine("end");