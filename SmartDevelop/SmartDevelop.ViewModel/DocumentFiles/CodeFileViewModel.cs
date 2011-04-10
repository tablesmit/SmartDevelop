﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using WPFCommon.ViewModels;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.CodeCompletion;
using System.Windows.Controls;
using SmartDevelop.Model.Projecting;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Folding;
using SmartDevelop.ViewModel.Folding;

namespace SmartDevelop.ViewModel.DocumentFiles
{
    public class CodeFileViewModel : WorkspaceViewModel
    {
        readonly ProjectItemCode _projectitem;
        readonly TextEditor _texteditor = new TextEditor();
        FoldingManager foldingManager;

        AbstractFoldingStrategy foldingStrategy = new BraceFoldingStrategy();

        public CodeFileViewModel(ProjectItemCode projectitem) {

            if(projectitem == null)
                throw new ArgumentNullException("projectitem");
            _projectitem = projectitem;

            _texteditor.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            _texteditor.FontSize = 15;

            _texteditor.Document = _projectitem.Document;

            _texteditor.SyntaxHighlighting = SyntaxHighlighterFinder.Find(projectitem.Type);

			if (foldingManager == null)
                foldingManager = FoldingManager.Install(_texteditor.TextArea);
            foldingStrategy.UpdateFoldings(foldingManager, _texteditor.Document);


            _texteditor.MouseHover += TextEditorMouseHover;
            _texteditor.TextArea.TextEntered += OnTextEntered;
            _texteditor.TextArea.TextEntering += OnTextEntering;

            DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
            foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
            foldingUpdateTimer.Tick += foldingUpdateTimer_Tick;
            foldingUpdateTimer.Start();
        }

        #region Properties

        public TextEditor Editor {
            get {
                return _texteditor;
            }
        }

        #endregion




        #region Event Handlers

        CompletionWindow completionWindow;

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
                //_toolTip.PlacementTarget = _texteditor; // required for property inheritance
                _toolTip.Content = pos.ToString();
                _toolTip.IsOpen = true;
                e.Handled = true;
            }
        }

        void TextEditorMouseHoverStopped(object sender, MouseEventArgs e) {
            _toolTip.IsOpen = false;
        }


        void foldingUpdateTimer_Tick(object sender, EventArgs e) {
            if(foldingStrategy != null) {
                foldingStrategy.UpdateFoldings(foldingManager, _texteditor.Document);
            }
        }
        #endregion

    }
}
