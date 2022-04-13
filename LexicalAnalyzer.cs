public class FatalException : Exception { }
public class LexicalAnalyzer
{
    private char? ch;
    private CToken current_token;
    private IOModule iomodule;

    private Dictionary<string, KeyWord> KeywordsTable = new Dictionary<string, KeyWord>()
    {
        { "if", KeyWord.ifsy },
        { "do", KeyWord.dosy },
        { "else", KeyWord.elsesy },
        { "var", KeyWord.varsy },
        { "begin", KeyWord.beginsy },
        { "end", KeyWord.endsy },
        { "and", KeyWord.andsy },
        { "or", KeyWord.orsy },
        { "not", KeyWord.notsy},
        { "while", KeyWord.whilesy },
        { "program", KeyWord.programsy }
    };

    public LexicalAnalyzer(IOModule iomodule)
    {
        this.iomodule = iomodule;
        ch = iomodule.NextChar();
    }

    public CToken Current_token { get => current_token; }

    public CToken? NextToken()
    {
        if (!ch.HasValue)
            return null;

        // запоминаем позицию начала
        Position current_position = iomodule.Position;

        // пропуск пробелов и комментариев
        while (ch == ' ' || ch == '{' || ch == '(')
        {
            // пропуск комментариев в { }
            if (ch == '{')
            {
                while (ch.HasValue && ch != '}')
                {
                    ch = iomodule.NextChar();
                    if (!ch.HasValue)
                        return null;
                }
            }
            // пропуск блоков комментариев в (* *)
            if (ch == '(')
            {
                ch = iomodule.NextChar();
                if (ch == '*')
                {
                    while (ch.HasValue)
                    {
                        ch = iomodule.NextChar();
                        if (ch == '*')
                        {
                            ch = iomodule.NextChar();
                            if (ch == ')')
                                break;
                        }

                    }
                    // ch = iomodule.NextChar();
                }
                else
                {
                    current_token = new CKeywordToken(current_position, KeyWord.leftpar);
                    return current_token;
                }
            }
            ch = iomodule.NextChar();
            if (!ch.HasValue)
                return null;
        }

        // если буква или нижнее подчёркивание, то это либо ключевое слово, либо идентификатор, 
        // ЛИБО БУЛЕВОЕ ЗНАЧЕНИЕ АААААААААААААААААААААА
        if (char.IsLetter(ch.Value) || ch == '_')
        {
            string cur_token = string.Empty;
            // запоминаем до конца
            while (ch.HasValue && (char.IsLetter(ch.Value) || ch == '_'))
            {
                cur_token += ch;
                ch = iomodule.NextChar();
            }
            // ищем в ключевых словах, если не ключевое слово, то это идентификатор, его в таблицу имён
            if (KeywordsTable.ContainsKey(cur_token))
                current_token = new CKeywordToken(current_position, KeywordsTable[cur_token]);
            else if (cur_token == "true" || cur_token == "false")
                current_token = new CConstToken(current_position, KeyWord.constsy, new CBooleanVariant(Convert.ToBoolean(cur_token)));
            else
                // TODO: чё за таблица имён и с чем её едят 
                current_token = new CIdentToken(current_position, KeyWord.identsy, cur_token);
            // вроде всё ок и можно возвращать
            return current_token;
        }

        //если цифра, то может быть целая константа, а может быть вещественная константа
        if (char.IsDigit(ch.Value))
        {
            string cur_token = string.Empty;
            bool is_double = false;
            bool is_error = false;
            double realvalue = 0;
            int integervalue = 0;
            while (ch.HasValue && (char.IsDigit(ch.Value) || ch == '.'))
            {
                cur_token += ch;

                is_double = is_double || ch == '.';

                if (is_double)
                    is_error = !Double.TryParse(cur_token, out realvalue);
                else
                    is_error = !Int32.TryParse(cur_token, out integervalue);

                ch = iomodule.NextChar();
            }
            if (is_error)
                // 207 - слишком большая вещественная константа
                // 203 - целая константа превышает предел
                iomodule.AddError(is_double ? (uint)207 : (uint)203, current_position);
            if (is_double)
                current_token = new CConstToken(current_position, KeyWord.constsy, new CRealVariant(realvalue));
            else
                current_token = new CConstToken(current_position, KeyWord.constsy, new CIntVariant(integervalue));
            return current_token;
        }

        switch (ch)
        {
            // с символа ' начинаются строковые константы!! их нужно читать до следующего '
            case '\'':
                string cur_token = string.Empty;
                while (ch.HasValue && ch != '\'')
                {
                    cur_token += ch;
                    ch = iomodule.NextChar();
                }
                current_token = new CConstToken(current_position, KeyWord.constsy, new CStringVariant(cur_token));
                ch = iomodule.NextChar();
                return current_token;

            // с этих символов могут начинаться несколько разных токенов!!
            case '<':
                ch = iomodule.NextChar();
                if (ch == '=' || ch == '>')
                {
                    current_token = new CKeywordToken(current_position, ch == '=' ? KeyWord.laterequal : KeyWord.latergreater);
                    ch = iomodule.NextChar();
                }
                else current_token = new CKeywordToken(current_position, KeyWord.later);
                return current_token;
            case '>':
                ch = iomodule.NextChar();
                if (ch == '=')
                {
                    current_token = new CKeywordToken(current_position, KeyWord.greaterequal);
                    ch = iomodule.NextChar();
                }
                else current_token = new CKeywordToken(current_position, KeyWord.greater);
                return current_token;
            case ':':
                ch = iomodule.NextChar();
                if (ch == '=')
                {
                    current_token = new CKeywordToken(current_position, KeyWord.assign);
                    ch = iomodule.NextChar();
                }
                else current_token = new CKeywordToken(current_position, KeyWord.colon);
                return current_token;
            case '.':
                ch = iomodule.NextChar();
                if (ch == '.')
                {
                    current_token = new CKeywordToken(current_position, KeyWord.twopoints);
                    ch = iomodule.NextChar();
                }
                else current_token = new CKeywordToken(current_position, KeyWord.point);
                return current_token;
            case '+':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.plus);
                return current_token;
            case '-':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.minus);
                return current_token;
            case '/':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.slash);
                return current_token;
            case '=':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.equal);
                return current_token;
            case ',':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.comma);
                return current_token;
            case ';':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.semicolon);
                return current_token;
            case '^':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.arrow);
                return current_token;
            case '[':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.lbracket);
                return current_token;
            case ']':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.rbracket);
                return current_token;
            case '*':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.star);
                return current_token;
            case ')':
                ch = iomodule.NextChar();
                current_token = new CKeywordToken(current_position, KeyWord.rightpar);
                return current_token;

            // ! тут должна быть ошибка ?? что тут делать блин
            default:
                iomodule.AddError((uint)6, current_position);
                ch = iomodule.NextChar();
                throw new FatalException();
        }
    }
}