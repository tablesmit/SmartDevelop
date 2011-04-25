﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SmartDevelop.Model.Projecting
{
    public class ProjectItemFolder : ProjectItem
    {
        string _name;
        public ProjectItemFolder(string name, ProjectItem parent) 
            : base(parent) {

        }

        public override string Name {
            get {
                return _name;
            }
            set {
                _name = value;
            }
        }
    }
}