﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartDevelop.Model.Projecting;
using SmartDevelop.Model.CodeLanguages;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using SmartDevelop.Model.Errors;

namespace SmartDevelop.AHK.AHKv1.Projecting
{
    public class SmartCodeProjectAHK : SmartCodeProject
    {
        CodeLanguageAHKv1 _language;
        ProjectItemFolder _localLib;
        ProjectItemFolder _stdLibFolder;

        internal SmartCodeProjectAHK(string name, string location, CodeLanguage language)
            : base(name, location, language) {

                _language = language as CodeLanguageAHKv1;

                //UpdateStdLib();

                _language.Settings.SettingsChanged += (s, e) => {
                        //UpdateStdLib();
                    };
        }


        void UpdateStdLib() {

            foreach(var item in this.StdLib.GetAllItems()) {
                item.Remove(item);
            }

            var folder = Path.GetDirectoryName(_language.Settings.InterpreterPath);
            var dir = Path.Combine(folder, _language.Settings.StdLibName);

            if(Directory.Exists(dir)) {
                foreach(var file in Directory.GetFiles(dir)) {
                    if(_language.Extensions.Contains(Path.GetExtension(file))) {
                        var codeItem = ProjectItemCodeDocument.FromFile(file, this);
                        if(codeItem != null)
                            this.StdLib.Add(codeItem);

                    }
                }
            }

        }

        public override void Run() {
            string output = "";
            string errOutput = "";

            QuickSaveAll();

            Solution.ErrorService.ClearAllErrorsFrom(ErrorSource.External);
            Solution.ClearOutput();
            Solution.AddNewOutputLine("Running: " + this.StartUpCodeDocument.FilePath);

            if(this.StartUpCodeDocument != null) {

                // this.StartUpCodeDocument.FilePath

                if(!File.Exists(_language.Settings.InterpreterPath)) {

                    // Displays an OpenFileDialog so the user can select a Cursor.
                    var openFileDialog1 = new OpenFileDialog();
                    openFileDialog1.Filter = "Autohotkey Interpreter|*.exe";
                    openFileDialog1.Title = "Select yout Autohotkey Interpreter";

                    if(openFileDialog1.ShowDialog() == DialogResult.OK) {
                        _language.Settings.InterpreterPath = openFileDialog1.FileName;
                        _language.Settings.Save();
                    }
                }

                if(!File.Exists(_language.Settings.InterpreterPath)) {
                    output = "Can't find AHK Interpreter!";
                    return;
                }

                // Start the child process.
                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = _language.Settings.InterpreterPath;
                p.StartInfo.Arguments = "/ErrorStdOut " + @"""" + this.StartUpCodeDocument.FilePath + @"""";
                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                output = p.StandardOutput.ReadToEnd();
                errOutput = p.StandardError.ReadToEnd();
                p.WaitForExit();
            }
            var error = ParseInterpreterOutput(errOutput);
            if(error != null) {

                var tokenLine = error.CodeItem.SegmentService.QueryCodeTokenLine(error.StartLine);
                if(!tokenLine.IsEmpty){
                    foreach(var t in tokenLine.CodeSegments)
                        t.ErrorContext = error.Error;
                }
                Solution.ErrorService.Add(error);
                error.BringIntoView();
            }

            Solution.AddNewOutputLine(errOutput);
            return;
        }

        static Regex rx = new Regex(@"(.*) \((\d+)\) : ==> (.*)");
        ErrorItem ParseInterpreterOutput(string errOutput) {

            if(rx.IsMatch(errOutput)) {
                var m = rx.Match(errOutput);
                string filepath = m.Groups[1].Value;
                int linenum = int.Parse(m.Groups[2].Value);
                string description = m.Groups[3].Value + Environment.NewLine;

                int i = 0;
                foreach(var s in errOutput.Split('\n')) {
                    if(i > 0)
                        description += s.Trim() + Environment.NewLine;
                        i++;
                }

                var targetItem = this.Project.FindCodeDocumentByPath(filepath);

                if(targetItem != null) {
                    var error = new ErrorItem(linenum, targetItem, description.Trim())
                    {
                        Source = ErrorSource.External
                    };
                    return error;
                }
            }
            return null;
        }

        #region Specail Folders


        /// <summary>
        /// Gets the local lib from this project (actually from the current startup code document)
        /// </summary>
        public ProjectItemFolder LocalLib {
            get {
                if(this.StartUpCodeDocument != null && this.StartUpCodeDocument.Parent != null) {
                    return this.StartUpCodeDocument.Parent.Children
                            .Find(x => (
                                x is ProjectItemFolder &&
                                x.Name.Equals(_language.Settings.LocalLibName, StringComparison.InvariantCultureIgnoreCase)))
                                as ProjectItemFolder;
                }
                return null;
            }
        }

        public ProjectItemFolder StdLib {
            get { return _stdLibFolder; }
            internal set { _stdLibFolder = value; }
        }

        #endregion

        public override bool CanRun {
            get {
                return Solution != null && StartUpCodeDocument != null;
            }
        }

    }
}
