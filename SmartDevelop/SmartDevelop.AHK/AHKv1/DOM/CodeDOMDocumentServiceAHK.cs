﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartDevelop.TokenizerBase;
using System.CodeDom;
using SmartDevelop.Model.Tokening;
using SmartDevelop.Model.Projecting;
using SmartDevelop.Model.DOM.Types;
using ICSharpCode.AvalonEdit.Document;
using SmartDevelop.Model.DOM.Ranges;
using SmartDevelop.AHK.AHKv1.DOM.Types;
using System.Collections;
using SmartDevelop.Model.Tokenizing;
using System.Threading.Tasks;
using System.ComponentModel;

using System.Windows;
using SmartDevelop.AHK.AHKv1.DOM;
using System.Threading;
using Archimedes.Patterns.Utils;
using System.Diagnostics;


namespace SmartDevelop.Model.DOM
{

    /// <summary>
    ///  CodeDOM Service Impl. for AHK
    /// </summary>
    public class CodeDOMDocumentServiceAHK : CodeDocumentDOMService
    {

        const string BY_REF = "byref";
        const string MODIFIER_STATIC = "static";
        const char NEWLINE = '\n';

        #region Fields

        Archimedes.CodeDOM.CodeDOMTraveler _codeDOMTraveler = new Archimedes.CodeDOM.CodeDOMTraveler();
        CodeMemberMethodEx _autoexec;
        CodeTypeDeclarationEx _superBase;
        
        object startcompilerLock = new object();
        CancellationTokenSource _cts;

        CodeTypeDeclarationEx _rootLanguageSnapshot;
        object _rootLanguageSnapshotLOCK = new object();

        CodeDocumentDOMService _dependingOnSave;

        /// <summary>
        /// This is the language Root
        /// which is staticaly and always gets merged into this RootType if there is no Depending 
        /// DOM Service is found
        /// </summary>
        protected readonly CodeTypeDeclarationEx _root;
        #endregion

        #region Constructor

        public CodeDOMDocumentServiceAHK(ProjectItemCodeDocument document)
            : base(document) {


                #region Create Language Master Root

                _root = new CodeTypeDeclarationEx(null, "Global") { CodeDocumentItem = document };


                #region Create  auto exec method for AHK Scripts

                _root.Members.Add(_autoexec = new CodeMemberMethodExAHK(true)
                {
                    Name = "AutoExec",
                    Attributes = MemberAttributes.Public | MemberAttributes.Static,
                    DefiningType = _languageRoot,
                    ReturnType = new CodeTypeReference(typeof(void)),
                    LinePragma = new CodeLinePragma("all", 0),
                    IsHidden = true,
                    CodeDocumentItem = document
                });

                #endregion

                #region Setup Base Object

                var baseobj = new CodeTypeDeclarationEx(null, "Object")
                {
                    CodeDocumentItem = document,
                    IsClass = true,
                    IsBuildInType = true
                };
                baseobj.Comments.Add(new CodeCommentStatement("Base Object of all other Custom Objects", true));

                CodeMemberMethodExAHK method;

                #region Object.Insert

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "Insert",
                    CodeDocumentItem = document,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false,
                };
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "key"));
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "value"));
                method.Comments.Add(new CodeCommentStatement("Inserts key-value pairs into the object, automatically adjusting existing keys if appropriate.", true));
                method.ReturnType = new CodeTypeReference(typeof(bool));
                baseobj.Members.Add(method);

                #endregion

                #region Object.Remove

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "Remove",
                    Project = document.Project,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false
                };
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "key"));
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(object)), "value"));
                method.Comments.Add(new CodeCommentStatement("Removes key-value pairs from an object.", true));
                method.ReturnType = new CodeTypeReference(typeof(bool));
                baseobj.Members.Add(method);

                #endregion

                #region Object.Clone

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "Clone",
                    CodeDocumentItem = document,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false
                };
                method.Comments.Add(new CodeCommentStatement("Returns a shallow copy of the object.", true));
                method.ReturnType = new CodeTypeReference(typeof(object));
                baseobj.Members.Add(method);

                #endregion

                #region Object.MinIndex

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "MinIndex",
                    CodeDocumentItem = document,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false
                };
                method.Comments.Add(new CodeCommentStatement("If any integer keys are present, MinIndex returns the lowest and MaxIndex returns the highest. Otherwise an empty string is returned.", true));
                method.ReturnType = new CodeTypeReference(typeof(int));
                baseobj.Members.Add(method);

                #endregion

                #region Object.MaxIndex

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "MaxIndex",
                    CodeDocumentItem = document,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false
                };
                method.Comments.Add(new CodeCommentStatement("If any integer keys are present, MinIndex returns the lowest and MaxIndex returns the highest. Otherwise an empty string is returned.", true));
                method.ReturnType = new CodeTypeReference(typeof(int));
                baseobj.Members.Add(method);

                #endregion

                #region Object.SetCapacity

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "SetCapacity",
                    CodeDocumentItem = document,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false
                };
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "MaxItemsOrKey"));
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "ByteSize"));
                method.Comments.Add(new CodeCommentStatement("If any integer keys are present, MinIndex returns the lowest and MaxIndex returns the highest. Otherwise an empty string is returned.", true));
                method.ReturnType = new CodeTypeReference(typeof(int));
                baseobj.Members.Add(method);

                #endregion

                #region Object.GetCapacity

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "GetCapacity",
                    CodeDocumentItem = document,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false
                };
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "Key"));
                method.Comments.Add(new CodeCommentStatement("Returns the current capacity of an object or one of its fields.", true));
                method.ReturnType = new CodeTypeReference(typeof(int));
                baseobj.Members.Add(method);

                #endregion

                #region Object.HasKey

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "HasKey",
                    CodeDocumentItem = document,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false
                };
                method.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "Key"));
                method.Comments.Add(new CodeCommentStatement("Returns true if Key is associated with a value (even '') within Object, otherwise false.", true));
                method.ReturnType = new CodeTypeReference(typeof(bool));
                baseobj.Members.Add(method);

                #endregion

                #region Object._NewEnum

                method = new CodeMemberMethodExAHK(true)
                {
                    Name = "_NewEnum",
                    CodeDocumentItem = document,
                    IsDefaultMethodInvoke = true,
                    IsTraditionalCommand = false
                };

                method.Comments.Add(new CodeCommentStatement("Returns a new enumerator to enumerate this object's key-value pairs.", true));
                method.ReturnType = new CodeTypeReference(typeof(IEnumerator));
                baseobj.Members.Add(method);

                #endregion


                _root.Members.Add(baseobj);

                #endregion

                // Import build-in members
                foreach(var m in document.CodeLanguage.BuildInMembers) {
                    var codeobj = m as ICodeMemberEx;
                    if(codeobj != null) {
                        codeobj.Project = document.Project;
                    }
                    _root.Members.Add(m);
                }

                #endregion

                _rootLanguageSnapshot = _root;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Ensures that the parser is finished.
        /// </summary>
        public override bool WaitUntilUpdated(int timeout) {
            while(true) {
                if(!IsBusy)
                    return true;
                else if(timeout < 0) {
                    return false;
                }
                System.Threading.Thread.Sleep(1);
                timeout--;
            }
        }

        
        public override async Task CompileTokenFileAsync() {

            await TaskEx.Run(() => {

                if(IsBusy) {
                    _cts.Cancel();
                    while(IsBusy)
                        Thread.Sleep(5);
                }

                IsBusy = true;
                _cts = new CancellationTokenSource();
                try {
                    var tsk = CompileTokenFile(_cts.Token);
                    tsk.Wait();
                    OnASTUpdated();
                } catch(OperationCanceledException e) {
                    //Console.WriteLine("Processing canceled.");
                    var dummy = e.Message;
                } catch(AggregateException e) {
                    var dummy = e.Message;
                }
            });

            IsBusy = false;
        }

        public override CodeTypeDeclarationEx GetRootTypeSnapshot() {
            lock(_rootLanguageSnapshotLOCK) {
                return _rootLanguageSnapshot;
            }
        }


        #endregion

        #region Token Compiler Worker

        bool firstAfterNull = true;
        bool ignoreDependingOnce = false;

        protected async Task CompileTokenFile(CancellationToken cancellationToken) {
            try {
                await TaskEx.Run(() => {
                    _languageRoot = new CodeTypeDeclarationRoot() { Project = _document.Project };
                    CodeTypeDeclarationEx initialparent = _languageRoot;


                    cancellationToken.ThrowIfCancellationRequested();

                    _dependingOnSave = this.DependingOn;

                    #region Clean Up

                    _document.Project.Solution.ErrorService.ClearAllErrorsFrom(_document, Errors.ErrorSource.ASTParser);
                    _codeRangeManager.Clear();

                    #endregion

                    #region Merge DependingOn Members

                    if(_dependingOnSave == null) {
                        // merge super base members
                        _languageRoot.Members.AddRange(_root.Members);
                        firstAfterNull = true;
                    } else {

                        //if(!_project.IsInUpdate) {
                        //    if(firstAfterNull) {
                        //        ignoreDependingOnce = true;
                        //        _dependingOnSave.CompileTokenFileAsync();
                        //        firstAfterNull = false;
                        //    }
                        //    _dependingOnSave.WaitUntilUpdated(200);
                        //}
                        
                        _languageRoot.Members.AddRange(_dependingOnSave.GetRootTypeSnapshot().Members);
                    }

                    #endregion

                    var codeLineMap = _document.SegmentService.GetCodeSegmentLinesMap();
                    CodeTypeDeclaration parent = initialparent;
                    Stack<CodeSegment> paramstack = new Stack<CodeSegment>();
                    int linecnt = 0;
                    if(codeLineMap.Keys.Any())
                        linecnt = codeLineMap.Keys.Max();

                    CodeTokenLine line;

                    Stack<CodeTypeDeclarationEx> parentHirarchy = new Stack<CodeTypeDeclarationEx>();
                    int bcc = 0;
                    parentHirarchy.Push(initialparent);

                    cancellationToken.ThrowIfCancellationRequested();

                    #region Parse

                    for(int i = 0; i <= linecnt; i++) {

                        cancellationToken.ThrowIfCancellationRequested();

                        if(codeLineMap.ContainsKey(i))
                            line = codeLineMap[i];
                        else
                            continue;

                        // is class definition?:

                        #region Parse Class Definition

                        var classkeywordSegment = line.CodeSegments[0].ThisOrNextOmit(whitespacetokenNewLines);
                        if(classkeywordSegment != null && classkeywordSegment.Token == Token.KeyWord && classkeywordSegment.TokenString.Equals("class", StringComparison.CurrentCultureIgnoreCase)) {
                            var classNameSegment = classkeywordSegment.FindNextOnSameLine(Token.Identifier);
                            if(classNameSegment != null) {

                                var next = classNameSegment.NextOmit(whitespacetokenNewLines);
                                if(next != null) {
                                    CodeTypeDeclarationEx thisparent = parentHirarchy.Any() ? parentHirarchy.Peek() : _languageRoot;
                                    CodeTypeReferenceEx basecls = null;
                                    CodeSegment refBaseClass = null;
                                    if(next.Token == Token.KeyWord && next.TokenString.Equals("extends", StringComparison.InvariantCultureIgnoreCase)) {
                                        refBaseClass = next.NextOmit(whitespacetokenNewLines);

                                        if(refBaseClass != null) {
                                            if(refBaseClass.Token == Token.Identifier) {
                                                refBaseClass.CodeDOMObject = basecls = new CodeTypeReferenceEx(_document, refBaseClass.TokenString, thisparent);
                                                next = refBaseClass.NextOmit(whitespacetokenNewLines);
                                            } else {
                                                RegisterError(_document, next.Next, "Expected: Class Name Identifier");
                                                next = next.NextOmit(whitespacetokenNewLines);
                                            }
                                        } else {
                                            if(next.Next != null && next.Next.Token != Token.BlockOpen) {
                                                RegisterError(_document, next.Next, "Expected: Class Name Identifier");
                                                next = next.NextOmit(whitespacetokenNewLines);
                                            }
                                        }
                                    }

                                    if(next != null) {
                                        if(next.Token == Token.BlockOpen) {

                                            #region Add Class Declaration

                                            CodeSegment classBodyStart = next;

                                            var type = new CodeTypeDeclarationEx(_document, classNameSegment.TokenString)
                                            {
                                                IsClass = true,
                                                LinePragma = CreatePragma(classNameSegment, _document.FilePath),
                                                CodeDocumentItem = _document
                                            };
                                            classNameSegment.CodeDOMObject = type;


                                            // check if this type was alread defined in this scope
                                            if(thisparent.GetInheritedMembers().Contains(type)) {
                                                RegisterError(_document, classNameSegment, "oh my dear, this class already exisits in the current scope!");
                                            } else {

                                                #region Check & Resolve Baseclass

                                                if(basecls != null) {
                                                    //check if we have a circual interhance tree
                                                    var baseclassImpl = basecls.ResolveTypeDeclarationCache();
                                                    if(baseclassImpl != null && baseclassImpl.IsSubclassOf(new CodeTypeReferenceEx(_document, classNameSegment.TokenString, thisparent))) {
                                                        //circular dependency detected!!
                                                        RegisterError(_document, refBaseClass, "Woops you just produced a circular dependency in your inheritance tree!");
                                                    } else {
                                                        if(basecls != null)
                                                            type.BaseTypes.Add(basecls);
                                                        else
                                                            type.BaseTypes.Add(new CodeTypeReferenceEx(_document, "Object", thisparent) { ResolvedTypeDeclaration = _superBase });
                                                    }
                                                }

                                                #endregion


                                                // extract class documentation Comment
                                                var comment = ExtractComment(classkeywordSegment);
                                                if(comment != null)
                                                    type.Comments.Add(comment);


                                                // Add it to the CodeDOM Tree
                                                thisparent.Members.Add(type);
                                                type.Parent = thisparent;
                                            }

                                            // Create a CodeRange Item
                                            int startOffset = classBodyStart.Range.Offset;
                                            var classBodyEnd = classBodyStart.FindClosingBracked(true);
                                            if(classBodyEnd != null) {
                                                int length = (classBodyEnd.Range.Offset - startOffset);
                                                _codeRangeManager.Add(new CodeRange(new SimpleSegment(startOffset, length), type));
                                            } else {
                                                RegisterError(_document, classBodyStart, "Expected: " + Token.BlockClosed);
                                            }

                                            parentHirarchy.Push(type);
                                            bcc++;

                                            i = classBodyStart.LineNumber; // jumt to:  class Foo { * <---|
                                            continue;

                                            #endregion

                                        } else {
                                            RegisterError(_document, next, "Expected: " + Token.BlockOpen);
                                            i = (next.Next != null) ? next.Next.LineNumber : next.LineNumber;
                                        }
                                    }
                                }
                            }
                        }

                        #endregion

                        // adjust some asumptions the tokenizer has made

                        if(parentHirarchy.Count > 1) {
                            // if we are in a class body, we can't have a traditional command invoke
                            foreach(var s in line.CodeSegments)
                                if(s.Token == Token.TraditionalCommandInvoke) {
                                    s.Token = Token.Identifier;
                                    break;
                                }
                        }


                        // is method definition?:

                        #region Analyze for Method Definition

                        var methodSegment = line.CodeSegments[0].ThisOrNextOmit(whitespacetokenNewLines);
                        if(methodSegment != null && methodSegment.Token == Token.Identifier) {
                            var methodSignatureStart = methodSegment.Next;
                            if(methodSignatureStart != null && methodSignatureStart.Token == Token.LiteralBracketOpen) {
                                var methodSignatureEnd = methodSignatureStart.FindClosingBracked(false);
                                if(methodSignatureEnd != null) {
                                    var startMethodBody = methodSignatureEnd.NextOmit(whitespacetokenNewLinesComments);
                                    if(startMethodBody != null && startMethodBody.Token == Token.BlockOpen) {
                                        // jup we have a method definition here.
                                        // Method body starts at startMethodBody


                                        CodeTypeDeclarationEx thisparent = parentHirarchy.Any() ? parentHirarchy.Peek() : _languageRoot;
                                        bool hasDeclarationError = false;

                                        #region Generate Method Definition DOM

                                        var method = new CodeMemberMethodExAHK(_document)
                                        {
                                            Name = methodSegment.TokenString,
                                            LinePragma = CreatePragma(methodSegment, _document.FilePath),
                                            CodeDocumentItem = _document,
                                            ReturnType = new CodeTypeReferenceEx(_document, typeof(object))
                                        };
                                        methodSegment.CodeDOMObject = method;

                                        //check if this method is not already defined elsewere in current scope

                                        var equalmethods = from m in thisparent.Members.Cast<CodeTypeMember>()
                                                           let meth = m as CodeMemberMethodExAHK
                                                           where meth != null && !meth.IsBuildInType && meth.Equals(method)
                                                           select meth;


                                        if(equalmethods.Any()) {
                                            RegisterError(_document, methodSegment,
                                                string.Format("The Methodename '{0}' is already used in the current scope!", method.Name));
                                            hasDeclarationError = true;
                                        } else {


                                            // extract Method Comment
                                            var comment = ExtractComment(methodSegment);
                                            if(comment != null)
                                                method.Comments.Add(comment);

                                            // extract method params
                                            paramstack.Clear();
                                            CodeSegment previous = methodSignatureStart;

                                            // get method params:
                                            while(true) {
                                                var current = previous.Next;
                                                if(current.Token == Token.Identifier || current.Token == Token.KeyWord) {
                                                    paramstack.Push(current);
                                                } else if(current.Token == Token.ParameterDelemiter || current.Token == Token.LiteralBracketClosed) {
                                                    // end of param reached:
                                                    if(paramstack.Count == 1) {
                                                        // thread one param as the untyped argument, type of Object
                                                        method.Parameters.Add(new CodeParameterDeclarationExpressionEx(typeof(object), paramstack.Pop().TokenString)
                                                            {
                                                                Direction = FieldDirection.In
                                                            });
                                                    } else if(paramstack.Count == 2) {

                                                        CodeParameterDeclarationExpressionEx param;

                                                        var second = paramstack.Pop();
                                                        var first = paramstack.Pop();

                                                        //handle byref ->
                                                        if(first.Token == Token.KeyWord && first.TokenString.Equals(BY_REF, StringComparison.InvariantCultureIgnoreCase)) {
                                                            param = new CodeParameterDeclarationExpressionEx(typeof(object), second.TokenString);
                                                            param.Direction = FieldDirection.Ref;
                                                        } else {
                                                            param = new CodeParameterDeclarationExpressionEx(
                                                                new CodeTypeReferenceEx(_document, first.TokenString, thisparent),
                                                                paramstack.Pop().TokenString);
                                                        }

                                                        // thread two param as the type and argument
                                                        method.Parameters.Add(param);
                                                    }
                                                    if(current.Token == Token.LiteralBracketClosed)
                                                        break;
                                                } else if(current.Token == Token.NewLine){
                                                    break;
                                                }
                                                previous = current;
                                            }
                                        }
                                        #endregion

                                        // Method body ends at
                                        var endMethodBody = startMethodBody.FindClosingBracked(true);
                                        if(endMethodBody != null) {

                                            // get method statements
                                            method.Statements.AddRange(
                                                CollectAllCodeStatements(cancellationToken, thisparent, codeLineMap, startMethodBody.LineNumber + 1, endMethodBody.LineNumber));


                                            // add it to the code DOM Tree
                                            if(!hasDeclarationError) {
                                                thisparent.Members.Add(method);
                                                method.DefiningType = thisparent;
                                            }


                                            // Create a CodeRange Item
                                            int startOffset = startMethodBody.Range.Offset;
                                            int length = (endMethodBody.Range.Offset - startOffset);

                                            _codeRangeManager.Add(new CodeRange(new SimpleSegment(startOffset, length), method));

                                            // move the scanpointer to the method end:
                                            i = endMethodBody.LineNumber;
                                            continue;
                                        } else {
                                            RegisterError(_document, startMethodBody, "Missing: " + Token.BlockClosed);


                                        }
                                    }
                                }
                            }
                        }

                        #endregion


                        // is class property / field

                        #region Parse Class Properties / Fields

                        if(parentHirarchy.Count > 1) {
                            // we must be in a class to have method properties
                            // extract keywords & Identifiers untill we reach
                            // any otherToken - omit whitespaces 

                            List<CodeSegment> declarationStack = new List<CodeSegment>();

                            CodeSegment current = line.CodeSegments[0].ThisOrNextOmit(whitespacetokens);

                            while(current != null) {
                                if(current.Token == Token.WhiteSpace) {
                                    //ignore white spaces
                                } else if(current.Token == Token.Identifier || current.Token == Token.KeyWord) {
                                    declarationStack.Add(current);
                                } else {
                                    break;
                                }
                                current = current.Next;
                            }

                            switch(declarationStack.Count) {


                                case 0:
                                    break;
                                case 1:
                                    // instance field definition
                                    if(declarationStack[0].Token == Token.Identifier) {

                                        // this is an untyped instance class field declaration

                                        var propertyType = new CodeTypeReference(typeof(object));
                                        var memberprop = new CodeMemberPropertyEx(_document)
                                        {
                                            Name = declarationStack[0].TokenString,
                                            Attributes = MemberAttributes.Public,
                                            Type = propertyType,
                                            LinePragma = CreatePragma(declarationStack[0], _document.FilePath)
                                        };
                                        declarationStack[0].CodeDOMObject = memberprop;
                                        parentHirarchy.Peek().Members.Add(memberprop);

                                    } else {
                                        RegisterError(_document, declarationStack[0], string.Format("Unexpected Token: {0}", declarationStack[0].Token));
                                    }

                                    break;

                                case 2:
                                    // we have two members in the decalration stack
                                    // expected token flows
                                    // keyword - identifier:  static Field, int Field
                                    // identifier - identifier: Car myTypedCarField

                                    // for the time beeing, AHK just supports static Field

                                    if(declarationStack[0].Token == Token.KeyWord
                                        && declarationStack[0].TokenString.Equals(MODIFIER_STATIC, StringComparison.InvariantCultureIgnoreCase)) {

                                        if(declarationStack[1].Token == Token.Identifier) {

                                            // this is an untyped static class field declaration

                                            var propertyType = new CodeTypeReference(typeof(object));
                                            var memberprop = new CodeMemberPropertyEx(_document)
                                            {
                                                Name = declarationStack[1].TokenString,
                                                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                                                Type = propertyType,
                                                LinePragma = CreatePragma(declarationStack[1], _document.FilePath)
                                            };
                                            declarationStack[1].CodeDOMObject = memberprop;
                                            parentHirarchy.Peek().Members.Add(memberprop);
                                            

                                        } else {
                                            RegisterError(_document, declarationStack[0], string.Format("Expected: {0}", Token.Identifier));

                                        }

                                    } else {
                                        RegisterError(_document, declarationStack[0], string.Format("Unexpected '{0}' in Class Body", declarationStack[0].TokenString));
                                    }
                                    break;

                                default:
                                    for(int ci = 0; ci < declarationStack.Count; ci++) {
                                        RegisterError(_document, declarationStack[ci], string.Format("To much declaration Members", declarationStack[0].Token));
                                    }
                                    break;

                            }

                        }

                        #endregion


                        #region Parse Remaining Tokens

                        if(codeLineMap.ContainsKey(i)) {
                            var lineBlock = codeLineMap[i];
                            CodeTypeDeclarationEx thisparent = parentHirarchy.Any() ? parentHirarchy.Peek() : _languageRoot;

                            foreach(var segment in lineBlock.CodeSegments) {

                                cancellationToken.ThrowIfCancellationRequested();

                                if(segment.Token == Token.BlockOpen) {
                                    bcc++;
                                } else if(segment.Token == Token.BlockClosed) {
                                    bcc--;
                                    if(parentHirarchy.Count - 2 == bcc) {
                                        if(parentHirarchy.Any())
                                            parentHirarchy.Pop();
                                    }
                                }
                            }
                            _autoexec.Statements.AddRange(
                                            CollectAllCodeStatements(cancellationToken, thisparent, codeLineMap, i, i));

                        } else
                            continue;

                        #endregion
                    }

                    #endregion

                    AnalyzeAST(cancellationToken);

                    lock(_rootLanguageSnapshotLOCK) {
                        _rootLanguageSnapshot = _languageRoot;
                    }
                });
            } catch(OperationCanceledException) {
                throw;
            }
        }

        CodeCommentStatement ExtractComment(CodeSegment identifier) {
            var comment = identifier.PreviousOmit(whitespacetokenNewLines);
            if(comment != null && comment.Token == Token.MultiLineComment) {
                return new CodeCommentStatement(GetDocumentationComment(comment.TokenString), true);
            } else if(comment != null && comment.Token == Token.SingleLineComment) {

                //todo: collect all above singleline comments
            }
            return null;
        }


        string GetDocumentationComment(string data) {
            StringBuilder sb = new StringBuilder();
            foreach(var line in data.Split('\n','\r')) {
                var cline = line.Trim();
                cline = cline.TrimStart(' ', '\t', ';', '/');
                cline = cline.TrimStart('*');
                cline = cline.TrimEnd(' ', '\t', '/');
                cline = cline.TrimEnd('*');
                cline = cline.Trim();
                if(!string.IsNullOrWhiteSpace(cline))
                    sb.AppendLine(cline);
            }
            return sb.ToString();
        }

        #endregion

        #region Analyze AST


        void AnalyzeAST(CancellationToken cancellationToken) {

            var segmentService = _document.SegmentService;
            var segments = segmentService.GetSegments();

            foreach(var segment in segments) {

                cancellationToken.ThrowIfCancellationRequested();

                #region Resolve CodeTypeReferencees

                if(segment.CodeDOMObject is CodeTypeReferenceEx) {
                    var codeTypeRef = segment.CodeDOMObject as CodeTypeReferenceEx;

                    if(codeTypeRef.ResolvedTypeDeclaration == null) {
                        var refi = codeTypeRef.ResolveTypeDeclarationCache();
                        if(refi == null) {
                            RegisterError(_document, segment,
                                string.Format("Type '{0}' does not exist!", segment.TokenString));
                        }
                    }

                }

                #endregion

                #region Resolve Code Method Invoke Referencees


                if(segment.CodeDOMObject is CodeMethodReferenceExpressionEx) {
                    var methodRef = segment.CodeDOMObject as CodeMethodReferenceExpressionEx;
                    if(methodRef.ResolvedMethodMember == null && !(methodRef.EnclosingType is CodeTypeDeclarationDynamic)) {
                        var refi = methodRef.ResolveMethodDeclarationCache();
                        if(refi == null) {
                            RegisterError(_document, segment,
                                string.Format("Method '{0}' does not exist!", segment.TokenString));
                        }
                    }
                }

                #endregion

                #region Resolve Code Property Invoke Referencees

                if(segment.CodeDOMObject is CodePropertyReferenceExpressionEx) {
                    var propRef = segment.CodeDOMObject as CodePropertyReferenceExpressionEx;
                    if(propRef.ResolvedPropertyMember == null && !(propRef.EnclosingType is CodeTypeDeclarationDynamic)) {
                        var refi = propRef.ResolvePropertyDeclarationCache();
                        if(refi == null) {
                            RegisterError(_document, segment,
                                string.Format("Property '{0}' does not exist!", segment.TokenString));
                        }
                    }
                }

                #endregion
            }
        }

        #endregion

        #region Helper Methods

        [DebuggerStepThrough]
        void RegisterError(Projecting.ProjectItemCodeDocument codeitem, CodeSegment segment, string errorDescription) {
            ThrowUtil.ThrowIfNull(codeitem);
            ThrowUtil.ThrowIfNull(segment);
            var errorService = codeitem.Project.Solution.ErrorService;
            segment.ErrorContext = new CodeError() { Description = errorDescription };
            errorService.Add(new Errors.ErrorItem(segment, codeitem));
        }

        CodeLinePragma CreatePragma(CodeSegment segment, string filename) {
            return new CodeLinePragma() { LineNumber = segment.LineNumber, FileName = filename };
        }

        #endregion

        #region Parse CodeStatements

        /// <summary>
        /// Get all CodeStatements in the given Linerange
        /// </summary>
        /// <param name="segments"></param>
        /// <param name="startLine"></param>
        /// <param name="endLine"></param>
        /// <returns></returns>
        CodeStatementCollection CollectAllCodeStatements(CancellationToken cancelToken, CodeTypeDeclarationEx enclosingType, Dictionary<int, CodeTokenLine> segments, int startLine, int endLine) {
            CodeTokenLine line;
            var codeStatements = new CodeStatementCollection();

            for(int i = startLine; i <= endLine; i++) {

                cancelToken.ThrowIfCancellationRequested();

                if(segments.ContainsKey(i))
                    line = segments[i];
                else
                    continue;

                CodeSegment toParse = line.CodeSegments.First();
                CodeSegment next;
                while(toParse != null) {
                    var ex = ParseExpression(toParse, out next, enclosingType);
                    codeStatements.Add(ex);
                    toParse = next;
                }
            }
            return codeStatements;
        }

        static readonly List<Token> LocalExpressionEndTokens = new List<Token>() { Token.NewLine, Token.ParameterDelemiter };


        CodeExpression ParseExpression(CodeSegment tokenSegment, out CodeSegment nextToParse, CodeTypeDeclarationEx enclosingType) {
            CodeExpression expression = null;
            nextToParse = tokenSegment.Next;


            if(tokenSegment.Token == Token.Identifier && tokenSegment.Next != null
                    && tokenSegment.Next.Token == Token.LiteralBracketOpen) {

                bool ismethodDeclaration = false;
                var closingliteral = tokenSegment.Next.FindClosingBracked(true);
                if(closingliteral != null) {
                    //ensure that it is not a defect method declaration
                    var bra = closingliteral.NextOmit(whitespacetokenNewLines);
                    if(bra != null && bra.Token == Token.BlockOpen) {
                        // it is a method indeed
                        ismethodDeclaration = true;
                    }
                } else {
                    RegisterError(_document, tokenSegment.Next, "Missing: )");
                }

                #region Parse for Method Invokes

                if(!ismethodDeclaration) {

                    CodeTypeDeclarationEx methodContext = null;

                    if(tokenSegment.Previous != null && tokenSegment.Previous.Previous != null
                        && tokenSegment.Previous.Token == Token.MemberInvoke) {

                        var invoker = tokenSegment.Previous.Previous;

                        #region adjust Method Context

                        if(_document.CodeLanguage.SELFREF_CAN_BE_OMITTED)
                            methodContext = enclosingType;

                        if(invoker.CodeDOMObject is CodeBaseReferenceExpression) {
                            foreach(CodeTypeReferenceEx bt in enclosingType.BaseTypes) {
                                var typedeclaration = bt.ResolveTypeDeclarationCache();
                                if(typedeclaration != null && typedeclaration.IsClass) {
                                    methodContext = typedeclaration;
                                    break;
                                }
                            }
                        } else if(invoker.CodeDOMObject is CodeThisReferenceExpression) {
                            methodContext = enclosingType;
                        } else if(invoker.Token == Token.Identifier) {
                            invoker.CodeDOMObject = CodeTypeDeclarationDynamic.Default;
                            methodContext = CodeTypeDeclarationDynamic.Default;
                        }

                        #endregion
                    }

                    var invokeExpression = new CodeMethodInvokeExpression();
                    var methodRef = new CodeMethodReferenceExpressionExAHK(_document, null, tokenSegment.TokenString, methodContext);

                    invokeExpression.Method = methodRef;
                    tokenSegment.CodeDOMObject = methodRef;
                    expression = invokeExpression;
                }

                nextToParse = tokenSegment.Next.Next;
                
                #endregion

            } else if(tokenSegment.Token == Token.KeyWord) {

                #region Parse Keywords

                if(tokenSegment.TokenString.Equals("new", _document.CodeLanguage.NameComparisation)) {

                    #region NEW parse for new Object Expressions

                    var newObjectInvoke = tokenSegment.NextOmit(whitespacetokenNewLines);
                    if(newObjectInvoke != null && newObjectInvoke.Token == Token.Identifier) {

                        var objectinstangicing = new CodeObjectCreateExpression();
                        objectinstangicing.CreateType = new CodeTypeReferenceEx(_document, newObjectInvoke.TokenString, enclosingType);
                        tokenSegment.CodeDOMObject = objectinstangicing;
                        newObjectInvoke.CodeDOMObject = objectinstangicing.CreateType;

                        expression = objectinstangicing;
                        nextToParse = newObjectInvoke.Next;
                    }

                    #endregion

                } else if(tokenSegment.TokenString.Equals("this", _document.CodeLanguage.NameComparisation)) {
                    var thisrefExpression = new CodeThisReferenceExpression();
                    tokenSegment.CodeDOMObject = thisrefExpression;
                    expression = thisrefExpression;
                } else if(tokenSegment.TokenString.Equals("base", _document.CodeLanguage.NameComparisation)) {
                    var baserefExpression = new CodeBaseReferenceExpression();
                    tokenSegment.CodeDOMObject = baserefExpression;
                    expression = baserefExpression;
                }

                #endregion

            } else if(tokenSegment.Token == Token.Identifier  && tokenSegment.Previous != null
                    && tokenSegment.Previous.Token == Token.MemberInvoke) {

                #region Parse MemberInvoke (Dot) Class.Member

                var context = tokenSegment.Previous.Previous;
                if(context == null) {
                    //unexpected!
                    var err = "Unexpected Member Invoke!";
                    RegisterError(_document, tokenSegment, err);
                    RegisterError(_document, tokenSegment.Previous, err);
                    nextToParse = tokenSegment.Next;
                } else if(context.Token == Token.KeyWord && context.TokenString.Equals("this", _document.CodeLanguage.NameComparisation)) {

                    var propRef = new CodePropertyReferenceExpressionEx(_document, null, tokenSegment.TokenString, enclosingType);
                    tokenSegment.CodeDOMObject = propRef;

                } else if(context.Token == Token.KeyWord && context.TokenString.Equals("base", _document.CodeLanguage.NameComparisation)) {

                    CodeTypeDeclarationEx typedeclaration = null;
                    foreach(CodeTypeReferenceEx bt in enclosingType.BaseTypes) {
                        typedeclaration = bt.ResolveTypeDeclarationCache();
                        if(typedeclaration != null && typedeclaration.IsClass) {
                            break;
                        }
                    }
                    var propRef = new CodePropertyReferenceExpressionEx(_document, null, tokenSegment.TokenString, typedeclaration);
                    tokenSegment.CodeDOMObject = propRef;

                } else if(context.Token == Token.Identifier) {
                    // we currently not supprt real expression parsing, so leave here...

                    context.CodeDOMObject = CodeTypeDeclarationDynamic.Default;
                    var propRef = new CodePropertyReferenceExpressionEx(_document, null, tokenSegment.TokenString, CodeTypeDeclarationDynamic.Default);
                    tokenSegment.CodeDOMObject = propRef;

                }

                #region Parse for one hirarchy this/base Property/Field Invokes



                #endregion

                #endregion

            } else if(tokenSegment.Token == Token.TraditionalCommandInvoke) {

                #region Parse Traditional Command Invoke

                var members = from m in _languageRoot.Members.Cast<CodeTypeMember>()
                              let methd = m as CodeMemberMethodExAHK
                              where methd != null && methd.IsTraditionalCommand && methd.Name.Equals(tokenSegment.TokenString, StringComparison.InvariantCultureIgnoreCase)
                              select methd;
                if(members.Any()) {

                    var invokeExpression = new CodeMethodInvokeExpression();
                    var methodRef = new CodeMethodReferenceExpressionExAHK(_document, members.First());

                    tokenSegment.CodeDOMObject = methodRef;
                    expression = invokeExpression;
                } else {
                    RegisterError(_document, tokenSegment, string.Format("Unknown traditional Command '{0}'", tokenSegment.TokenString));
                }

                #endregion
            } else if(tokenSegment.Token == Token.LiteralString) {

                if(tokenSegment.TokenString[tokenSegment.TokenString.Length - 1] != '"') {
                    RegisterError(_document, tokenSegment, "Missing string end Quote");
                }
            }

            if(!(nextToParse != null && nextToParse.LineNumber == tokenSegment.LineNumber)) {
                nextToParse = null;
            }
            return expression;
        }

        #endregion

        protected override void OnDependingOnASTUpdated(object sender, EventArgs e) {

            if(!_project.ASTManager.UpdateAtWill)
                return;

            if(!ignoreDependingOnce) {
                this.CompileTokenFileAsync();
                base.OnDependingOnASTUpdated(sender, e);
            } else {
                ignoreDependingOnce = false;
            }
        }
        
        bool _isBusy = false;
        object _isBusyLock = new object();

        public override bool IsBusy {
            get {  
                lock(_isBusyLock)  {
                    return _isBusy;
                }
            }
            protected set {
                lock(_isBusyLock) { _isBusy = value; }
            }
        }

    }
}
