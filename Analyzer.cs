public class Analyzer
{
    private IOModule iomodule;
    private LexicalAnalyzer lexicalAnalyzer;

    CToken? cur_token;

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
            Accept(KeyWord.identsy);
            while (!EOF() && cur_token.Code == KeyWord.comma)
            {
                Accept(KeyWord.comma);
                Accept(KeyWord.identsy);
            }
            Accept(KeyWord.colon);
            Type(followers);
            GotoFollowers(followers);
        }
    }

    void Type(params KeyWord[] followers)
    {
        // if (EOF())
        //     return;
        if (!Belong(KeyWord.integersy, KeyWord.stringsy, KeyWord.realsy, KeyWord.booleansy))
            Skip(10, followers.Append(KeyWord.integersy)
                              .Append(KeyWord.stringsy)
                              .Append(KeyWord.realsy)
                              .Append(KeyWord.booleansy).ToArray());
        if (Belong(KeyWord.integersy, KeyWord.stringsy, KeyWord.realsy, KeyWord.booleansy))
        {
            switch (cur_token.Code)
            {
                case KeyWord.integersy:
                    Accept(KeyWord.integersy);
                    break;
                case KeyWord.stringsy:
                    Accept(KeyWord.stringsy);
                    break;
                case KeyWord.realsy:
                    Accept(KeyWord.realsy);
                    break;
                case KeyWord.booleansy:
                    Accept(KeyWord.booleansy);
                    break;
                    // default:
                    //     // 10:	ошибка в типе
                    //     iomodule.AddError(cur_token.Position, (uint)10);
                    //     break;
            }
            GotoFollowers(followers);
        }
    }
    #endregion VariableDeclarationPart

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
            Accept(KeyWord.identsy);
            Accept(KeyWord.assign);
            Expression(followers);
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
    void Expression(params KeyWord[] followers)
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
        if (Belong(starters.ToArray()))
        {
            SimpleExpression(followers.Concat(relationalOperators).ToArray());
            if (!EOF() && relationalOperators.Contains(cur_token.Code))
            {
                RelationalOperator(followers.Concat(starters).ToArray());
                SimpleExpression(followers.Concat(relationalOperators).ToArray());
            }
            GotoFollowers(followers);
        }
    }

    // <операция отношения>::=
    //                       =|
    //                       <>|
    //                       <|
    //                       <=|
    //                       >=|
    //                       >|
    //                       in
    void RelationalOperator(params KeyWord[] followers)
    {
        if (!Belong(relationalOperators.ToArray()))
            // 183: запрещенная в данном контексте операция
            Skip(183, followers.Concat(relationalOperators).ToArray());
        if (Belong(relationalOperators.ToArray()))
        {
            Accept(relationalOperators.ToArray());
            GotoFollowers(followers);
        }
    }

    // <простое выражение>::=<знак><слагаемое>{<аддитивная операция><слагаемое>}
    void SimpleExpression(params KeyWord[] followers)
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
        if (Belong(starters.ToArray()))
        {
            // if (!EOF())
            //     switch (cur_token.Code)
            //     {
            //         case KeyWord.plus:
            //         case KeyWord.minus:
            //         case KeyWord.constint:
            //         case KeyWord.constreal:
            if (cur_token.Code == KeyWord.plus || cur_token.Code == KeyWord.minus)
                Accept(cur_token.Code);
            Term(followers.Concat(addingOperators).ToArray());
            while (!EOF() && addingOperators.Contains(cur_token.Code))
            {
                AddingOperator(followers.Concat(starters).ToArray());
                Term(followers.Concat(addingOperators).ToArray());
            }
            //         break;
            // }
            GotoFollowers(followers);
        }
    }
    // <аддитивная операция>::= + | - | or
    void AddingOperator(params KeyWord[] followers)
    {
        if (!Belong(addingOperators.ToArray()))
            // 183: запрещенная в данном контексте операция
            Skip(183, followers.Concat(addingOperators).ToArray());
        if (Belong(addingOperators.ToArray()))
        {
            Accept(addingOperators.ToArray());
            GotoFollowers(followers);
        }
    }
    // <слагаемое>::=<множитель>{<мультипликативная операция><множитель>}
    void Term(params KeyWord[] followers)
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
        if (Belong(starters.ToArray()))
        {
            Factor(followers.Concat(multiplyingOperators).ToArray());
            while (!EOF() && multiplyingOperators.Contains(cur_token.Code))
            {
                MultiplyingOperator(followers.Concat(starters).ToArray());
                Factor(followers.Concat(multiplyingOperators).ToArray());
            }
            GotoFollowers(followers);
        }
    }
    // <мультипликативная операция>::=
    //\                                *|
    //                                 /|
    //                                 div|
    //                                 mod|
    //                                 and
    void MultiplyingOperator(params KeyWord[] followers)
    {
        if (!Belong(multiplyingOperators.ToArray()))
            // 183: запрещенная в данном контексте операция
            Skip(183, followers.Concat(multiplyingOperators).ToArray());
        if (Belong(multiplyingOperators.ToArray()))
        {
            Accept(multiplyingOperators.ToArray());
            GotoFollowers(followers);
        }
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
    void Factor(params KeyWord[] followers)
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
        if (Belong(starters.ToArray()))
        {
            switch (cur_token.Code)
            {
                case KeyWord.identsy:
                case KeyWord.constboolean:
                case KeyWord.constint:
                case KeyWord.constreal:
                case KeyWord.conststring:
                    Accept(cur_token.Code);
                    break;
                case KeyWord.leftpar:
                    Accept(KeyWord.leftpar);
                    Expression(followers.Append(KeyWord.rightpar).ToArray());
                    Accept(KeyWord.rightpar);
                    break;
                case KeyWord.notsy:
                    Accept(KeyWord.notsy);
                    Factor(followers);
                    break;
            }
            GotoFollowers(followers);
        }
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
            Expression(followers.Append(KeyWord.thensy)
                                .Append(KeyWord.elsesy).ToArray());
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
            Expression(followers.Append(KeyWord.dosy).ToArray());
            Accept(KeyWord.dosy);
            Statement(followers);
            GotoFollowers(followers);
        }
    }
    #endregion StatementPart
}
