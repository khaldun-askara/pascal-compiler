public enum TokenType
{
    ttIdent,
    ttKeyword,
    ttConst
}
public enum VariableType
{
    vartInteger,
    vartReal,
    vartString,
    vartBoolean,
    vartUndef,
}

public abstract class CToken
{
    private TokenType type;
    private Position position;
    protected KeyWord code;

    protected CToken(TokenType type, Position position, KeyWord code)
    {
        this.type = type;
        this.position = position;
        this.code = code;
    }

    public KeyWord Code { get => code; }
    public TokenType Type { get => type; }
    public Position Position { get => position; }

    public abstract override string ToString();
}

public class CIdentToken : CToken
{
    private string name;
    private VariableType variable_type;
    public CIdentToken(Position position, KeyWord code, string name) : base(TokenType.ttIdent, position, code)
    {
        this.name = name;
    }

    public VariableType Variable_type { get => variable_type; set => variable_type = value; }
    public string Name { get => name; set => name = value; }

    public override string ToString()
    {
        return Name;
    }
}

public class CKeywordToken : CToken
{
    public CKeywordToken(Position position, KeyWord code) : base(TokenType.ttKeyword, position, code) { }

    public override string ToString()
    {
        return code.ToString();
    }
}

public class CConstToken : CToken
{
    CVariant cvalue;
    public CConstToken(Position position, KeyWord code, CVariant value) : base(TokenType.ttConst, position, code)
    {
        this.cvalue = value;
    }

    CVariant CValue { get => cvalue; }

    public override string ToString()
    {
        return cvalue.ToString();
    }
}