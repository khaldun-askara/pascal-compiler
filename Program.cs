Console.WriteLine("Hello, World!");
IOModule iomodule = new IOModule("emptyfile.txt");
while (iomodule.NextChar())
{
    Console.WriteLine(iomodule.Current_char);
    iomodule.AddError(13);
}
iomodule.PrintErrors(Console.WriteLine);