﻿using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Archimedes.Patterns.Services;
using Archimedes.Patterns.WPF.ViewModels;
using Archimedes.Services.WPF.WorkBenchServices;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Folding;
using SmartDevelop.Model.Projecting;
using SmartDevelop.TokenizerBase.IA.Indentation;
using SmartDevelop.ViewModel.Folding;
using Archimedes.Patterns.WPF.Commands;
using Archimedes.Patterns.MVMV.ViewModels.PoolCache;

namespace SmartDevelop.ViewModel.DocumentFiles
{
    public class CodeFileViewModel : WorkspaceViewModel, ICacheable
    {
        #region Fields

        readonly ProjectItemCode _projectitem;
        readonly TextEditor _texteditor = new TextEditor();
        FoldingManager _foldingManager;
        AbstractFoldingStrategy _foldingStrategy;
        readonly IWorkBenchService _workbenchservice = ServiceLocator.Instance.Resolve<IWorkBenchService>();
        
        bool foldingDirty = true;
        #endregion

        /// <summary>
        /// Raised when this Instance no longer is required in the cache
        /// </summary>
        public event EventHandler CacheExpired;

        #region Constructor

        public static CodeFileViewModel Create(ProjectItemCode projectitem) {
            var viewModelPoolService = ServiceLocator.Instance.Resolve<IViewModelPoolService>();
            CodeFileViewModel vm;
            vm = viewModelPoolService.Resolve<CodeFileViewModel>(projectitem);
            if(vm == null) {
                vm = new CodeFileViewModel(projectitem);
                viewModelPoolService.Register(projectitem, vm);
            }
            return vm;
        }


        CodeFileViewModel(ProjectItemCode projectitem) {

            if(projectitem == null)
                throw new ArgumentNullException("projectitem");
            _projectitem = projectitem;

            _texteditor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            _texteditor.FontSize = 15;

            _texteditor.Document = _projectitem.Document;

            _texteditor.Document.TextChanged += OnDocumentTextChanged;
            _texteditor.SyntaxHighlighting = SyntaxHighlighterFinder.Find(projectitem.Type);
            _foldingStrategy = new IAFoldingStrategy(_projectitem.TokenService);

			if (_foldingManager == null)
                _foldingManager = FoldingManager.Install(_texteditor.TextArea);
            _foldingStrategy.UpdateFoldings(_foldingManager, _texteditor.Document);


            _texteditor.MouseHover += TextEditorMouseHover;
            _texteditor.MouseHoverStopped += TextEditorMouseHoverStopped;
            _texteditor.TextArea.TextEntered += OnTextEntered;
            _texteditor.TextArea.TextEntering += OnTextEntering;

            _texteditor.TextArea.IndentationStrategy = new IAIndentationStrategy();

            DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
            foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
            foldingUpdateTimer.Tick += foldingUpdateTimer_Tick;
            foldingUpdateTimer.Start();
        }

        #endregion

        #region VM Properties

        public TextEditor Editor {
            get {
                return _texteditor;
            }
        }

        public override string DisplayName {
            get {
                return _projectitem.Name;
            }
            set {
                _projectitem.Name = value;
            }
        }

        string _contextToolTip;
        bool _contextToolTipIsOpen;

        public bool ContextToolTipIsOpen {
            get { return _contextToolTipIsOpen; }
            set { 
                _contextToolTipIsOpen = value;
                OnPropertyChanged(() => ContextToolTipIsOpen);
            }
        }

        public string ContextToolTip {
            get { return _contextToolTip; }
            set { 
                _contextToolTip = value;
                OnPropertyChanged(() => ContextToolTip); 
            }
        }

        #endregion

        #region Commands

        #region Show Command

        RelayCommand _showCommand;
        public ICommand ShowCommand {
            get {
                if(_showCommand == null) {
                    _showCommand = new RelayCommand(x => Show());
                }
                return _showCommand;
            }
        }

        void Show() {
            _workbenchservice.ShowDockedDocument(this, this.DisplayName);
        }

        #endregion

        #endregion

        #region Event Handlers

        CompletionWindow completionWindow;

        void OnDocumentTextChanged(object sender, EventArgs e) {
            foldingDirty = true;
        }

        void OnTextEntered(object sender, TextCompositionEventArgs e) {
            if(e.Text == ".") {
                // Open code completion after the user has pressed dot:
                completionWindow = new CompletionWindow(_texteditor.TextArea);
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;

                //data.Add(new MyCompletionData("Item1"));
                //data.Add(new MyCompletionData("Item2"));
                //data.Add(new MyCompletionData("Item3"));
                //completionWindow.Show();

                //completionWindow.Closed += delegate
                //{
                //    completionWindow = null;
                //};
            }
        }

        void OnTextEntering(object sender, TextCompositionEventArgs e) {
            if(e.Text.Length > 0 && completionWindow != null) {
                if(!char.IsLetterOrDigit(e.Text[0])) {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            // Do not set e.Handled=true.
            // We still want to insert the character that was typed.
        }

        ToolTip _toolTip = new ToolTip();

        void TextEditorMouseHover(object sender, MouseEventArgs e) {
            var pos = _texteditor.GetPositionFromPoint(e.GetPosition(_texteditor));

            if(pos != null) {

                var segment = _projectitem.TokenService.QueryCodeSegmentAt(_projectitem.Document.GetOffset(pos.Value.Line, pos.Value.Column + 1));

                var msg = string.Format("[{0}] {1}", segment.Type, segment.TokenString);
                _toolTip.PlacementTarget = _texteditor; // required for property inheritance
                _toolTip.Content = msg;
                _toolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse;
                _toolTip.IsOpen = true;
                e.Handled = true;
            }
        }

        void TextEditorMouseHoverStopped(object sender, MouseEventArgs e) {
            _toolTip.IsOpen = false;
        }


        void foldingUpdateTimer_Tick(object sender, EventArgs e) {
            if(foldingDirty && !_texteditor.Document.IsInUpdate && _foldingStrategy != null) {
                foldingDirty = false;
                _foldingStrategy.UpdateFoldings(_foldingManager, _texteditor.Document);
            }
        }
        #endregion

    }
}
