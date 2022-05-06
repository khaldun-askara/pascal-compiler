public class Analyzer
{
    private IOModule iomodule;
    private LexicalAnalyzer lexicalAnalyzer;
    CToken? cur_token;
    private Dictionary<string, CIdentToken> identifiers = new Dictionary<string, CIdentToken>();
    private static List<KeyWord> relationalOperators = new List<KeyWord>
    {
        KeyWord.equal,         // =
        KeyWord.latergreater,  // <>
        KeyWord.later,         // <
        KeyWord.laterequal,    // <=
        KeyWord.greaterequal,  // >=
        KeyWord.greater,       // >
        // in тут нет!
    };
    private static List<KeyWord> addingOperators = new List<KeyWord>
    {
        KeyWord.plus,  // +
        KeyWord.minus, // -
        KeyWord.orsy   // or
    };
    private static List<KeyWord> multiplyingOperators = new List<KeyWord>
    {
        KeyWord.star,   //' *
        KeyWord.slash,  //  /
        // div и mod не надо вроде делать
        KeyWord.andsy   //  and
    };
    public Analyzer(IOModule iomodule, LexicalAnalyzer lexicalAnalyzer)
    {
        this.iomodule = iomodule;
        this.lexicalAnalyzer = lexicalAnalyzer;
        this.cur_token = lexicalAnalyzer.NextToken();
    }
    bool EOF_error_added = false;
    bool EOF()
    {
        if (cur_token == null && !EOF_error_added)
        {
            // у Залоговой я не нашла обозначения Unexpected end of file, поэтому погуглила и нашла номер 10 вот тут: https://coderbook.ru/pascal-коды-ошибок/
            // в учебнике вообще другие циферки, но мне короче всё равно
            // UPD я теперь путаюсь с 10 "ошибка в типе", так что теперь unexpected end of file будет 1000
            iomodule.AddError(iomodule.Position, (uint)1000);
            EOF_error_added = true;
        }
        return EOF_error_added;
    }
    void Accept(params KeyWord[] expected_keywords)
    {
        bool is_ok = false;
        if (!EOF())
            foreach (KeyWord expected in expected_keywords)
            {
                is_ok |= expected == cur_token.Code;
                if (expected == cur_token.Code)
                {
                    cur_token = lexicalAnalyzer.NextToken();
                    break;
                }
            }
        if (!is_ok)
            iomodule.AddError(cur_token == null ? iomodule.Position : cur_token.Position, expected_keywords.Select(x => (uint)x).ToArray());
    }
    void Skip(int error_code, params KeyWord[] toKeywords)
    {
        iomodule.AddError(cur_token == null ? iomodule.Position : cur_token.Position, (uint)error_code);
        while (!EOF() && !toKeywords.ToList().Contains(cur_token.Code))
            cur_token = lexicalAnalyzer.NextToken();
    }
    void GotoFollowers(KeyWord[] followers)
    {
        if (!Belong(followers) && !EOF_error_added)
            // если честно, я не знаю, почему тут 6, но в учебнике Залоговой так, а я человек простой...
            Skip(6, followers);
    }
    bool Belong(params KeyWord[] starters)
    {
        return !EOF() && starters.ToList().Contains(cur_token.Code);
    }
    // <программа>::=program <имя>(<имя файла>{,<имя файла>}); <блок>.
    public void Program()
    {
        if (EOF())
        {
            iomodule.AddError(iomodule.Position, (uint)3);
            return;
        }
        if (!Belong(KeyWord.programsy))
            // 3:	должно быть служебное слово PROGRAM
            Skip(3, KeyWord.varsy, KeyWord.beginsy, KeyWord.programsy);
        if (Belong(KeyWord.programsy))
        {
            Accept(KeyWord.programsy);
            Accept(KeyWord.identsy);
            // кто такие (<имя файла>{,<имя файла>}) я не знаю
            Accept(KeyWord.semicolon);
            // тут нет проверки на followers, потому что дальше ничего нет!
        }
        Block(KeyWord.point);
        Accept(KeyWord.point);
    }
    // <блок>: :=
    ////           <раздел меток>
    ////           <раздел констант>
    ////           <раздел типов>
    //             <раздел переменных>
    ////           <раздел процедур и функций>
    //             <раздел операторов>
    void Block(params KeyWord[] followers)
    {
        if (!Belong(KeyWord.varsy, KeyWord.beginsy))
            // 18:	ошибка в разделе описаний
            Skip(18, followers.Append(KeyWord.varsy)
                              .Append(KeyWord.beginsy).ToArray());
        if (Belong(KeyWord.varsy, KeyWord.beginsy))
        {
            VariableDeclarationPart(followers.Append(KeyWord.beginsy).ToArray());
            StatementPart(followers);
            GotoFollowers(followers);
        }
    }
    #region VariableDeclarationPart
    // <раздел переменных> : = var <описание однотипных переменных>; {<описание однотипных переменных>;} 
    //                          | <пусто>
    void VariableDeclarationPart(params KeyWord[] followers)
    {
        if (!EOF())
        {
            if (!Belong(KeyWord.varsy) && !Belong(followers))
                // 18:	ошибка в разделе описаний
                Skip(18, followers.Append(KeyWord.varsy).ToArray());
            if (Belong(KeyWord.varsy))
            {
                Accept(KeyWord.varsy);
                do
                {
                    VariableDeclaration(followers.Append(KeyWord.semicolon).ToArray());
                    Accept(KeyWord.semicolon);
                }
                while (!EOF() && cur_token.Code == KeyWord.identsy);
                GotoFollowers(followers);
            }
        }
    }

    // описание однотипных переменных>::=<имя>{,<имя>} : <тип>
    void VariableDeclaration(params KeyWord[] followers)
    {
        if (!Belong(KeyWord.identsy))
            // 2: должно идти имя
            Skip(2, followers.Append(KeyWord.identsy).ToArray());
        if (Belong(KeyWord.identsy))
        {
            // текущие переменные одного типа
            List<CIdentToken> current_variables = new List<CIdentToken>();
            if (!EOF() && cur_token.Code == KeyWord.identsy)
            {
                string cur_name = ((CIdentToken)cur_token).Name;
                // если такой идентификатор уже есть в ТИ, нужно выдать ошибку 101: имя описано повторно
                if (identifiers.ContainsKey(cur_name))
                    iomodule.AddError(cur_token.Position, 101);
                else
                    current_variables.Add((CIdentToken)cur_token);
            }
            Accept(KeyWord.identsy);
            while (!EOF() && cur_token.Code == KeyWord.comma)
            {
                Accept(KeyWord.comma);
                if (!EOF() && cur_token.Code == KeyWord.identsy)
                {
                    string cur_name = ((CIdentToken)cur_token).Name;
                    // если такой идентификатор уже есть в ТИ или в текущих переменных (например, "a, a: string"), 
                    // нужно выдать ошибку 101: имя описано повторно
                    if (identifiers.ContainsKey(cur_name) || current_variables.Find(x => x.Name == cur_name) != null)
                        iomodule.AddError(cur_token.Position, 101);
                    else
                        current_variables.Add((CIdentToken)cur_token);
                }
                Accept(KeyWord.identsy);
            }
            Accept(KeyWord.colon);
            VariableType current_type = Type(followers);
            foreach (CIdentToken token in current_variables)
            {
                token.Variable_type = current_type;
                identifiers.Add(token.Name, token);
            }
            GotoFollowers(followers);
        }
    }

    VariableType Type(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        if (!Belong(KeyWord.integersy, KeyWord.stringsy, KeyWord.realsy, KeyWord.booleansy))
            Skip(10, followers.Append(KeyWord.integersy)
                          .Append(KeyWord.stringsy)
                          .Append(KeyWord.realsy)
                          .Append(KeyWord.booleansy).ToArray());
        VariableType result = VariableType.vartUndef;
        if (Belong(KeyWord.integersy, KeyWord.stringsy, KeyWord.realsy, KeyWord.booleansy))
        {
            switch (cur_token.Code)
            {
                case KeyWord.integersy:
                    result = VariableType.vartInteger;
                    break;
                case KeyWord.stringsy:
                    result = VariableType.vartString;
                    break;
                case KeyWord.realsy:
                    result = VariableType.vartReal;
                    break;
                case KeyWord.booleansy:
                    result = VariableType.vartBoolean;
                    break;
            }
            Accept(KeyWord.integersy, KeyWord.stringsy, KeyWord.realsy, KeyWord.booleansy);
            GotoFollowers(followers);
        }
        return result;
    }
    #endregion VariableDeclarationPart
    #region SemanticFunctions
    // эта функция проверяет, является ли type допустимым для if, while и not типом
    // среди реализованных такой только Boolean
    bool LogicalTest(VariableType type)
    {
        switch (type)
        {
            case VariableType.vartBoolean:
                return true;
            default:
                return false;
        }
    }
    // возвращает тип переменной, если такая была описана ранее в var
    // если переменную не описывали, говорит ошибку и возвращает тип-ошибку.
    VariableType GetIdentType()
    {
        VariableType result = VariableType.vartUndef;
        if (cur_token == null)
            return result;
        string cur_name = ((CIdentToken)cur_token).Name;
        // 104: имя не описано
        if (!identifiers.ContainsKey(cur_name))
        {
            iomodule.AddError(cur_token.Position, 104);
            ((CIdentToken)cur_token).Variable_type = VariableType.vartUndef;
            identifiers.Add(cur_name, ((CIdentToken)cur_token));
            result = VariableType.vartUndef;
        }
        else
            result = identifiers[cur_name].Variable_type;
        return result;
    }
    // функция проверяет, можно ли переменной первого типа assign выражение второго типа
    bool CanAssign(VariableType first_type, VariableType second_type)
    {
        bool result = false;
        if (first_type == VariableType.vartUndef || second_type == VariableType.vartUndef)
            return result;
        switch (first_type)
        {
            case VariableType.vartInteger:
                result = second_type == VariableType.vartInteger;
                break;
            case VariableType.vartReal:
                result = second_type == VariableType.vartReal || second_type == VariableType.vartInteger;
                break;
            case VariableType.vartString:
                result = second_type == VariableType.vartString;
                break;
            case VariableType.vartBoolean:
                result = second_type == VariableType.vartBoolean;
                break;
        }
        return result;
    }
    // проверка корректности типов операндов first_type и second_type для передаваемой операции nullab_operation, и в случае ошибки добавление ошибки в позиции operator_pos
    VariableType OperationType(VariableType first_type, VariableType second_type, Position operator_pos, KeyWord? nullab_operation)
    {
        VariableType result = VariableType.vartUndef;
        if (nullab_operation == null)
            return result;
        if (first_type == VariableType.vartUndef || second_type == VariableType.vartUndef)
            return VariableType.vartUndef;
        KeyWord operation = nullab_operation.Value;
        if (relationalOperators.Contains(operation))
            if (first_type == second_type ||
                first_type == VariableType.vartInteger && second_type == VariableType.vartReal ||
                first_type == VariableType.vartReal && second_type == VariableType.vartInteger)
                result = VariableType.vartBoolean;
            else
                // 186: несоответствие типов для операции отношения
                iomodule.AddError(operator_pos, (uint)186);
        if (addingOperators.Contains(operation) || multiplyingOperators.Contains(operation))
            switch (operation)
            {
                // складывать можно все пары, кроме boolean, а разные типы только между числовыми
                case KeyWord.plus:
                    if (first_type == second_type && first_type != VariableType.vartBoolean)
                        result = first_type;
                    else if (first_type == VariableType.vartInteger && second_type == VariableType.vartReal ||
                       first_type == VariableType.vartReal && second_type == VariableType.vartInteger)
                        result = VariableType.vartReal;
                    // 211: недопустимые типы операндов операции + или —
                    else iomodule.AddError(operator_pos, (uint)211);
                    break;
                // вычитать, умножать и делить можно только числовые
                case KeyWord.star:
                case KeyWord.slash:
                case KeyWord.minus:
                    // если делить инт на инт, будет real
                    if (first_type == second_type && first_type != VariableType.vartBoolean && first_type != VariableType.vartString)
                        result = operation == KeyWord.slash ? VariableType.vartReal : first_type;
                    // операции с real дают real
                    else if (first_type == VariableType.vartInteger && second_type == VariableType.vartReal ||
                       first_type == VariableType.vartReal && second_type == VariableType.vartInteger)
                        result = VariableType.vartReal;
                    else if (operation == KeyWord.minus)
                        // 211: недопустимые типы операндов операции + или —
                        iomodule.AddError(operator_pos, (uint)211);
                    else if (operation == KeyWord.slash)
                        //214: недопустимые типы операндов операции /
                        iomodule.AddError(operator_pos, (uint)214);
                    else if (operation == KeyWord.star)
                        //213: недопустимые типы операндов операции *
                        iomodule.AddError(operator_pos, (uint)213);
                    break;
                // логические только между boolean
                case KeyWord.andsy:
                case KeyWord.orsy:
                    if (first_type == second_type && first_type == VariableType.vartBoolean)
                        result = VariableType.vartBoolean;
                    else
                        // 210: операнды AND, NOT, OR должны быть булевыми
                        iomodule.AddError(operator_pos, (uint)210);
                    break;
            }
        return result;
    }
    // проверка корректности типа выражения, перед которым стоит знак (или не стоит)
    VariableType OperationType(bool unary_sign, Position sign_pos, VariableType type)
    {
        if (!unary_sign)
            return type;
        VariableType result = VariableType.vartUndef;
        switch (type)
        {
            case VariableType.vartInteger:
            case VariableType.vartReal:
                result = type;
                break;
            case VariableType.vartBoolean:
            case VariableType.vartString:
                // 184: элемент этого типа не может иметь знак
                iomodule.AddError(sign_pos, (uint)184);
                break;
        }
        return result;
    }
    #endregion SemanticFunctions
    #region StatementPart
    // <составной оператор>: := begin <оператор>{; <оператор>} end
    void StatementPart(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        if (!Belong(KeyWord.beginsy))
            // 17:	должно идти слово BEGIN
            Skip(17, followers.Append(KeyWord.beginsy).ToArray());
        if (Belong(KeyWord.beginsy))
        {
            Accept(KeyWord.beginsy);
            Statement(followers.Append(KeyWord.semicolon)
                               .Append(KeyWord.endsy).ToArray());
            while (!EOF() && cur_token.Code == KeyWord.semicolon)
            {
                Accept(KeyWord.semicolon);
                Statement(followers.Append(KeyWord.semicolon)
                                   .Append(KeyWord.endsy).ToArray());
            }
            Accept(KeyWord.endsy);
            GotoFollowers(followers);
        }
    }
    // <оператор>::=
    //   <непомеченный оператор>|
    //   <метка><непомеченный оператор>
    // <непомеченный оператор>::=<простой оператор>|<сложный оператор>
    // то есть по сути если без меток, то <оператор>::=<простой оператор>|<сложный оператор>
    // оператор присваивания начинается с <переменная>, составной оператор с begin, выбирающий оператор c if, оператор цикла (нам нужен только while) с while
    // ээээ получается можно определять, какой стейтмент в данный момент, исходя из первого токена
    void Statement(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        List<KeyWord> starters = new List<KeyWord>()
        {
            KeyWord.identsy,
            KeyWord.beginsy,
            KeyWord.ifsy,
            KeyWord.whilesy,
        };
        if (!Belong(starters.ToArray()) && !Belong(followers))
            // 183: запрещенная в данном контексте операция
            Skip(183, followers.Concat(starters).ToArray());
        if (Belong(starters.ToArray()))
        {
            switch (cur_token.Code)
            {
                // <простой оператор>::=<оператор присваивания>|
                ////                    <оператор процедуры>|
                ////                    <оператор перехода>|
                //                      <пустой оператор>
                case KeyWord.identsy:
                    Assignment(followers);
                    break;
                // <сложный оператор>::=<составной оператор>|
                //                      <выбирающий оператор>|
                //                      <оператор цикла>|
                ////                    <оператор присоединения>
                case KeyWord.beginsy:
                    StatementPart(followers);
                    break;
                case KeyWord.ifsy:
                    IfStatement(followers);
                    break;
                case KeyWord.whilesy:
                    WhileStatement(followers);
                    break;
            }
            GotoFollowers(followers);
        }
    }
    // <оператор присваивания>::=<переменная>:=<выражение>|
    ////                        <имя функции>:=<выражение>
    void Assignment(params KeyWord[] followers)
    {
        if (!Belong(KeyWord.identsy))
            Skip(2, followers.Append(KeyWord.identsy).ToArray());
        if (Belong(KeyWord.identsy))
        {
            VariableType ident_type = GetIdentType();
            Accept(KeyWord.identsy);
            Position assignment_pos = cur_token == null ? iomodule.Position : cur_token.Position;
            Accept(KeyWord.assign);
            VariableType expression_type = Expression(followers);
            if (!CanAssign(ident_type, expression_type) && ident_type != VariableType.vartUndef && expression_type != VariableType.vartUndef)
                // 145: конфликт типов
                iomodule.AddError(assignment_pos, (uint)145);
            GotoFollowers(followers);
        }
    }
    // <выражение>::=<простое выражение>|
    //               <простое выражение><операция отношения><простое выражение>
    // <простое выражение>::=<знак><слагаемое>{<аддитивная операция><слагаемое>}
    // так как грустно забивать на унарный знак, будем считать, что он [<знак>]
    // <знак>::= + | -
    // <слагаемое>::=<множитель>{<мультипликативная операция><множитель>}
    // <множитель>::=<переменная>|
    //               <константа без знака>|
    //               (<выражение>)|
    //               <обозначение функции>|
    //               <множество>|
    //               not <множитель>
    // <константа без знака>::=<число без знака>|
    //                         <строка>|
    //                         <имя константы>|
    //                         nil

    // получается, выражение может быть начато с:
    //                              знака (тогда это должно быть число)
    //                              идентификатора переменной
    //                              открывающей скобочки (
    //                              числа
    //                              строки
    //                              булевой ээээ константы ???? почему этого в бнф нет?!
    //                              not
    VariableType Expression(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        List<KeyWord> starters = new List<KeyWord>()
        {
            KeyWord.plus,
            KeyWord.minus,
            KeyWord.identsy,
            KeyWord.leftpar,
            KeyWord.constboolean,
            KeyWord.constint,
            KeyWord.constreal,
            KeyWord.conststring,
            KeyWord.notsy,
        };
        if (!Belong(starters.ToArray()))
            // 144: недопустимый тип выражения
            Skip(144, followers.Concat(starters).ToArray());
        VariableType result = VariableType.vartUndef;
        if (Belong(starters.ToArray()))
        {
            result = SimpleExpression(followers.Concat(relationalOperators).ToArray());
            if (!EOF() && relationalOperators.Contains(cur_token.Code))
            {
                Position operator_pos = cur_token.Position;
                KeyWord? relationalOperator = Operator(relationalOperators, followers.Concat(starters).ToArray());
                result = OperationType(result, SimpleExpression(followers.Concat(relationalOperators).ToArray()), operator_pos, relationalOperator);
            }
            GotoFollowers(followers);
        }
        return result;
    }
    KeyWord? Operator(List<KeyWord> operators, params KeyWord[] followers)
    {
        if (!Belong(operators.ToArray()))
            // 183: запрещенная в данном контексте операция
            Skip(183, followers.Concat(operators).ToArray());
        KeyWord? operator_res = null;
        if (Belong(operators.ToArray()))
        {
            operator_res = cur_token.Code;
            Accept(operators.ToArray());
            GotoFollowers(followers);
        }
        return operator_res;
    }
    // <простое выражение>::=<знак><слагаемое>{<аддитивная операция><слагаемое>}
    VariableType SimpleExpression(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        List<KeyWord> starters = new List<KeyWord>()
        {
            KeyWord.plus,
            KeyWord.minus,
            KeyWord.identsy,
            KeyWord.leftpar,
            KeyWord.constboolean,
            KeyWord.constint,
            KeyWord.constreal,
            KeyWord.conststring,
            KeyWord.notsy,
        };
        if (!Belong(starters.ToArray()))
            // 144: недопустимый тип выражения
            Skip(144, followers.Concat(starters).ToArray());
        VariableType result = VariableType.vartUndef;
        if (Belong(starters.ToArray()))
        {
            // if (!EOF())
            //     switch (cur_token.Code)
            //     {
            //         case KeyWord.plus:
            //         case KeyWord.minus:
            //         case KeyWord.constint:
            //         case KeyWord.constreal:
            bool unary_sign = false;
            // если сейчас не унарный знак, то эта позиция не будет использована в любом случае
            Position unary_sign_pos = cur_token.Position;
            if (cur_token.Code == KeyWord.plus || cur_token.Code == KeyWord.minus)
            {
                Accept(cur_token.Code);
                unary_sign = true;
            }
            result = OperationType(unary_sign, unary_sign_pos, Term(followers.Concat(addingOperators).ToArray()));
            while (!EOF() && addingOperators.Contains(cur_token.Code))
            {
                Position operator_pos = cur_token.Position;
                KeyWord? addingOperator = Operator(addingOperators, followers.Concat(starters).ToArray());
                result = OperationType(result, Term(followers.Concat(addingOperators).ToArray()), operator_pos, addingOperator);
            }
            //         break;
            // }
            GotoFollowers(followers);
        }
        return result;
    }
    // <слагаемое>::=<множитель>{<мультипликативная операция><множитель>}
    VariableType Term(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        List<KeyWord> starters = new List<KeyWord>()
        {
            KeyWord.identsy,
            KeyWord.leftpar,
            KeyWord.constboolean,
            KeyWord.constint,
            KeyWord.constreal,
            KeyWord.conststring,
            KeyWord.notsy,
        };
        if (!Belong(starters.ToArray()))
            // 144: недопустимый тип выражения
            Skip(144, followers.Concat(starters).ToArray());
        VariableType result = VariableType.vartUndef;
        if (Belong(starters.ToArray()))
        {
            result = Factor(followers.Concat(multiplyingOperators).ToArray());
            while (!EOF() && multiplyingOperators.Contains(cur_token.Code))
            {
                Position operator_pos = cur_token.Position;
                KeyWord? multiplyingOperator = Operator(multiplyingOperators, followers.Concat(starters).ToArray());
                result = OperationType(result, Factor(followers.Concat(multiplyingOperators).ToArray()), operator_pos, multiplyingOperator);
            }
            GotoFollowers(followers);
        }
        return result;
    }
    // <множитель>::=<переменная>|
    //               <константа без знака>|
    //               (<выражение>)|
    ////               <обозначение функции>|
    ////               <множество>|
    //               not <множитель>
    // <константа без знака>::=<число без знака>|
    //                         <строка>|
    //                         <имя константы>|
    ////                       nil
    VariableType Factor(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        List<KeyWord> starters = new List<KeyWord>(){
            KeyWord.identsy,
            KeyWord.constboolean,
            KeyWord.constint,
            KeyWord.constreal,
            KeyWord.conststring,
            KeyWord.leftpar,
            KeyWord.notsy,
        };
        if (!Belong(starters.ToArray()))
            // 144: недопустимый тип выражения
            Skip(144, followers.Concat(starters).ToArray());
        VariableType result = VariableType.vartUndef;
        if (Belong(starters.ToArray()))
        {
            switch (cur_token.Code)
            {
                case KeyWord.identsy:
                    result = GetIdentType();
                    Accept(KeyWord.identsy);
                    break;
                case KeyWord.constboolean:
                    result = VariableType.vartBoolean;
                    Accept(KeyWord.constboolean);
                    break;
                case KeyWord.constint:
                    result = VariableType.vartInteger;
                    Accept(KeyWord.constint);
                    break;
                case KeyWord.constreal:
                    result = VariableType.vartReal;
                    Accept(KeyWord.constreal);
                    break;
                case KeyWord.conststring:
                    result = VariableType.vartString;
                    Accept(cur_token.Code);
                    break;
                case KeyWord.leftpar:
                    Accept(KeyWord.leftpar);
                    result = Expression(followers.Append(KeyWord.rightpar).ToArray());
                    Accept(KeyWord.rightpar);
                    break;
                case KeyWord.notsy:
                    Position for_error = cur_token.Position;
                    Accept(KeyWord.notsy);
                    result = Factor(followers);
                    if (!LogicalTest(result))
                        // 210: операнды AND, NOT, OR должны быть булевыми
                        iomodule.AddError(for_error, (uint)210);
                    break;
            }
            GotoFollowers(followers);
        }
        return result;
    }
    // // <выбирающий оператор>::=<условный оператор>|
    // //                         <оператор варианта>
    // <условный оператор>::= if <выражение> then <оператор>|
    //                        if <выражение> then <оператор> else <оператор>
    void IfStatement(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        if (!Belong(KeyWord.ifsy))
            Skip(56, followers.Append(KeyWord.ifsy).ToArray());
        if (Belong(KeyWord.ifsy))
        {
            Accept(KeyWord.ifsy);
            Position expr_start = cur_token == null ? iomodule.Position : cur_token.Position;
            bool is_logic = LogicalTest(
                Expression(followers.Append(KeyWord.thensy)
                                    .Append(KeyWord.elsesy).ToArray()));
            if (!is_logic)
                //135: тип операнда должен быть BOOLEAN
                iomodule.AddError(expr_start, (uint)135);
            Accept(KeyWord.thensy);
            Statement(followers.Append(KeyWord.elsesy).ToArray());
            if (!EOF() && cur_token.Code == KeyWord.elsesy)
            {
                Accept(KeyWord.elsesy);
                Statement(followers);
            }
            GotoFollowers(followers);
        }
    }
    // <оператор цикла>::=<цикл с предусловием>|
    // //                 <цикл с постусловием>|
    // //                 <цикл с параметром>
    // <цикл с предусловием>::= while <выражение> do <оператор>
    void WhileStatement(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        if (!Belong(KeyWord.whilesy))
            Skip(76, followers.Append(KeyWord.whilesy).ToArray());
        if (Belong(KeyWord.whilesy))
        {
            Accept(KeyWord.whilesy);
            Position expr_start = cur_token == null ? iomodule.Position : cur_token.Position;
            bool is_logic = LogicalTest(
                Expression(followers.Append(KeyWord.dosy).ToArray()));
            if (!is_logic)
                //135: тип операнда должен быть BOOLEAN
                iomodule.AddError(expr_start, (uint)135);
            Accept(KeyWord.dosy);
            Statement(followers);
            GotoFollowers(followers);
        }
    }
    #endregion StatementPart
}
