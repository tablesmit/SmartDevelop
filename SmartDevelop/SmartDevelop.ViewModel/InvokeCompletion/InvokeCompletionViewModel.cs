﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Archimedes.Patterns.WPF.ViewModels;
using System.Collections.ObjectModel;
using SmartDevelop.Model.Projecting;
using SmartDevelop.ViewModel.DocumentFiles;
using System.Windows.Controls;
using System.Windows.Input;
using SmartDevelop.Model.Tokenizing;
using SmartDevelop.Model.DOM.Types;
using System.CodeDom;

namespace SmartDevelop.ViewModel.InvokeCompletion
{
    public class InvokeCompletionViewModel : WorkspaceViewModel
    {
        #region Fields

        InvokeParameter _activeParameter;
        string _invokeDescription;
        string _prefix;
        string _sufix;

        ProjectItemCodeDocument _document;
        CodeFileViewModel _documentVM;
        readonly ToolTip _toolTip;

        #endregion
        CodeMethodReferenceExpressionEx _methodRef;

        public InvokeCompletionViewModel(CodeFileViewModel documentVM, CodeMethodReferenceExpressionEx methodRef) {
            _document = documentVM.CodeDocument;
            _documentVM = documentVM;

            _toolTip = new ToolTip();
            _toolTip.Content = this;

            _documentVM.Editor.TextArea.KeyDown += (s, e) => {
                if(e.Key == Key.Escape)
                    this.CloseCommand.Execute(null);
            };

            _documentVM.Editor.TextArea.TextEntered += OnTextEntered;
            _documentVM.Editor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

            AllParameters = new ObservableCollection<InvokeParameter>();
            SetMethod(methodRef);
        }

        void SetMethod(CodeMethodReferenceExpressionEx methodref) {
            _methodRef = methodref;

            var methodDecl = methodref.ResolvedMethodMember;


            Prefix = methodDecl.Name + "(";
            Sufix = ")";
            InvokeDescription = methodref.CommentInfo;


            AllParameters.Clear();
            int i = 0;
            foreach(CodeParameterDeclarationExpressionEx p in methodDecl.Parameters) {

                var pVM = new InvokeParameter(((p.Direction == FieldDirection.Ref) ? "byref " : "") + 
                    p.Name + ((++i != methodDecl.Parameters.Count) ? ", " : ""), "-")
                {
                    ParameterDescripton = p.ParameterDocumentationComment
                };
                AllParameters.Add(pVM);
            }

        }

        /// <summary>
        /// Set the current parameter
        /// </summary>
        void SetCurrentParam(int paramNumber) {

            if(AllParameters.Count < paramNumber) {
                paramNumber = AllParameters.Count;
            }

            var param = AllParameters[paramNumber - 1];
            ActiveParameter = param;

            //InvokeDescription = paramNumber.ToString();
        }


        public void Show() {
            this.IsShown = true;
        }

        #region Properties


        public InvokeParameter ActiveParameter {
            get {
                return _activeParameter;
            }
            set {

                if(_activeParameter != null)
                    _activeParameter.IsActiveParameter = false;
                _activeParameter = value;
                if(value != null)
                    value.IsActiveParameter = true;
                OnPropertyChanged(() => ActiveParameter);
            }
        }

        public string Prefix {
            get { return _prefix; }
            set { 
                _prefix = value;
                OnPropertyChanged(() => Prefix);
            }
        }

        public string Sufix {
            get { return _sufix; }
            set { 
                _sufix = value;
                OnPropertyChanged(() => Sufix);
            }
        }

        
        public string InvokeDescription {
            get { 
                return _invokeDescription;
            }
            set { 
                _invokeDescription = value;
                OnPropertyChanged(() => InvokeDescription);
            }
        }

        public ObservableCollection<InvokeParameter> AllParameters {
            get;
            protected set;
        }

        /// <summary>
        /// Gets/Sets if the current InvokeCompletion VM is shown
        /// </summary>
        public bool IsShown {
            get {
                return _toolTip.IsOpen;
            }
            set {
                _toolTip.IsOpen = value;
                if(!_toolTip.IsOpen)
                OnClosed();
            }
        }

        #endregion

        static readonly List<Token> _endingTokens = new List<Token>() { Token.BlockClosed, Token.BlockOpen };

        /// <summary>
        /// Searches the enclosing Methode-Invoke Expression
        /// </summary>
        /// <param name="segment">segment to analyze</param>
        /// <returns>Returns the enclosing Method Reference Expression</returns>
        public static CodeMethodReferenceExpressionEx FindEnclosingMethodInvoke(CodeSegment segment, out int paramNumber) {
            int literalBracketCnt = 1;
            int indexerBrackedCnt = 0;
            paramNumber = 1;

            CodeMethodReferenceExpressionEx methodRef = null;
            CodeSegment current = segment;

            while(current != null) {

                if(_endingTokens.Contains(current.Token))
                    break;
                else if(current.Token == Token.LiteralBracketClosed)
                    literalBracketCnt++;
                else if(current.Token == Token.IndexerBracketClosed)
                    literalBracketCnt++;
                else if(current.Token == Token.IndexerBracketOpen)
                    literalBracketCnt--;
                else if(current.Token == Token.LiteralBracketOpen) {
                    literalBracketCnt--;
                } else if(current.Token == Token.ParameterDelemiter) {

                    if(literalBracketCnt == 1 && indexerBrackedCnt == 0) {
                        paramNumber++;
                    }

                } else if(current.Token == Token.Identifier && current.CodeDOMObject is CodeMethodReferenceExpressionEx) {
                    if(literalBracketCnt == 0 && indexerBrackedCnt == 0) {
                        methodRef = current.CodeDOMObject as CodeMethodReferenceExpressionEx;
                        break;
                    }
                }
                current = current.Previous;
            }
            return methodRef;
        }






        void OnTextEntered(object sender, TextCompositionEventArgs e) {
            char current = e.Text[0];


        }

        void OnCaretPositionChanged(object sender, EventArgs e) {

            var segment = _document.SegmentService.QueryCodeSegmentAt(_documentVM.Editor.CaretOffset);
            if(segment == null)
                this.CloseCommand.Execute(null);
            int paramNumber;
            var currentMethodRef = FindEnclosingMethodInvoke(segment, out paramNumber);

            if(currentMethodRef == null) {
                this.CloseCommand.Execute(null);
            } else if(!_methodRef.MethodName.Equals(currentMethodRef.MethodName, StringComparison.InvariantCultureIgnoreCase)) {
                SetMethod(currentMethodRef);
                SetCurrentParam(paramNumber);
            } else {
                SetCurrentParam(paramNumber);
            }

        }


        public override void OnRequestClose() {
            IsShown = false;
        }

        protected override void OnDispose() {
            _documentVM.Editor.TextArea.TextEntered -= OnTextEntered;
            _documentVM.Editor.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
            base.OnDispose();
        }


    }
}