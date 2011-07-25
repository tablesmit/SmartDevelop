﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartDevelop.Model.DOM;
using System.CodeDom;
using SmartDevelop.Model.CodeLanguages;
using Archimedes.Patterns.Services;
using SmartDevelop.Model.CodeContexts;
using Archimedes.Patterns.Utils;

namespace SmartDevelop.Model.Projecting
{
    /// <summary>
    /// Represents a smart Project
    /// </summary>
    public class SmartCodeProject : ProjectItem
    {
        #region Fields

        readonly CodeDOMService _domservice;
        readonly CodeLanguage _language;

        #endregion

        #region Events

        public event EventHandler<ProjectItemEventArgs> ItemAdded;
        public event EventHandler<ProjectItemEventArgs> ItemRemoved;

        #endregion

        #region Constructor

        public SmartCodeProject(string name, CodeLanguage language) 
            : base(null) {

                ThrowUtil.ThrowIfNull(language);

            Name = name;
            _language = language;
            _domservice = language.CreateDOMService(this);
            
        }

        #endregion

        #region ProjectItem Access

        public void Add(ProjectItem item) {
            Children.Add(item);
            if(ItemAdded != null)
                ItemAdded(this, new ProjectItemEventArgs(item));

            if(item is ProjectItemCode) {
                ((ProjectItemCode)item).TokenizerUpdated += OnCodeFileTokenizerUpdated;
            }
        }

        public void Remove(ProjectItem item) {
            if(item is ProjectItemCode) {
                ((ProjectItemCode)item).TokenizerUpdated -= OnCodeFileTokenizerUpdated;
            }
            Children.Remove(item);
            if(ItemRemoved != null)
                ItemRemoved(this, new ProjectItemEventArgs(item));
        }

        public IEnumerable<ProjectItem> GetAllItems() {
            return new List<ProjectItem>(Children);
        }

        public IEnumerable<T> GetAllItems<T>()
            where T : ProjectItem {
                return from i in Children
                   where i is T
                   select i as T;
        }

        #endregion

        #region Properties

        public CodeDOMService DOMService {
            get { return _domservice; }
        }

        public CodeLanguage Language {
            get { return _language; }
        }


        /// <summary>
        /// Returns itself ;) 
        /// (children delegate their Project Get up to the parent root)
        /// </summary>
        public override SmartCodeProject Project {
            get {
                return this;
            }
        }

        string _name="";
        public override string Name {
            get { return _name; }
            set { _name = value; }
        }

        #endregion

        #region Event Handlers

        void OnCodeFileTokenizerUpdated(object sender, EventArgs e) {
                _domservice.CompileTokenFile((ProjectItemCode)sender, _domservice.RootType);
        }



        #endregion
    }

    public class ProjectItemEventArgs : EventArgs
    {
        readonly ProjectItem _p;

        public ProjectItemEventArgs(ProjectItem p) {
            _p = p;
        }
        public ProjectItem Project {
            get { return _p; }
        }
    }

}
