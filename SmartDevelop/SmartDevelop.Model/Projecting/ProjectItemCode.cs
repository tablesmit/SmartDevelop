﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICSharpCode.AvalonEdit.Document;
using SmartDevelop.TokenizerBase.IA;
using System.Threading;
using System.Windows.Threading;
using System.IO;
using SmartDevelop.Model.Tokening;
using SmartDevelop.Model.CodeLanguages;
using Archimedes.Patterns.Services;
using SmartDevelop.TokenizerBase;


namespace SmartDevelop.Model.Projecting
{


    /// <summary>
    /// Represents a single Codefile
    /// </summary>
    public class ProjectItemCode : ProjectItem
    {
        #region Fields

        readonly TextDocument _codedocument;

        readonly ICodeLanguageService _languageService = ServiceLocator.Instance.Resolve<ICodeLanguageService>();
        CodeLanguage _language;
        Tokenizer _tokenizer;
        DocumentCodeSegmentService _codeSegmentService;
        bool _documentdirty = false;
        bool _isModified = false;

        #endregion

        /// <summary>
        /// Raised when the background tokenizer has refreshed the tokens 
        /// </summary>
        public event EventHandler TokenizerUpdated;

        /// <summary>
        /// 
        /// </summary>
        public event EventHandler RequestTextInvalidation;

        public event EventHandler HasUnsavedChangesChanged;

        #region Constructors

        /// <summary>
        /// Attempts to load a code file
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static ProjectItemCode FromFile(string filepath, ProjectItem parent) {
            if(!File.Exists(filepath))
                return null;

            var language = ServiceLocator.Instance.Resolve<ICodeLanguageService>().GetByExtension(Path.GetExtension(filepath));
            ProjectItemCode newp = new ProjectItemCode(language, parent);
            newp.FilePath = filepath;
            try
            {
                using(StreamReader sr = new StreamReader(filepath))
                {
                    newp.Document.Text = sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                // Let the user know what went wrong.
                // to do
            }
            newp.HasUnsavedChanges = false;
            newp.Document.UndoStack.ClearAll();
            return newp;
        }

        ProjectItemCode(CodeLanguage languageId, ProjectItem parent) 
            : base(parent) {

            if(languageId == null)
                throw new ArgumentNullException("languageId");

            _codedocument = new TextDocument();
            Encoding = Encoding.UTF8;
            _language = languageId; 
            _codedocument.Changed += OnCodedocumentChanged;

            _codeSegmentService = new DocumentCodeSegmentService(this);
            _tokenizer = _language.CreateTokenizer(_codedocument);

            _tokenizer.Finished += (s, e) => {
                _codeSegmentService.Reset(_tokenizer.GetSegmentsSnapshot());
                if(TokenizerUpdated != null) {
                    TokenizerUpdated(this, EventArgs.Empty);
                }
                OnRequestTextInvalidation();
            };


            

            DispatcherTimer tokenUpdateTimer = new DispatcherTimer();
            tokenUpdateTimer.Interval = TimeSpan.FromMilliseconds(200);
            tokenUpdateTimer.Tick += CheckUpdateTokenRepresentation;
            tokenUpdateTimer.Start();
        }

        #endregion

        public string FilePath {
            get;
            set;
        }

        public override string Name {
            get {
                if(string.IsNullOrEmpty(FilePath)) {
                    return "New Item";
                } else {
                    return Path.GetFileName(FilePath);
                }
            }
            set { }
        }

        #region Save the document

        /// <summary>
        /// Saves the text to the stream.
        /// </summary>
        /// <remarks>
        /// This method sets <see cref="HasUnsavedChanges"/> to false.
        /// </remarks>
        public void Save(Stream stream) {
            if(stream == null)
                throw new ArgumentNullException("stream");
            StreamWriter writer = new StreamWriter(stream, Encoding);
            writer.Write(_codedocument.Text);
            writer.Flush();
            // do not close the stream
            this.HasUnsavedChanges = false;
        }


        public Encoding Encoding {
            get;
            set;
        }

        /// <summary>
        /// Saves the text to the file.
        /// </summary>
        public void Save(string fileName) {
            if(fileName == null)
                throw new ArgumentNullException("fileName");
            using(FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                Save(fs);
            }
            HasUnsavedChanges = false;
        }

        #endregion

        public CodeLanguage CodeLanguage {
            get { return _language; }
        }

        public TextDocument Document {
            get { return _codedocument; }
        }

        public DocumentCodeSegmentService SegmentService {
            get { return _codeSegmentService; }
        }

        /// <summary>
        /// This Method ensures that the Tokenizer has been updated with the current changes in this document.
        /// </summary>
        public void EnsureTokenizerHasWorked() {
            while(_tokenizer.IsBusy) {
                Thread.Sleep(10);
            }

            if(_documentdirty)
                _tokenizer.TokenizeSync();
        }


        void OnCodedocumentChanged(object sender, EventArgs e){
            _documentdirty = true;
            HasUnsavedChanges = true;
        }

        void CheckUpdateTokenRepresentation(object sender, EventArgs e) {
            UpdateTokenizer();
        }

        void UpdateTokenizer() {
            if(_documentdirty && !_tokenizer.IsBusy) {
                _documentdirty = false;
                _tokenizer.TokenizeAsync();
            }
        }

        void OnRequestTextInvalidation(){
            if(RequestTextInvalidation != null)
                    RequestTextInvalidation(this, EventArgs.Empty);
        }

        public bool HasUnsavedChanges { 
            get { return _isModified; } 
            private set {
                if(_isModified == value)
                    return;

                _isModified = value;
                if(HasUnsavedChangesChanged != null)
                    HasUnsavedChangesChanged(this, EventArgs.Empty);
            } 
        }

        public override string ToString() {
            return string.Format("{0} ({1})", this.Name);

        }
    }
}
