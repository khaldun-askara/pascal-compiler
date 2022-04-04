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
    private uint errorcode;
    public Error(uint line, uint position, uint errorcode)
    {
        this.position = new Position(line, position);
        this.errorcode = errorcode;
    }

    public Position Position { get => position; set => position = value; }
    public uint Errorcode { get => errorcode; set => errorcode = value; }
    public override string ToString() => $"Error code: {errorcode}, {Position.ToString()}.";
}
public class IOModule
{
    private uint linenumber = 0;
    private uint charnumber = 0;
    private string current_line;
    private char current_char;
    StreamReader reader;
    List<Error> errors = new List<Error>();

    public IOModule(string filepath)
    {
        reader = new StreamReader(filepath);
        current_line = reader.ReadLine();
    }

    public char Current_char { get => current_char; }
    public Position Position { get => new Position(linenumber, charnumber); }

    public bool NextChar()
    {
        if (current_line == null)
            return false;
        if (charnumber == current_line.Length)
        {
            current_line = reader.ReadLine();
            while (current_line == "")
                current_line = reader.ReadLine();
            linenumber++;
            charnumber = 0;
        }
        if (current_line == null)
            return false;
        current_char = current_line[(int)charnumber];
        charnumber++;
        return true;
    }

    public void AddError(uint error_code)
    {
        errors.Add(new Error(linenumber, charnumber, error_code));
    }

    public void PrintErrors(Action<string> Print)
    {
        foreach (var error in errors)
            Print(error.ToString());
    }
}