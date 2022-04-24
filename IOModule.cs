public struct Position
{
    private uint linenumber;
    private uint charnumber;
    public Position(uint linenumber, uint charnumber)
    {
        this.linenumber = (uint)linenumber;
        this.charnumber = (uint)charnumber;
    }
    public uint Linenumber { get => linenumber; set => linenumber = value; }
    public uint Charnumber { get => charnumber; set => charnumber = value; }
    public override string ToString() => $"line: {linenumber}, position: {charnumber}";
}

public struct Error
{
    private Position position;
    private uint[] errorcodes;
    public Error(Position position, uint[] errorcodes)
    {
        this.position = position;
        this.errorcodes = errorcodes;
    }


    public Position Position { get => position; set => position = value; }
    public uint[] Errorcode { get => errorcodes; set => errorcodes = value; }
    public override string ToString() => $"Error code: {String.Join(", ", errorcodes.Select(x => x.ToString()))}, {Position.ToString()}.";
}
public class IOModule
{
    private uint linenumber = 1;
    private uint charnumber = 0;
    private string? current_line;
    private char current_char;
    StreamReader reader;
    List<Error> errors = new List<Error>();

    public IOModule(string filepath)
    {
        reader = new StreamReader(filepath);
        current_line = reader.ReadLine();
    }

    public Position Position { get => new Position(linenumber, charnumber); }
    public char Current_char { get => current_char; }

    public char? NextChar()
    {
        if (current_line == null)
            return null;
        if (charnumber == current_line.Length)
        {
            current_line = reader.ReadLine();
            while (current_line == "")
                current_line = reader.ReadLine();
            linenumber++;
            charnumber = 0;
        }
        if (current_line == null)
            return null;
        current_char = current_line[(int)charnumber];
        charnumber++;
        return current_char;
    }

    public void AddError(Position position, params uint[] error_code)
    {
        errors.Add(new Error(position, error_code));
    }

    public void PrintErrors(Action<string> Print)
    {
        foreach (var error in errors)
            Print(error.ToString());
    }
}