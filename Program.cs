IOModule iomodule = new IOModule("error2.txt");
LexicalAnalyzer analyzer = new LexicalAnalyzer(iomodule);
CToken token = analyzer.NextToken();
while (token != null)
{
    Console.WriteLine(analyzer.Current_token.ToString());
    token = analyzer.NextToken();
}
iomodule.PrintErrors(Console.WriteLine);
