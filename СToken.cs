public enum TokenType
{
    ttIdent,
    ttKeyword,
    ttConst
}

public abstract class CToken
{
    private TokenType type;
    private Position position;
    protected KeyWord current_token_code;

    protected CToken(TokenType type, Position position, KeyWord current_token_code)
    {
        this.type = type;
        this.position = position;
        this.current_token_code = current_token_code;
    }

    internal TokenType Type { get => type; }

    public abstract override string ToString();
}

public class CIdentToken : CToken
{
    public string name;
    public CIdentToken(Position position, KeyWord code, string name) : base(TokenType.ttIdent, position, code)
    {
        this.name = name;
    }

    public override string ToString()
    {
        return name;
    }
}

public class CKeywordToken : CToken
{
    public CKeywordToken(Position position, KeyWord code) : base(TokenType.ttKeyword, position, code) { }

    public override string ToString()
    {
        return current_token_code.ToString();
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