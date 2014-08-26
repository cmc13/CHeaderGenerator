using CHeaderGenerator.Data;
using CHeaderGenerator.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CHeaderGenerator.Parser.C
{
    public class CParser : IParser<CSourceFile>
    {
        #region Private Data Members

        private static readonly Logger log = LogManager.GetCurrentClassLogger();
        private readonly ICLexer lexer;

        #endregion

        #region Public Constructor Definition

        public CParser(ICLexer lexer)
        {
            if (lexer == null)
                throw new ArgumentNullException("lexer");

            this.lexer = lexer;
        }

        #endregion

        #region Public Function Definitions

        public CSourceFile PerformParse()
        {
            var sf = new CSourceFile();

            log.Trace("Beginning parse");

            Token<CTokenType> token = null;
            while (this.lexer.HasMoreTokens)
            {
                try
                {
                    token = this.lexer.GetNextToken();
                    if (token != null)
                    {
                        switch (token.Type)
                        {
                            case CTokenType.PP_SYMBOL:
                                this.ParsePPDirective(token, sf);
                                break;

                            case CTokenType.PUNCTUATOR:
                                if (token.Value.Equals("("))
                                {
                                    foreach (var decl in this.ParseDeclaration(token))
                                        sf.AddDeclaration(decl);
                                }
                                else if (token.Value.Equals("{"))
                                    this.ParseUntil(CTokenType.PUNCTUATOR, "}");
                                break;

                            case CTokenType.KEYWORD:
                            case CTokenType.SYMBOL:
                            case CTokenType.ENUM_SPECIFIER:
                            case CTokenType.STRUCTURE_SPECIFIER:
                            case CTokenType.TYPE_SPECIFIER:
                            case CTokenType.TARGET_IDENTIFIER:
                                foreach (var decl in this.ParseDeclaration(token))
                                    sf.AddDeclaration(decl);
                                break;

                            default:
                                log.Trace("Unexpected token encountered at position {0} on line {1}.",
                                    token.PositionInLine, token.LineNumber);
                                throw new InvalidTokenException(token);
                        }
                    }
                }
                catch (InvalidTokenException ex)
                {
                    Token exToken = ex.Token ?? token;
                    string message = string.Format("Failed to parse file. An invalid token was encountered. {0} at position {1} on line {2}.",
                        ex.Message, exToken.PositionInLine, exToken.LineNumber);
                    throw new ParserException(message, ex, exToken.PositionInLine, exToken.LineNumber);
                }
                catch (Exception ex)
                {
                    var messageBuilder = new StringBuilder("Failed to parse file.");
                    if (token != null)
                        messageBuilder.AppendFormat(" The error occurred at position {0} on line {1}.", token.PositionInLine, token.LineNumber);

                    string message = messageBuilder.ToString();
                    log.Error(message, ex);
                    if (token != null)
                        throw new ParserException(message, ex, token.PositionInLine, token.LineNumber);
                    else
                        throw new ParserException(message, ex, 0, 0);
                }
            }

            log.Trace("Parse completed successfully");

            return sf;
        }

        #endregion

        #region Parse Basic Declaration

        private IEnumerable<Declaration> ParseDeclaration(Token<CTokenType> firstToken)
        {
            Token<CTokenType> token;
            DeclarationSpecifiers declSpec = new DeclarationSpecifiers();

            log.Trace("Parsing declarations...");

            token = this.ParseDeclarationSpecifiers(firstToken, declSpec);

            if (token != null)
            {
                if (token.Type == CTokenType.SYMBOL
                    || token.Type == CTokenType.TARGET_IDENTIFIER
                    || (token.Type == CTokenType.PUNCTUATOR
                        && (token.Value.Equals("*") || token.Value.Equals("("))))
                {
                    foreach (var decl in this.ParseInitDeclaratorList(token, declSpec))
                    {
                        var messageBuilder = new StringBuilder("Successfully parsed declaration");
                        if (decl.Declarator != null
                            && decl.Declarator.DirectDeclarator != null)
                            messageBuilder.AppendFormat(" ({0})", decl.Declarator.DirectDeclarator.Identifier);
                        log.Trace(messageBuilder.ToString());
                        yield return decl;
                    }

                    // consume end token
                    token = this.lexer.GetNextToken();
                    if (token != null)
                    {
                        if (token.Type == CTokenType.TERMINATOR)
                        {
                            // good
                        }
                        else if (token.Type == CTokenType.PUNCTUATOR
                            && token.Value.Equals("{"))
                        {
                            // ignore function definition
                            this.ParseUntil(CTokenType.PUNCTUATOR, "}");
                        }
                        else if (token.Type == CTokenType.PP_SYMBOL)
                        {
                            this.lexer.PushToken(token);
                        }
                        else
                        {
                            log.Trace("Missing terminating token for declaration at position {0} on line {1}",
                                token.PositionInLine, token.LineNumber);
                            throw new InvalidTokenException("Missing declaration terminator", token);
                        }
                    }
                    else
                    {
                        log.Trace("Unterminated declaration detected at position {0} on line {1}",
                            firstToken.PositionInLine, firstToken.LineNumber);
                        throw new InvalidTokenException("Unfinished declaration", firstToken);
                    }
                }
                else if (token.Type == CTokenType.TERMINATOR)
                {
                    var decl = new Declaration { DeclarationSpecifiers = declSpec };
                    log.Trace("Successfully parsed declaration ({0})", decl.ToString());
                    yield return decl;
                }
            }
            else
            {
                log.Trace("Unfinished declaration encountered at position {0} on line {1}", firstToken.PositionInLine, firstToken.LineNumber);
                throw new InvalidTokenException("Unfinished declaration", firstToken);
            }
        }

        private IEnumerable<Declaration> ParseInitDeclaratorList(Token<CTokenType> firstToken, DeclarationSpecifiers declSpec)
        {
            Token<CTokenType> token = firstToken;
            int count = 0;

            while (count == 0 || (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals(",")))
            {
                if (count++ > 0 && token.Type == CTokenType.PUNCTUATOR && token.Value.Equals(","))
                    token = this.lexer.GetNextToken();

                Declarator declarator;
                token = this.ParseInitDeclarator(token, out declarator);
                var decl = new Declaration
                {
                    DeclarationSpecifiers = declSpec,
                    Declarator = declarator
                };

                log.Trace("Parsed declaration: {0}", decl);
                yield return decl;
            }

            if (token.Type == CTokenType.TERMINATOR
                || token.Type == CTokenType.PP_SYMBOL
                || (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals("{")))
                this.lexer.PushToken(token);
        }

        private Token<CTokenType> ParseInitDeclarator(Token<CTokenType> firstToken, out Declarator decl)
        {
            decl = new Declarator();
            this.ParseDeclarator(firstToken, decl);

            Token<CTokenType> token = this.lexer.GetNextToken();
            if (token != null)
            {
                if (token.Type == CTokenType.PUNCTUATOR
                    && token.Value.Equals("="))
                {
                    decl.Initializer = this.ParseConstantExpression(token, ",;", false);
                    token = this.lexer.GetNextToken();
                }
            }

            return token;
        }

        private void ParseDeclarator(Token<CTokenType> firstToken, Declarator declarator)
        {
            log.Trace("Attempting to parse declarator at position {0} on line {1}", firstToken.PositionInLine, firstToken.LineNumber);

            Token<CTokenType> copyToken = firstToken;
            declarator.Pointer = this.ParsePointer(ref firstToken);
            if (firstToken == null)
            {
                log.Trace("Invalid pointer declaration encountered at position {0} on line {1}",
                    copyToken.PositionInLine, copyToken.LineNumber);
                throw new InvalidTokenException("Invalid pointer declaration", copyToken);
            }
            declarator.DirectDeclarator = this.ParseDirectDeclarator(firstToken);
        }

        private Pointer ParsePointer(ref Token<CTokenType> token)
        {
            Pointer pointer = null;

            log.Trace("Attempting to parse pointer at position {0} on line {1}", token.PositionInLine, token.LineNumber);

            if (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals("*"))
            {
                pointer = new Pointer();
                token = this.lexer.GetNextToken();
                if (token != null)
                {
                    if (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals("*"))
                        pointer.InnerPointer = this.ParsePointer(ref token);
                }
                else
                {
                    log.Trace("Unfinished pointer declarator encountered at position {0} on line {1}",
                        token.PositionInLine, token.LineNumber);
                    throw new InvalidTokenException("Unfinished pointer declarator", token);
                }

                while (token.Type == CTokenType.KEYWORD && TypeQualifier.IsTypeQualifierKeyword(token.Value))
                {
                    pointer.TypeQualifiers |= ParseTypeQualifier(token);
                    token = this.lexer.GetNextToken();
                }
            }

            log.Trace("Pointer successfully parsed ({0})", pointer);

            return pointer;
        }

        private DirectDeclarator ParseDirectDeclarator(Token<CTokenType> token)
        {
            DirectDeclarator decl = null;

            if (token != null)
            {
                log.Trace("Attempting to parse direct declarator at position {0} on line {1}", token.PositionInLine, token.LineNumber);

                switch (token.Type)
                {
                    case CTokenType.SYMBOL:
                    case CTokenType.TARGET_IDENTIFIER:
                        decl = new DirectDeclarator { Identifier = token.Value };
                        break;

                    case CTokenType.PUNCTUATOR:
                        if (token.Value.Equals("("))
                        {
                            var tokenStack = new Stack<Token<CTokenType>>();
                            Token<CTokenType> peekToken = this.lexer.GetNextToken();
                            tokenStack.Push(peekToken);
                            int parenCount = 1;
                            bool isParamList = false;
                            int symbolCount = 0;
                            while (peekToken != null && parenCount > 0 && !isParamList)
                            {
                                if (peekToken.Type == CTokenType.PUNCTUATOR)
                                {
                                    if (peekToken.Value.Equals(")"))
                                        parenCount--;
                                    else if (peekToken.Value.Equals("("))
                                        parenCount++;
                                    else if (peekToken.Value.Equals(",")
                                        && parenCount == 1)
                                        isParamList = true;
                                    else
                                    {
                                        peekToken = this.lexer.GetNextToken();
                                        tokenStack.Push(peekToken);
                                    }
                                }
                                else if (peekToken.Type == CTokenType.KEYWORD
                                    || peekToken.Type == CTokenType.TYPE_SPECIFIER
                                    || peekToken.Type == CTokenType.ENUM_SPECIFIER
                                    || peekToken.Type == CTokenType.STRUCTURE_SPECIFIER)
                                    isParamList = true;
                                else if (peekToken.Type == CTokenType.SYMBOL)
                                {
                                    if (symbolCount > 0)
                                        isParamList = true;
                                    else
                                        symbolCount++;
                                }
                                else
                                {
                                    peekToken = this.lexer.GetNextToken();
                                    tokenStack.Push(peekToken);
                                }
                            }

                            if (parenCount == 0 && symbolCount == 0)
                                isParamList = true;

                            while (tokenStack.Count > 0)
                                this.lexer.PushToken(tokenStack.Pop());

                            if (isParamList)
                                decl = this.ParseFunctionDeclarator(ref token, null);
                            else
                                decl = ParseParenthesizedDeclarator(token, decl);
                        }
                        else if (token.Value.Equals("["))
                        {
                            var arrayDecl = new ArrayDeclarator();
                            arrayDecl.ArraySizeExpression = ParseConstantExpression(token, "]");
                            decl = arrayDecl;
                        }
                        else if (token.Value.Equals(","))
                        {
                            this.lexer.PushToken(token);
                            return decl;
                        }
                        else
                            this.lexer.PushToken(token);
                        break;

                    default:
                        this.lexer.PushToken(token);
                        break;
                }

                while (decl != null)
                {
                    token = this.lexer.GetNextToken();
                    if (token.Type == CTokenType.PUNCTUATOR)
                    {
                        if (token.Value.Equals("["))
                        {
                            var arrayDecl = new ArrayDeclarator();
                            arrayDecl.Declarator = decl;
                            arrayDecl.ArraySizeExpression = this.ParseConstantExpression(token, "]");

                            decl = arrayDecl;
                        }
                        else if (token.Value.Equals("("))
                        {
                            decl = this.ParseFunctionDeclarator(ref token, decl);
                        }
                        else
                        {
                            this.lexer.PushToken(token);
                            break;
                        }
                    }
                    else
                    {
                        this.lexer.PushToken(token);
                        break;
                    }
                }
            }
            else
            {
                log.Trace("Missing direct declarator encountered");
                throw new InvalidTokenException("Missing direct declarator");
            }

            log.Trace("Successfully parsed direct declarator ({0})", decl);

            return decl;
        }

        private DirectDeclarator ParseParenthesizedDeclarator(Token<CTokenType> token, DirectDeclarator decl)
        {
            var parenthesizedDecl = new ParenthesizedDeclarator { Declarator = new Declarator() };
            Token<CTokenType> nextToken = this.lexer.GetNextToken();
            if (nextToken != null)
                this.ParseDeclarator(nextToken, parenthesizedDecl.Declarator);
            else
            {
                log.Trace("Unfinished parenthesized declarator encountered at position {0} on line {1}", token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Unfinished parenthesized declarator", token);
            }

            decl = parenthesizedDecl;

            var endToken = this.lexer.GetNextToken();
            if (endToken != null)
            {
                if (endToken.Type != CTokenType.PUNCTUATOR || !endToken.Value.Equals(")"))
                {
                    log.Trace("Invalid terminating token for parenthesized declarator at positiong {0} on line {1}",
                        endToken.PositionInLine, endToken.LineNumber);
                    throw new InvalidTokenException(endToken);
                }
            }
            else
            {
                log.Trace("Incomplete parenthesized declarator encountered at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Incomplete declarator", token);
            }
            return decl;
        }

        private DirectDeclarator ParseFunctionDeclarator(ref Token<CTokenType> token, DirectDeclarator decl)
        {
            var functionDecl = new FunctionDeclarator();
            functionDecl.Declarator = decl;
            token = this.lexer.GetNextToken();
            if (token != null)
            {
                log.Trace("Attempting to parse function declarator at position {0} on line {1}", token.PositionInLine, token.LineNumber);

                bool isIdentifierList = IsIdentifierList(token);

                this.lexer.PushToken(token);

                // parse if parameter list is not empty.
                if (token.Type != CTokenType.PUNCTUATOR || !token.Value.Equals(")"))
                {
                    if (isIdentifierList)
                        this.ParseIdentifierList(functionDecl.ParameterTypeList);
                    else
                        this.ParseParameterTypeList(functionDecl.ParameterTypeList);
                }

                // consume parameter list terminator token
                var nextToken = this.lexer.GetNextToken();
                if (nextToken != null)
                {
                    if (nextToken.Type != CTokenType.PUNCTUATOR
                        || !nextToken.Value.Equals(")"))
                    {
                        // shouldn't happen
                        log.Trace("Invalid parameter list terminator at position {0} on line {1}",
                            nextToken.PositionInLine, nextToken.LineNumber);
                        throw new InvalidTokenException(nextToken, ")");
                    }
                }
                else
                {
                    log.Trace("Incomplete function declaration encountered at position {0} on line {1}",
                        token.PositionInLine, token.LineNumber);
                    throw new InvalidTokenException("Incomplete function declaration", token);
                }

                if (isIdentifierList)
                    this.ParseKAndRParameters(token, functionDecl);
            }
            else
            {
                log.Trace("Incomplete function declarator encountered");
                throw new InvalidTokenException("Incomplete function declaration");
            }

            log.Trace("Successfully parsed function declarator ({0})", functionDecl);

            return functionDecl;
        }

        private bool IsIdentifierList(Token<CTokenType> token)
        {
            bool isIdentifierList = false;
            if (token.Type == CTokenType.SYMBOL)
            {
                var peekToken = this.lexer.GetNextToken();
                if (peekToken != null)
                {
                    if (peekToken.Type == CTokenType.PUNCTUATOR
                        && (peekToken.Value.Equals(",") || peekToken.Value.Equals(")")))
                    {
                        isIdentifierList = true;
                    }

                    this.lexer.PushToken(peekToken);
                }
                else
                {
                    log.Trace("Incomplete function declaration encountered at position {0} on line {1}",
                        token.PositionInLine, token.LineNumber);
                    throw new InvalidTokenException("Incomplete function declaration", token);
                }
            }

            return isIdentifierList;
        }

        private void ParseKAndRParameters(Token<CTokenType> lastToken, FunctionDeclarator functionDecl)
        {
            Token<CTokenType> token;
            do
            {
                log.Trace("Attempting to parse K & R parameters at position {0} on line {1}", lastToken.PositionInLine, lastToken.LineNumber);

                token = this.lexer.GetNextToken();
                if (token != null)
                {
                    if (token.Type == CTokenType.KEYWORD
                        || token.Type == CTokenType.SYMBOL
                        || token.Type == CTokenType.TYPE_SPECIFIER
                        || token.Type == CTokenType.ENUM_SPECIFIER
                        || token.Type == CTokenType.STRUCTURE_SPECIFIER)
                    {
                        var declList = this.ParseDeclaration(token);
                        foreach (var pDecl in declList)
                        {
                            var existingParam = functionDecl.ParameterTypeList.FirstOrDefault(p => pDecl.Declarator != null
                                && ((p.SpecifierQualifierList.TypeSpecifier == null && pDecl.DeclarationSpecifiers.TypeSpecifier == null)
                                    || (p.SpecifierQualifierList.TypeSpecifier != null && pDecl.DeclarationSpecifiers.TypeSpecifier != null
                                        && p.SpecifierQualifierList.TypeSpecifier.GetType() == pDecl.DeclarationSpecifiers.TypeSpecifier.GetType()))
                                && (p.Declarator != null && p.Declarator.DirectDeclarator != null && pDecl.Declarator.DirectDeclarator != null
                                    && ((Declarator)p.Declarator).DirectDeclarator.Identifier == pDecl.Declarator.DirectDeclarator.Identifier));
                            if (existingParam != null)
                            {
                                existingParam.Declarator = pDecl.Declarator;
                                existingParam.SpecifierQualifierList.TypeQualifiers = pDecl.DeclarationSpecifiers.TypeQualifiers;
                                existingParam.SpecifierQualifierList.TypeSpecifier = pDecl.DeclarationSpecifiers.TypeSpecifier;
                            }
                            else
                            {
                                log.Trace("Missing matching parameter for K&R-style parameter list at position {0} on line {1}",
                                    token.PositionInLine, token.LineNumber);
                                throw new InvalidTokenException("Missing matching parameter for K&R-style parameter list", token);
                            }
                        }
                    }
                    else if (token.Type == CTokenType.TERMINATOR)
                        break;
                    else if (token.Type != CTokenType.PUNCTUATOR || !token.Value.Equals("{"))
                    {
                        log.Trace("Invalid terminator for K&R-style parameter list at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new InvalidTokenException("Invalid terminator for K&R-style parameter list", token);
                    }
                }
                else
                {
                    log.Trace("Incomplete function declaration at position {0} on line {1}",
                        lastToken.PositionInLine, lastToken.LineNumber);
                    throw new InvalidTokenException("Incomplete function declaration", lastToken);
                }
            } while (token.Type != CTokenType.PUNCTUATOR || !token.Value.Equals("{"));

            this.lexer.PushToken(token);
        }

        private void ParseIdentifierList(List<ParameterDeclaration> list)
        {
            Token<CTokenType> token;

            do
            {
                token = this.lexer.GetNextToken();
                if (token != null)
                {
                    if (token.Type == CTokenType.SYMBOL)
                    {
                        var paramDecl = new ParameterDeclaration();
                        paramDecl.Declarator = new Declarator { DirectDeclarator = new DirectDeclarator { Identifier = token.Value } };
                        list.Add(paramDecl);

                        token = this.lexer.GetNextToken();
                    }
                    else
                    {
                        log.Trace("Invalid token encountered while parsing identifier list at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new InvalidTokenException("Invalid token encountered while parsing identifier list", token);
                    }
                }

            } while (token != null && token.Type == CTokenType.PUNCTUATOR && token.Value.Equals(","));

            if (token != null)
                this.lexer.PushToken(token);
        }

        private void ParseParameterTypeList(List<ParameterDeclaration> list)
        {
            Token<CTokenType> token = this.ParseParameterList(list);
            if (token.Type == CTokenType.PUNCTUATOR)
            {
                if (token.Value.Equals(","))
                {
                    string value = this.ParseUntil(CTokenType.PUNCTUATOR, ")", false);
                    if (value.Trim().Equals("..."))
                        list.Add(new EllipsisParameterDeclaration());
                    else
                    {
                        log.Trace("Expected ellipsis parameter, found '{0}' instead at position {1} on line {2}",
                            value, token.PositionInLine, token.LineNumber);
                        throw new InvalidTokenException(token, "...");
                    }
                }
                else
                    this.lexer.PushToken(token);
            }
            else
                this.lexer.PushToken(token);
        }

        private Token<CTokenType> ParseParameterList(List<ParameterDeclaration> list)
        {
            Token<CTokenType> token;
            Token<CTokenType> peekToken = null;

            do
            {
                token = this.ParseParameterDeclaration(list);
                if (token != null && token.Type == CTokenType.PUNCTUATOR && token.Value.Equals(","))
                {
                    peekToken = this.lexer.GetNextToken();
                    this.lexer.PushToken(peekToken);
                }
                else
                    peekToken = null;
            } while (token != null && (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals(",")
                && (peekToken == null || peekToken.Type != CTokenType.PUNCTUATOR || !peekToken.Value.Equals("."))));

            return token;
        }

        private Token<CTokenType> ParseParameterDeclaration(List<ParameterDeclaration> list)
        {
            var paramDecl = new ParameterDeclaration();

            Token<CTokenType> token = this.ParseSpecifierQualifierList(paramDecl.SpecifierQualifierList);

            if (token.Type != CTokenType.PUNCTUATOR || (!token.Value.Equals(",") && !token.Value.Equals(".")
                && !token.Value.Equals(")")))
            {
                BaseDeclarator baseDeclarator;
                token = this.ParseParameterDeclarator(token, out baseDeclarator);
                paramDecl.Declarator = baseDeclarator;

                log.Trace("Parsed parameter declaration ({0})", paramDecl);
                list.Add(paramDecl);
            }
            else if (token.Type == CTokenType.PUNCTUATOR && (token.Value.Equals(",") || token.Value.Equals(")")))
            {
                log.Trace("Parsed parameter declaration ({0})", paramDecl);
                list.Add(paramDecl); // abstract declarator
            }

            return token;
        }

        private Token<CTokenType> ParseSpecifierQualifierList(SpecifierQualifierList specifierQualifierList)
        {
            Token<CTokenType> token = this.lexer.GetNextToken();
            if (token != null)
                return this.ParseSpecifierQualifierList(token, specifierQualifierList);
            else
            {
                log.Trace("Missing specifier/qualifier keyword");
                throw new InvalidTokenException("Expected specifier/qualifier, found end of file instead.");
            }
        }

        private Token<CTokenType> ParseSpecifierQualifierList(Token<CTokenType> token, SpecifierQualifierList specifierQualifierList)
        {
            while (token != null
                && (token.Type == CTokenType.KEYWORD
                    || token.Type == CTokenType.TYPE_SPECIFIER
                    || token.Type == CTokenType.STRUCTURE_SPECIFIER
                    || token.Type == CTokenType.ENUM_SPECIFIER
                    || token.Type == CTokenType.SYMBOL))
            {
                if (token.Type == CTokenType.SYMBOL)
                {
                    Token<CTokenType> peekToken = this.lexer.GetNextToken();

                    if (peekToken.Type == CTokenType.PUNCTUATOR && peekToken.Value.Equals("("))
                    {
                        var tokenStack = new Stack<Token<CTokenType>>();
                        tokenStack.Push(peekToken);
                        peekToken = this.lexer.GetNextToken();
                        tokenStack.Push(peekToken);
                        int parenCount = 1;
                        bool isParamList = false;
                        int symbolCount = 0;
                        while (peekToken != null && parenCount > 0 && !isParamList)
                        {
                            if (peekToken.Type == CTokenType.PUNCTUATOR)
                            {
                                if (peekToken.Value.Equals(")"))
                                    parenCount--;
                                else if (peekToken.Value.Equals("("))
                                    parenCount++;
                                else if (peekToken.Value.Equals(",")
                                    && parenCount == 1)
                                    isParamList = true;
                                else
                                {
                                    peekToken = this.lexer.GetNextToken();
                                    tokenStack.Push(peekToken);
                                }
                            }
                            else if (peekToken.Type == CTokenType.KEYWORD
                                || peekToken.Type == CTokenType.TYPE_SPECIFIER
                                || peekToken.Type == CTokenType.ENUM_SPECIFIER
                                || peekToken.Type == CTokenType.STRUCTURE_SPECIFIER)
                                isParamList = true;
                            else if (peekToken.Type == CTokenType.SYMBOL)
                            {
                                if (symbolCount > 0)
                                    isParamList = true;
                                else
                                    symbolCount++;
                            }
                            else
                            {
                                peekToken = this.lexer.GetNextToken();
                                tokenStack.Push(peekToken);
                            }
                        }

                        if (parenCount == 0 && symbolCount == 0)
                            isParamList = true;

                        while (tokenStack.Count > 0)
                            this.lexer.PushToken(tokenStack.Pop());

                        if (isParamList)
                            break;
                    }
                    else
                    {
                        this.lexer.PushToken(peekToken);
                        if (peekToken.Type == CTokenType.TERMINATOR
                            || (peekToken.Type == CTokenType.PUNCTUATOR
                                && (peekToken.Value.Equals(")")
                                    || peekToken.Value.Equals("[")
                                    || peekToken.Value.Equals(",")
                                    || peekToken.Value.Equals("=")
                                    || peekToken.Value.Equals(";"))))
                            break;
                    }
                }
                this.ParseSpecifierQualifier(token, specifierQualifierList);
                token = this.lexer.GetNextToken();
            }

            if (token != null && specifierQualifierList.TypeSpecifier == null
                && specifierQualifierList.Modifiers.Count > 0)
            {
                specifierQualifierList.TypeSpecifier = new TypeSpecifier { TypeName = specifierQualifierList.Modifiers.Last() };
                specifierQualifierList.Modifiers.RemoveAt(specifierQualifierList.Modifiers.Count - 1);
            }

            return token;
        }

        private void ParseSpecifierQualifier(Token<CTokenType> token, SpecifierQualifierList specifierQualifierList)
        {
            if (token.Type == CTokenType.KEYWORD)
            {
                if (TypeQualifier.IsTypeQualifierKeyword(token.Value))
                    specifierQualifierList.TypeQualifiers |= ParseTypeQualifier(token);
                else if (Utilities.IsStorageClassKeyword(token.Value))
                {
                    var declSpec = specifierQualifierList as DeclarationSpecifiers;
                    if (declSpec != null)
                        declSpec.StorageClass = Utilities.GetStorageClass(token.Value);
                    else
                    {
                        log.Trace("Invalid storage class keyword encountered at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new InvalidTokenException("Invalid storage class keyword encountered", token);
                    }
                }
            }
            else if (token.Type == CTokenType.STRUCTURE_SPECIFIER)
            {
                if (specifierQualifierList.TypeSpecifier == null)
                    specifierQualifierList.TypeSpecifier = this.ParseStructure(token);
                else
                {
                    log.Trace("Invalid structure specifier encountered at position {0} on line {1}",
                        token.PositionInLine, token.LineNumber);
                    throw new InvalidTokenException("Invalid structure specifier encountered", token);
                }
            }
            else if (token.Type == CTokenType.ENUM_SPECIFIER)
            {
                if (specifierQualifierList.TypeSpecifier == null)
                    specifierQualifierList.TypeSpecifier = this.ParseEnum(token);
                else
                {
                    log.Trace("Invalid enum specifier encountered at position {0} on line {1}",
                        token.PositionInLine, token.LineNumber);
                    throw new InvalidTokenException("Invalid enum specifier encountered", token);
                }
            }
            else if (token.Type == CTokenType.TYPE_SPECIFIER)
            {
                ParseTypeSpecifier(token, specifierQualifierList);
            }
            else if (token.Type == CTokenType.SYMBOL)
            {
                specifierQualifierList.Modifiers.Add(token.Value);
            }
            else // shouldn't happen
            {
                log.Trace("Invalid specifier/qualifier encountered at position {0} on line {1}", token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Invalid specifier/qualifier encountered", token);
            }
        }

        private Token<CTokenType> ParseParameterDeclarator(Token<CTokenType> token, out BaseDeclarator baseDeclarator)
        {
            var decl = new Declarator();
            this.ParseDeclarator(token, decl);
            baseDeclarator = decl;

            return this.lexer.GetNextToken();
        }

        private Token<CTokenType> ParseDeclarationSpecifiers(Token<CTokenType> token, DeclarationSpecifiers declarationSpecifiers)
        {
            while (token != null
                && (token.Type == CTokenType.KEYWORD
                    || token.Type == CTokenType.TYPE_SPECIFIER
                    || token.Type == CTokenType.STRUCTURE_SPECIFIER
                    || token.Type == CTokenType.ENUM_SPECIFIER
                    || token.Type == CTokenType.SYMBOL))
            {
                if (token.Type == CTokenType.SYMBOL)
                {
                    Token<CTokenType> peekToken = this.lexer.GetNextToken();

                    if (peekToken.Type == CTokenType.PUNCTUATOR && peekToken.Value.Equals("("))
                    {
                        var tokenStack = new Stack<Token<CTokenType>>();
                        tokenStack.Push(peekToken);
                        peekToken = this.lexer.GetNextToken();
                        tokenStack.Push(peekToken);
                        int parenCount = 1;
                        bool isParamList = false;
                        int symbolCount = 0;
                        while (peekToken != null && parenCount > 0 && !isParamList)
                        {
                            if (peekToken.Type == CTokenType.PUNCTUATOR)
                            {
                                if (peekToken.Value.Equals(")"))
                                    parenCount--;
                                else if (peekToken.Value.Equals("("))
                                    parenCount++;
                                else if (peekToken.Value.Equals(",")
                                    && parenCount == 1)
                                    isParamList = true;
                                else
                                {
                                    peekToken = this.lexer.GetNextToken();
                                    tokenStack.Push(peekToken);
                                }
                            }
                            else if (peekToken.Type == CTokenType.KEYWORD
                                || peekToken.Type == CTokenType.TYPE_SPECIFIER
                                || peekToken.Type == CTokenType.ENUM_SPECIFIER
                                || peekToken.Type == CTokenType.STRUCTURE_SPECIFIER)
                                isParamList = true;
                            else if (peekToken.Type == CTokenType.SYMBOL)
                            {
                                if (symbolCount > 0)
                                    isParamList = true;
                                else
                                    symbolCount++;
                            }
                            else
                            {
                                peekToken = this.lexer.GetNextToken();
                                tokenStack.Push(peekToken);
                            }
                        }

                        if (parenCount == 0 && symbolCount == 0)
                            isParamList = true;

                        while (tokenStack.Count > 0)
                            this.lexer.PushToken(tokenStack.Pop());

                        if (isParamList)
                            break;
                    }
                    else
                    {
                        this.lexer.PushToken(peekToken);
                        if (peekToken.Type == CTokenType.TERMINATOR
                            || (peekToken.Type == CTokenType.PUNCTUATOR
                                && (peekToken.Value.Equals(")")
                                    || peekToken.Value.Equals("[")
                                    || peekToken.Value.Equals(",")
                                    || peekToken.Value.Equals("=")
                                    || peekToken.Value.Equals(";"))))
                            break;
                    }
                }
                this.ParseSpecifierQualifier(token, declarationSpecifiers);
                token = this.lexer.GetNextToken();
            }

            if (token != null && declarationSpecifiers.TypeSpecifier == null
                && declarationSpecifiers.Modifiers.Count > 0)
            {
                declarationSpecifiers.TypeSpecifier = new TypeSpecifier { TypeName = declarationSpecifiers.Modifiers.Last() };
                declarationSpecifiers.Modifiers.RemoveAt(declarationSpecifiers.Modifiers.Count - 1);
            }

            return token;
        }

        private Token<CTokenType> ParseDeclarationSpecifiers(DeclarationSpecifiers declarationSpecifiers)
        {
            Token<CTokenType> token = this.lexer.GetNextToken();
            if (token != null)
                return this.ParseDeclarationSpecifiers(token, declarationSpecifiers);
            else
            {
                log.Trace("Missing declaration specifiers, found end-of-file");
                throw new InvalidTokenException("Expected declaration specifier, found end of file instead.");
            }
        }

        private static void ParseTypeSpecifier(Token<CTokenType> token, SpecifierQualifierList specifierQualifierList)
        {
            if (specifierQualifierList.TypeSpecifier == null)
                specifierQualifierList.TypeSpecifier = new TypeSpecifier();
            else if (specifierQualifierList.TypeSpecifier.GetType() == typeof(TypeSpecifier))
                specifierQualifierList.TypeSpecifier.TypeName += " ";
            else
            {
                log.Trace("Invalid type specifier encountered at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Invalid type specifier encountered", token);
            }

            specifierQualifierList.TypeSpecifier.TypeName += token.Value;
        }

        private static int ParseTypeQualifier(Token<CTokenType> token)
        {
            if (token.Value.Equals("const"))
                return TypeQualifier.Const;
            else if (token.Value.Equals("volatile"))
                return TypeQualifier.Volatile;
            else
            {
                log.Trace("Invalid type qualifier keyword encountered at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Invalid type qualifier keyword encountered", token);
            }
        }

        private string ParseConstantExpression(Token<CTokenType> lastToken, string terminators, bool consume = true)
        {
            var expressionValue = new StringBuilder();

            // Parse constant expression
            Token<CTokenType> token = this.lexer.GetNextToken();

            while (token != null && !terminators.Contains(token.Value))
            {
                expressionValue.Append(token.Value);

                if (token.Type == CTokenType.PUNCTUATOR)
                {
                    if (token.Value.Equals("("))
                        expressionValue.Append(this.ParseUntil(CTokenType.PUNCTUATOR, ")"));
                    else if (token.Value.Equals("{"))
                        expressionValue.Append(this.ParseUntil(CTokenType.PUNCTUATOR, "}"));
                    else if (token.Value.Equals("["))
                        expressionValue.Append(this.ParseUntil(CTokenType.PUNCTUATOR, "]"));
                }

                token = this.lexer.GetNextToken();
            }

            if (token == null)
            {
                log.Trace("Incomplete constant expression encountered at position {0} on line {1}", lastToken.PositionInLine, lastToken.LineNumber);
                throw new InvalidTokenException("Incomplete constant expression", lastToken);
            }

            if (!consume)
                this.lexer.PushToken(token);

            return expressionValue.ToString();
        }

        private string ParseUntil(CTokenType type, string value, bool consume = true)
        {
            var str = new StringBuilder();
            Token<CTokenType> token;
            while ((token = this.lexer.GetNextToken(false)) != null)
            {
                if (token.Type == type && token.Value.Equals(value))
                {
                    if (consume)
                        str.Append(token.Value);
                    break;
                }

                str.Append(token.Value);

                if (token.Type == CTokenType.PUNCTUATOR)
                {
                    if (token.Value.Equals("("))
                        str.Append(this.ParseUntil(CTokenType.PUNCTUATOR, ")"));
                    else if (token.Value.Equals("{"))
                        str.Append(this.ParseUntil(CTokenType.PUNCTUATOR, "}"));
                    else if (token.Value.Equals("["))
                        str.Append(this.ParseUntil(CTokenType.PUNCTUATOR, "]"));
                }
            }

            if (token == null)
            {
                log.Trace("Unexpected end-of-file encountered while searching for '{0}'", value);
                throw new InvalidTokenException(string.Format("Looking for {0}, found end-of-file instead", value));
            }

            if (!consume)
                this.lexer.PushToken(token);

            return str.ToString();
        }

        #endregion

        #region Structure Parsing

        private StructureSpecifier ParseStructure(Token<CTokenType> token)
        {
            log.Trace("Attempting to parse structure specifier at position {0} on line {1}",
                token.PositionInLine, token.LineNumber);

            var structSpec = new StructureSpecifier();
            if (token.Value.Equals("struct"))
                structSpec.StructureType = StructureType.Struct;
            else if (token.Value.Equals("union"))
                structSpec.StructureType = StructureType.Union;
            else
            {
                log.Trace("Invalid structure specifier keyword encountered at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Invalid structure specifier keyword", token);
            }

            Token<CTokenType> nextToken = this.lexer.GetNextToken();
            if (nextToken == null)
            {
                log.Trace("Incomplete structure definition at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Incomplete structure definition", token);
            }

            switch (nextToken.Type)
            {
                case CTokenType.SYMBOL:
                    structSpec.Identifier = nextToken.Value;

                    nextToken = this.lexer.GetNextToken();
                    if (nextToken != null)
                    {
                        if (nextToken.Type == CTokenType.PUNCTUATOR
                            && nextToken.Value.Equals("{"))
                        {
                            structSpec.StructureDeclarationList = new List<StructureDeclarator>();
                            this.ParseStructureDeclarationList(nextToken, structSpec);
                        }
                        else
                            this.lexer.PushToken(nextToken);
                    }
                    else
                    {
                        log.Trace("Incomplete structure definition at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new InvalidTokenException("Incomplete structure definition", token);
                    }
                    break;

                case CTokenType.PUNCTUATOR:
                    if (nextToken.Type == CTokenType.PUNCTUATOR
                        && nextToken.Value.Equals("{"))
                    {
                        structSpec.StructureDeclarationList = new List<StructureDeclarator>();
                        this.ParseStructureDeclarationList(nextToken, structSpec);
                    }
                    else
                    {
                        log.Trace("Invalid structure definition at position {0} on line {1}", nextToken.PositionInLine, nextToken.LineNumber);
                        throw new InvalidTokenException("Invalid structure definition", nextToken);
                    }
                    break;

                default:
                    log.Trace("Invalid structure definition at position {0} on line {1}", nextToken.PositionInLine, nextToken.LineNumber);
                    throw new InvalidTokenException("Invalid structure definition", nextToken);
            }

            log.Trace("Successfully parsed structure specifier ({0})", structSpec);

            return structSpec;
        }

        private void ParseStructureDeclarationList(Token<CTokenType> lastToken, StructureSpecifier structSpec)
        {
            while (this.ParseStructureDeclaration(lastToken, structSpec)) { /* Empty */ }
        }

        private bool ParseStructureDeclaration(Token<CTokenType> lastToken, StructureSpecifier structSpec)
        {
            var prototypeStructDecl = new StructureDeclarator();

            Token<CTokenType> token = this.lexer.GetNextToken();
            if (token != null)
            {
                this.ParseStructureQualifierList(prototypeStructDecl.SpecifierQualifierList, token);
                this.ParseStructDeclaratorList(token, prototypeStructDecl, structSpec);
                token = this.lexer.GetNextToken();
                if (token != null)
                {
                    if (token.Type == CTokenType.TERMINATOR)
                    {
                        token = this.lexer.GetNextToken();
                        if (token != null)
                        {
                            if (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals("}"))
                                return false;
                            else
                            {
                                this.lexer.PushToken(token);
                                return true;
                            }
                        }
                        else
                        {
                            log.Trace("Incomplete structure definition at position {0} on line {1}", lastToken.PositionInLine,
                                lastToken.LineNumber);
                            throw new InvalidTokenException("Incomplete structure definition", lastToken);
                        }
                    }
                    else
                    {
                        log.Trace("Missing terminator for structure definition at position {0} on line {1}", token.PositionInLine,
                            token.LineNumber);
                        throw new InvalidTokenException("Missing terminator for structure definition", token);
                    }
                }
                else
                {
                    log.Trace("Incomplete structure definition at position {0} on line {1}", lastToken.PositionInLine,
                        lastToken.LineNumber);
                    throw new InvalidTokenException("Incomplete structure definition", lastToken);
                }
            }
            else
            {
                log.Trace("Incomplete structure definition at position {0} on line {1}", lastToken.PositionInLine,
                    lastToken.LineNumber);
                throw new InvalidTokenException("Incomplete structure definition", lastToken);
            }
        }

        private void ParseStructDeclaratorList(Token<CTokenType> lastToken, StructureDeclarator prototypeStructDecl, StructureSpecifier structSpec)
        {
            Token<CTokenType> token = lastToken;
            StructureDeclarator structDecl;

            do
            {
                structDecl = this.ParseStructureDeclarator(prototypeStructDecl, token);
                structSpec.StructureDeclarationList.Add(structDecl);

                token = this.lexer.GetNextToken();
                if (token == null)
                {
                    log.Trace("Incomplete structure definition at position {0} on line {1}", lastToken.PositionInLine,
                        lastToken.LineNumber);
                    throw new InvalidTokenException("Incomplete structure definition", lastToken);
                }
            } while (token.Type == CTokenType.PUNCTUATOR && token.Value == ",");

            this.lexer.PushToken(token);
        }

        private StructureDeclarator ParseStructureDeclarator(StructureDeclarator prototypeStructDecl, Token<CTokenType> lastToken)
        {
            log.Trace("Attempting to parse structure declarator at position {0} on line {1}", lastToken.PositionInLine, lastToken.LineNumber);

            var structDecl = new StructureDeclarator();
            structDecl.SpecifierQualifierList.TypeQualifiers = prototypeStructDecl.SpecifierQualifierList.TypeQualifiers;
            structDecl.SpecifierQualifierList.TypeSpecifier = prototypeStructDecl.SpecifierQualifierList.TypeSpecifier;

            Token<CTokenType> token = this.lexer.GetNextToken();
            if (token != null)
            {
                if (token.Type == CTokenType.PUNCTUATOR
                    && token.Value.Equals(":"))
                {
                    structDecl.ConstantExpression = this.ParseConstantExpression(token, ",;");
                }
                else
                {
                    structDecl.Declarator = new Declarator();
                    this.ParseDeclarator(token, structDecl.Declarator);

                    token = this.lexer.GetNextToken();
                    if (token != null)
                    {
                        if (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals(":"))
                            structDecl.ConstantExpression = this.ParseConstantExpression(token, ",;");
                        else
                            this.lexer.PushToken(token);
                    }
                    else
                    {
                        log.Trace("Incomplete structure definition encountered at position {0} on line {1}",
                            lastToken.PositionInLine, lastToken.LineNumber);
                        throw new InvalidTokenException("Incomplete structure definition", lastToken);
                    }
                }
            }
            else
            {
                log.Trace("Incomplete structure definition encountered at position {0} on line {1}",
                    lastToken.PositionInLine, lastToken.LineNumber);
                throw new InvalidTokenException("Incomplete structure definition", lastToken);
            }

            log.Trace("Successfully parsed structure declarator ({0})", structDecl);

            return structDecl;
        }

        private void ParseStructureQualifierList(SpecifierQualifierList specQualList, Token<CTokenType> token)
        {
            bool processQualifierList = true;
            while (processQualifierList)
            {
                switch (token.Type)
                {
                    case CTokenType.KEYWORD:
                        if (TypeQualifier.IsTypeQualifierKeyword(token.Value))
                            specQualList.TypeQualifiers |= ParseTypeQualifier(token);
                        else
                        {
                            log.Trace("Invalid keyword encountered at position {0} on line {1}", token.PositionInLine,
                                token.LineNumber);
                            throw new InvalidTokenException("Invalid keyword encountered", token);
                        }
                        break;

                    case CTokenType.SYMBOL:
                        if (specQualList.TypeSpecifier == null)
                            specQualList.TypeSpecifier = new TypeSpecifier { TypeName = token.Value };
                        else
                            processQualifierList = false;
                        break;

                    case CTokenType.TYPE_SPECIFIER:
                        if (specQualList.TypeSpecifier == null)
                            specQualList.TypeSpecifier = new TypeSpecifier();
                        else if (specQualList.TypeSpecifier.GetType() != typeof(TypeSpecifier))
                            specQualList.TypeSpecifier.TypeName += " ";
                        else
                        {
                            log.Trace("Invalid type specifier encountered at position {0} on line {1}",
                                token.PositionInLine, token.LineNumber);
                            throw new InvalidTokenException("Invalid type specifier encountered", token);
                        }

                        specQualList.TypeSpecifier.TypeName += token.Value;
                        break;

                    case CTokenType.STRUCTURE_SPECIFIER:
                        if (specQualList.TypeSpecifier == null)
                            specQualList.TypeSpecifier = this.ParseStructure(token);
                        else
                        {
                            log.Trace("Invalid structure specifier encountered at position {0} on line {1}",
                                token.PositionInLine, token.LineNumber);
                            throw new InvalidTokenException("Invalid structure specifier encountered", token);
                        }
                        break;

                    case CTokenType.ENUM_SPECIFIER:
                        if (specQualList.TypeSpecifier == null)
                            specQualList.TypeSpecifier = this.ParseEnum(token);
                        else
                        {
                            log.Trace("Invalid enum specifier encountered at position {0} on line {1}",
                                token.PositionInLine, token.LineNumber);
                            throw new InvalidTokenException("Invalid enum specifier encountered", token);
                        }
                        break;

                    default:
                        processQualifierList = false;
                        break;
                }

                if (processQualifierList)
                {
                    Token<CTokenType> testToken = this.lexer.GetNextToken();
                    if (testToken == null)
                    {
                        log.Trace("Incomplete structure definition at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new InvalidTokenException("Incomplete structure definition", token);
                    }
                    token = testToken;
                }
            }

            this.lexer.PushToken(token);
        }

        #endregion

        #region Enum Parsing

        private EnumSpecifier ParseEnum(Token<CTokenType> token)
        {
            var enumSpec = new EnumSpecifier();

            log.Trace("Attempting to parse enum definition at position {0} on line {1}",
                token.PositionInLine, token.LineNumber);

            Token<CTokenType> nextToken = this.lexer.GetNextToken();
            if (nextToken == null)
            {
                log.Trace("Incomplete enum definition at position {0} on line {1}", token.PositionInLine,
                    token.LineNumber);
                throw new InvalidTokenException("Incomplete enum definition", token);
            }

            switch (nextToken.Type)
            {
                case CTokenType.SYMBOL:
                    enumSpec.Identifier = nextToken.Value;
                    nextToken = this.lexer.GetNextToken();
                    if (nextToken != null)
                    {
                        this.lexer.PushToken(nextToken);
                        if (nextToken.Type == CTokenType.PUNCTUATOR
                            && nextToken.Value.Equals("{"))
                            this.ParseEnumList(enumSpec);
                    }
                    else
                    {
                        log.Trace("Incomplete enum definition at position {0} on line {1}",
                            token.PositionInLine, token.LineNumber);
                        throw new InvalidTokenException("Incomplete enum definition", token);
                    }
                    break;

                case CTokenType.PUNCTUATOR:
                    if (nextToken.Value.Equals("{"))
                    {
                        this.lexer.PushToken(nextToken);
                        this.ParseEnumList(enumSpec);
                    }
                    else
                    {
                        log.Trace("Invalid enum definition at position {0} on line {1}", nextToken.PositionInLine,
                            nextToken.LineNumber);
                        throw new InvalidTokenException(nextToken, "{");
                    }
                    break;

                default:
                    log.Trace("Invalid enum definition at position {0} on line {1}", nextToken.PositionInLine,
                        nextToken.LineNumber);
                    throw new InvalidTokenException("Invalid enum definition", nextToken);
            }

            log.Trace("Successfully parsed enum specifier ({0})", enumSpec);

            return enumSpec;
        }

        private void ParseEnumList(EnumSpecifier enumSpec)
        {
            Token<CTokenType> token = this.lexer.GetNextToken();
            if (token.Type == CTokenType.PUNCTUATOR
                && token.Value.Equals("{"))
            {
                while (this.ParseEnumerator(token, enumSpec))
                {
                    // Do nothing
                }
            }
            else
            {
                log.Trace("Invalid enum list definition at position {0} on line {1}", token.PositionInLine,
                    token.LineNumber);
                throw new InvalidTokenException("Invalid enum list definition", token);
            }
        }

        private bool ParseEnumerator(Token<CTokenType> lastToken, EnumSpecifier enumSpec)
        {
            Token<CTokenType> token = this.lexer.GetNextToken();
            if (token != null && token.Type == CTokenType.SYMBOL)
            {
                var enumValue = new Enumerator { Identifier = token.Value };
                token = this.lexer.GetNextToken();
                if (token != null && token.Type == CTokenType.PUNCTUATOR)
                {
                    if (enumSpec.EnumeratorList == null)
                        enumSpec.EnumeratorList = new List<Enumerator>();

                    if (token.Value.Equals("="))
                    {
                        enumValue.ConstantExpression = this.ParseConstantExpression(lastToken, ",}", false);
                        enumSpec.EnumeratorList.Add(enumValue);
                        token = this.lexer.GetNextToken();
                        if (token != null)
                        {
                            if (token.Type == CTokenType.PUNCTUATOR)
                            {
                                if (token.Value.Equals(","))
                                    return true;
                                else if (token.Value.Equals("}"))
                                    return false;
                                else
                                {
                                    log.Trace("Invalid enum value separator at position {0} on line {1}",
                                        token.PositionInLine, token.LineNumber);
                                    throw new InvalidTokenException(token);
                                }
                            }
                            else
                            {
                                log.Trace("Invalid enum definition at position {0} on line {1}", token.PositionInLine,
                                    token.LineNumber);
                                throw new InvalidTokenException("Invalid enum definition", token);
                            }
                        }
                        else
                        {
                            log.Trace("Incomplete enum value definiton at position {0} on line {1}",
                                lastToken.PositionInLine, lastToken.LineNumber);
                            throw new InvalidTokenException("Incomplete enum definition", lastToken);
                        }
                    }
                    else
                    {
                        enumSpec.EnumeratorList.Add(enumValue);
                        if (token.Value.Equals(","))
                            return true;
                        else if (token.Value.Equals("}"))
                            return false;
                        else
                        {
                            log.Trace("Invalid enum value separator at position {0} on line {1}",
                                token.PositionInLine, token.LineNumber);
                            throw new InvalidTokenException(token);
                        }
                    }
                }
                else if (token != null)
                {
                    log.Trace("Invalid enum definition at positiong {0} on line {1}",
                        token.PositionInLine, token.LineNumber);
                    throw new InvalidTokenException("Invalid enum definition", token);
                }
                else
                {
                    log.Trace("Incomplete enum definition at position {0} on line {1}",
                        lastToken.PositionInLine, lastToken.LineNumber);
                    throw new InvalidTokenException("Incomplete enum definition", lastToken);
                }
            }
            else if (token != null)
            {
                log.Trace("Invalid enum value at position {0} on line {1}", token.PositionInLine,
                    token.LineNumber);
                throw new InvalidTokenException("Invalid enum definition", token);
            }
            else
            {
                log.Trace("Incomplete enum definition encountered at position {0} on line {1}",
                    lastToken.PositionInLine, lastToken.LineNumber);
                throw new InvalidTokenException("Incomplete enum definition", lastToken);
            }
        }

        #endregion

        #region Preprocessor Parsing

        private string ProcessPPDirectiveString(Token<CTokenType> currentToken)
        {
            Token<CTokenType> token = null;
            int lineNumber = currentToken.LineNumber;
            var str = new StringBuilder();

            token = this.lexer.GetNextToken();
            while (str.Length == 0 || str.ToString().Trim().EndsWith(@"\"))
            {
                while (token != null && token.LineNumber == lineNumber)
                {
                    str.Append(token.Value);
                    token = this.lexer.GetNextToken(false);
                }

                if (token != null)
                    lineNumber = token.LineNumber;
                else
                    break;
            }

            if (token != null)
                this.lexer.PushToken(token);

            var ppDirectiveValue = str.ToString().Trim();

            log.Trace("Successfully parsed preprocessor directive ({0})", ppDirectiveValue);

            return ppDirectiveValue;
        }

        private void ParsePPDirective(Token<CTokenType> firstToken, CSourceFile sf)
        {
            Token<CTokenType> token = this.lexer.GetNextToken();
            if (token == null)
            {
                log.Trace("Unfinished preprocessor directive encountered at position {0} on line {1}",
                    firstToken.PositionInLine, firstToken.LineNumber);
                throw new InvalidTokenException("Unfinished preprocessor directive encountered", firstToken);
            }
            else if (token.Type == CTokenType.SYMBOL)
                this.lexer.SkipToNextLine();
            else if (token.Type != CTokenType.PP_DIRECTIVE)
            {
                log.Trace("Invalid preprocessor directive encountered at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Invalid preprocessor directive encountered", token);
            }
            else
            {
                if (token.Value.Equals("include"))
                {
                    token = this.lexer.GetNextToken();
                    if (token != null)
                    {
                        if (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals("<"))
                        {
                            string includeValue = this.ParseUntil(CTokenType.PUNCTUATOR, ">").TrimEnd('>');
                            log.Trace("Successfully parsed include directive ({0})", includeValue);
                            sf.AddInclude(new Include(includeValue, true));
                        }
                        else if (token.Type == CTokenType.STRING_LITERAL)
                        {
                            string includeValue = token.Value.Trim(1, '"');
                            log.Trace("Successfully parsed include directive ({0})", includeValue);
                            sf.AddInclude(new Include(includeValue, false));
                        }
                        else
                        {
                            log.Trace("Invalid include directive encountered at position {0} on line {1}",
                                token.PositionInLine, token.LineNumber);
                            throw new InvalidTokenException(token);
                        }
                    }
                    else
                    {
                        log.Trace("Incomplete include encountered at position {0} on line {1}",
                            firstToken.PositionInLine, firstToken.LineNumber);
                        throw new InvalidTokenException("Incomplete include directive", firstToken);
                    }
                }
                else if (token.Value.Equals("if"))
                {
                    sf.PushIfCondition(ParsePPIfCondition(token));
                }
                else if (token.Value.Equals("elif"))
                {
                    string cond = PopIfCond(sf, token);
                    string newCond = ParsePPIfCondition(token);
                    sf.PushIfCondition(string.Format("!({0}) && ({1})", cond, newCond));
                }
                else if (token.Value.Equals("ifdef"))
                {
                    string cond = this.ProcessPPDirectiveString(token);
                    sf.PushIfCondition(string.Format("defined({0})", cond));
                }
                else if (token.Value.Equals("ifndef"))
                {
                    string cond = this.ProcessPPDirectiveString(token);
                    sf.PushIfCondition(string.Format("!defined({0})", cond));
                }
                else if (token.Value.Equals("else"))
                {
                    string cond = PopIfCond(sf, token);
                    sf.PushIfCondition(string.Format("!({0})", cond));
                }
                else if (token.Value.Equals("endif"))
                {
                    PopIfCond(sf, token);
                }
                else if (token.Value.Equals("define"))
                {
                    token = ParsePPDefinition(firstToken, sf, token);
                }
                else
                {
                    this.lexer.SkipToNextLine();
                }
            }
        }

        private Token<CTokenType> ParsePPDefinition(Token<CTokenType> firstToken, CSourceFile sf, Token<CTokenType> token)
        {
            token = this.lexer.GetNextToken();
            if (token != null)
            {
                if (token.Type == CTokenType.SYMBOL)
                {
                    var defn = new Definition { Identifier = token.Value };
                    var peekToken = this.lexer.GetNextToken(false);
                    if (peekToken != null)
                    {
                        if (peekToken.Type == CTokenType.PUNCTUATOR && peekToken.Value.Equals("("))
                        {
                            defn.Arguments = new List<string>();

                            peekToken = lexer.GetNextToken();
                            if (peekToken != null)
                            {
                                token = peekToken;
                                while (token.Type == CTokenType.SYMBOL)
                                {
                                    defn.Arguments.Add(token.Value);

                                    peekToken = lexer.GetNextToken();
                                    if (peekToken != null)
                                    {
                                        token = peekToken;
                                        if (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals(","))
                                        {
                                            peekToken = lexer.GetNextToken();
                                            if (peekToken != null)
                                            {
                                                token = peekToken;
                                                if (token.Type != CTokenType.SYMBOL)
                                                {
                                                    log.Trace("Invalid preprocessor definition encountered at position {0} on line {1}",
                                                        token.PositionInLine, token.LineNumber);
                                                    throw new InvalidTokenException(token);
                                                }
                                            }
                                            else
                                            {
                                                log.Trace("Unterminated macro function parameter list at position {0} on line {1}",
                                                    token.PositionInLine, token.LineNumber);
                                                throw new InvalidTokenException("Unterminated macro function parameter list", token);
                                            }
                                        }
                                        else if (token.Type != CTokenType.PUNCTUATOR || !token.Value.Equals(")"))
                                        {
                                            log.Trace("Invalid macro function definition at position {0} on line {1}",
                                                token.PositionInLine, token.LineNumber);
                                            throw new InvalidTokenException("Invalid macro function definition", token);
                                        }
                                    }
                                    else
                                    {
                                        log.Trace("Unterminated macro function parameter list at position {0} on line {1}",
                                            token.PositionInLine, token.LineNumber);
                                        throw new InvalidTokenException("Unterminated macro function parameter list", token);
                                    }
                                }
                            }
                            else
                            {
                                log.Trace("Unterminated macro function parameter list at position {0} on line {1}",
                                    token.PositionInLine, token.LineNumber);
                                throw new InvalidTokenException("Unterminated macro function parameter list", token);
                            }

                            if (token != null)
                            {
                                if (token.Type == CTokenType.PUNCTUATOR && token.Value.Equals(")"))
                                {
                                    defn.Replacement = this.ProcessPPDirectiveString(token);
                                    log.Trace("Successfully parsed preprocessor definition ({0})", defn);
                                    sf.AddPreProcessorDefinition(defn);
                                }
                            }
                        }
                        else
                        {
                            this.lexer.PushToken(peekToken);
                            peekToken = this.lexer.GetNextToken();
                            this.lexer.PushToken(peekToken);
                            if (peekToken != null)
                            {
                                if (peekToken.LineNumber == token.LineNumber)
                                    defn.Replacement = this.ProcessPPDirectiveString(token);
                            }

                            log.Trace("Successfully parsed preprocessor definition ({0})", defn);
                            sf.AddPreProcessorDefinition(defn);
                        }
                    }
                    else
                    {
                        log.Trace("Successfully parsed preprocessor definition ({0})", defn);
                        sf.AddPreProcessorDefinition(defn);
                    }
                }
                else
                {
                    log.Trace("Invalid preprocessor definition at position {0} on line {1}",
                        token.PositionInLine, token.LineNumber);
                    throw new InvalidTokenException("Invalid preprocessor definition", token);
                }
            }
            else
            {
                log.Trace("Incomplete preprocessor definition encountered at position {0} on line {1}",
                    firstToken.PositionInLine, firstToken.LineNumber);
                throw new InvalidTokenException("Incomplete preprocessor definition", firstToken);
            }

            return token;
        }

        private static string PopIfCond(CSourceFile sf, Token<CTokenType> token)
        {
            try
            {
                return sf.PopIfCond();
            }
            catch (InvalidOperationException ex)
            {
                log.Trace("Invalid preprocessor directive (no matching '#if') at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Invalid preprocessor directive (no matching '#if')", token, ex);
            }
        }

        private string ParsePPIfCondition(Token<CTokenType> token)
        {
            Token<CTokenType> peekToken = this.lexer.GetNextToken();
            if (peekToken != null)
            {
                this.lexer.PushToken(peekToken);

                string cond = this.ProcessPPDirectiveString(token);
                return cond;
            }
            else
            {
                log.Trace("Unterminated preprocessor if directive encountered at position {0} on line {1}",
                    token.PositionInLine, token.LineNumber);
                throw new InvalidTokenException("Unterminated preprocessor if directive", token);
            }
        }

        #endregion
    }
}