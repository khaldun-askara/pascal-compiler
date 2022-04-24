public class SyntacticAnalyzer
{
    private IOModule iomodule;
    private LexicalAnalyzer lexicalAnalyzer;
    private Position position;

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

    public SyntacticAnalyzer(IOModule iomodule, LexicalAnalyzer lexicalAnalyzer)
    {
        this.iomodule = iomodule;
        this.lexicalAnalyzer = lexicalAnalyzer;
        this.cur_token = lexicalAnalyzer.NextToken();
    }

    void Accept(params KeyWord[] expected_keywords)
    {
        bool is_ok = false;
        if (cur_token != null)
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
            iomodule.AddError(position, expected_keywords.Select(x => (uint)x).ToArray());
    }

    // <программа>::=program <имя>(<имя файла>{,<имя файла>}); <блок>.
    public void Program()
    {
        position = iomodule.Position;
        if (cur_token == null)
        {
            iomodule.AddError(position, (uint)3);
            return;
        }
        Accept(KeyWord.programsy);
        Accept(KeyWord.identsy);
        // кто такие (<имя файла>{,<имя файла>}) я не знаю
        Accept(KeyWord.semicolon);
        Block();
        Accept(KeyWord.point);
    }

    // <блок>: :=<раздел меток>
    ////           <раздел констант>
    //?            <раздел типов>
    //             <раздел переменных>
    ////           <раздел процедур и функций>
    //             <раздел операторов>
    void Block()
    {
        TypeDefinitionPart();
        VariableDeclarationPart();
        StatementPart();
    }

    void TypeDefinitionPart()
    {
        // TODO: что тут делать-то алло
    }

    #region VariableDeclarationPart
    // <раздел переменных> : = var <описание однотипных переменных>; {<описание однотипных переменных>;} 
    //                          | <пусто>
    void VariableDeclarationPart()
    {
        if (cur_token != null && cur_token.Code == KeyWord.varsy)
        {
            Accept(KeyWord.varsy);
            do
            {
                VariableDeclaration();
                Accept(KeyWord.semicolon);
            }
            while (cur_token != null && cur_token.Code == KeyWord.identsy);
        }
    }

    // описание однотипных переменных>::=<имя>{,<имя>} : <тип>
    void VariableDeclaration()
    {
        Accept(KeyWord.identsy);
        while (cur_token != null && cur_token.Code == KeyWord.comma)
        {
            Accept(KeyWord.comma);
            Accept(KeyWord.identsy);
        }
        Accept(KeyWord.colon);
        Type();
    }

    void Type()
    {
        if (cur_token != null)
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
                default:
                    // 10:	ошибка в типе
                    iomodule.AddError(position, (uint)10);
                    break;
            }
    }
    #endregion VariableDeclarationPart

    #region StatementPart
    // <составной оператор>: := begin <оператор>{; <оператор>} end
    void StatementPart()
    {
        Accept(KeyWord.beginsy);
        Statement();
        while (cur_token != null && cur_token.Code == KeyWord.semicolon)
        {
            Accept(KeyWord.semicolon);
            Statement();
        }
        Accept(KeyWord.endsy);
    }


    // <оператор>::=
    //   <непомеченный оператор>|
    //   <метка><непомеченный оператор>
    // <непомеченный оператор>::=<простой оператор>|<сложный оператор>
    // то есть по сути если без меток, то <оператор>::=<простой оператор>|<сложный оператор>
    // оператор присваивания начинается с <переменная>, составной оператор с begin, выбирающий оператор c if, оператор цикла (нам нужен только while) с while
    // ээээ получается можно определять, какой стейтмент в данный момент, исходя из первого токена
    void Statement()
    {
        if (cur_token != null)
            switch (cur_token.Code)
            {
                // <простой оператор>::=<оператор присваивания>|
                ////                    <оператор процедуры>|
                ////                    <оператор перехода>|
                //                      <пустой оператор>
                case KeyWord.identsy:
                    Assignment();
                    break;
                // <сложный оператор>::=<составной оператор>|
                //                      <выбирающий оператор>|
                //                      <оператор цикла>|
                ////                    <оператор присоединения>
                case KeyWord.beginsy:
                    StatementPart();
                    break;
                case KeyWord.ifsy:
                    // TODO вызов if
                    break;
                case KeyWord.whilesy:
                    // TODO while
                    break;
            }
    }
    // <оператор присваивания>::=<переменная>:=<выражение>|
    ////                        <имя функции>:=<выражение>
    void Assignment()
    {
        Accept(KeyWord.identsy);
        Accept(KeyWord.assign);
        Expression();
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
    void Expression()
    {
        SimpleExpression();
        while (cur_token != null && relationalOperators.Contains(cur_token.Code))
        {
            RelationalOperator();
            SimpleExpression();
        }
    }

    void RelationalOperator()
    {
        Accept(relationalOperators.ToArray());
    }

    // <простое выражение>::=<знак><слагаемое>{<аддитивная операция><слагаемое>}
    void SimpleExpression()
    {
        // if (cur_token != null)
        //     switch (cur_token.Code)
        //     {
        //         case KeyWord.plus:
        //         case KeyWord.minus:
        //         case KeyWord.constint:
        //         case KeyWord.constreal:
        if (cur_token != null && (cur_token.Code == KeyWord.plus || cur_token.Code == KeyWord.minus))
            Accept(cur_token.Code);
        Term();
        while (cur_token != null && addingOperators.Contains(cur_token.Code))
        {
            AddingOperator();
            Term();
        }
        //         break;
        // }
    }
    void AddingOperator()
    {
        Accept(addingOperators.ToArray());
    }
    // <слагаемое>::=<множитель>{<мультипликативная операция><множитель>}
    void Term()
    {
        Factor();
        while (cur_token != null && multiplyingOperators.Contains(cur_token.Code))
        {
            MultiplyingOperator();
            Factor();
        }
    }
    void MultiplyingOperator()
    {
        Accept(multiplyingOperators.ToArray());
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
    //                         nil
    void Factor()
    {
        if (cur_token != null)
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
                    Statement();
                    Accept(KeyWord.rightpar);
                    break;
                case KeyWord.notsy:
                    Accept(KeyWord.notsy);
                    Factor();
                    break;
                default:
                    // TODO: пукнуть что-то про некорректное выражение
                    break;
            }
    }
    // <выбирающий оператор>::=<условный оператор>|
    //                         <оператор варианта>

    // <условный оператор>::= if <выражение> then <оператор>|
    //                        if <выражение> then <оператор> else <оператор>
    // <оператор цикла>::=<цикл с предусловием>|
    //                    <цикл с постусловием>|
    //                    <цикл с параметром>

    // <цикл с предусловием>::= while <выражение> do <оператор>

    // <операция отношения>::=
    //                       =|
    //                       <>|
    //                       <|
    //                       <=|
    //                       >=|
    //                       >|
    //                       in

    // <аддитивная операция>::= + | - | or
    // <мультипликативная операция>::=
    //\                                *|
    //                                 /|
    //                                 div|
    //                                 mod|
    //                                 and
    #endregion StatementPart
}
