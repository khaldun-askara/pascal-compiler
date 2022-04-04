Console.WriteLine("Hello, World!");
IOModule iomodule = new IOModule("pascal example.txt");
while (iomodule.NextChar())
{
    Console.WriteLine(iomodule.Current_char);
}