﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;
using System.Windows;
using SmartDevelop.Model.Projecting;
using System.CodeDom;
using SmartDevelop.TokenizerBase;
using SmartDevelop.Model.DOM.Types;
using SmartDevelop.Model.Tokenizing;
using SmartDevelop.Model.DOM;

namespace SmartDevelop.ViewModel.TextTransformators
{
    class ContextHighlightTransformator : DocumentColorizingTransformer
    {
        readonly ProjectItemCodeDocument _codeProject;
        Brush _classtypeBrush = new SolidColorBrush(Colors.CadetBlue);
        Brush _stringBrush = new SolidColorBrush(Colors.Crimson);
        Brush _errorBrush = new SolidColorBrush(Color.FromArgb(0xAA, 0xFF, 0x00, 0x00));
        //readonly CodeDOMService _domservice;

        public ContextHighlightTransformator(ProjectItemCodeDocument codeProject) {
            _codeProject = codeProject;
            //_domservice = _codeProject.Project.DOMService;
        }


        protected override void ColorizeLine(ICSharpCode.AvalonEdit.Document.DocumentLine line) {

            var codeline = _codeProject.SegmentService.QueryCodeTokenLine(line.LineNumber);

            if(codeline.IsEmpty)
                return;

            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);


            foreach(var segment in codeline.CodeSegments) {


                if(segment.Token == Token.TraditionalString) {

                    try {

                        base.ChangeLinePart(
                            segment.Range.Offset, // startOffset
                            segment.Range.EndOffset, // endOffset
                            (VisualLineElement element) => {
                                Typeface tf = element.TextRunProperties.Typeface;
                                element.TextRunProperties.SetForegroundBrush(_stringBrush);
                            });
                    } catch { }
                } else if(segment.CodeDOMObject is CodeMethodReferenceExpressionEx) {

                    var methodRef = segment.CodeDOMObject as CodeMethodReferenceExpressionEx;
                    if(methodRef.ResolvedMethodMember != null) {
                        try {
                            base.ChangeLinePart(
                            segment.Range.Offset, // startOffset
                            segment.Range.EndOffset, // endOffset
                            (VisualLineElement element) => {
                                // This lambda gets called once for every VisualLineElement
                                // between the specified offsets.
                                Typeface tf = element.TextRunProperties.Typeface;
                                // Replace the typeface with a modified version of
                                // the same typeface
                                element.TextRunProperties.SetTypeface(new Typeface(
                                    tf.FontFamily,
                                    FontStyles.Italic,
                                    FontWeights.Bold,
                                    tf.Stretch
                                ));
                            });
                        } catch {

                        }
                    }

                } else if(segment.CodeDOMObject is CodeTypeReferenceEx) {

                    if(((CodeTypeReferenceEx)segment.CodeDOMObject).ResolvedTypeDeclaration != null) {
                        try {
                            base.ChangeLinePart(
                            segment.Range.Offset, // startOffset
                            segment.Range.EndOffset, // endOffset
                            (VisualLineElement element) => {
                                Typeface tf = element.TextRunProperties.Typeface;
                                element.TextRunProperties.SetForegroundBrush(_classtypeBrush);
                                element.TextRunProperties.SetTypeface(new Typeface(
                                    tf.FontFamily,
                                    FontStyles.Normal,
                                    FontWeights.Bold,
                                    tf.Stretch
                                ));
                            });
                        } catch {

                        }
                    }
                }
            }

        }

        void HandleSegmentError(CodeSegment segment) {
            try {
                base.ChangeLinePart(
                segment.Range.Offset, // startOffset
                segment.Range.EndOffset, // endOffset
                (VisualLineElement element) => {
                    // This lambda gets called once for every VisualLineElement
                    // between the specified offsets.
                    Typeface tf = element.TextRunProperties.Typeface;
                    // Replace the typeface with a modified version of
                    // the same typeface
                    element.TextRunProperties.SetBackgroundBrush(_errorBrush);
                });
            } catch {
            }
        }
    }
}
