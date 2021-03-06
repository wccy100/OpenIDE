﻿using DigitalRune.Windows.TextEditor;
using OpenIDE.Core;
using OpenIDE.Core.Dialogs;
using OpenIDE.Core.Extensibility;
using OpenIDE.Core.Extensibility.ScriptedProviders;
using OpenIDE.Core.ProjectSystem;
using OpenIDE.Core.Views;
using OpenIDE.Dialogs;
using System;
using System.IO;
using Telerik.WinControls;
using Telerik.WinControls.UI;
using Telerik.WinControls.UI.Docking;

namespace OpenIDE
{
    public partial class Form1 : RadForm
    {
        public Form1()
        {
            InitializeComponent();

            startpageDocument.Controls.Add(new StartPage(openMenuItem_Click, newProjectMenuItem_Click) { Dock = System.Windows.Forms.DockStyle.Fill });

            NotificationService.Init(radDesktopAlert1);

            ThemeResolutionService.ApplicationThemeName = "VisualStudio2012Dark";

            Workspace.Settings.Load();

            if(!Workspace.Settings.Get<bool>("BetaAccepted"))
            {
                new BetaKeyDialog(this).ShowDialog();
                Hide();
            }

            Workspace.Output = new Core.Contracts.Debug(outputTextBox);
            Workspace.PluginManager.Load(Environment.CurrentDirectory + "\\Plugins");

            foreach (var item in Workspace.PluginManager.Plugins)
            {
                foreach (var win in item.Windows)
                {
                    var tw = new ToolWindow(win.Value.Title);
                    var ctrl = win.Value.View.Build();
                    ctrl.Dock = System.Windows.Forms.DockStyle.Fill;

                    tw.Controls.Add(ctrl);

                    dock.AddDocument(tw);
                }
            }

            // add here loading from startup
            // file, project, solution

            var args = Environment.GetCommandLineArgs();
            if(args.Length > 0)
            {
                string file = null;

#if DEBUG
                if (args.Length > 1)
                {
                    file = args[1];
                }
#else
                file = args[0];
#endif

                if (file != null)
                {
                    switch (Path.GetExtension(file))
                    {
                        case ".sln":
                            Workspace.Solution = Solution.Load(file);
                            Workspace.SolutionPath = file;

                            startpageDocument.Hide();
                            solutionExplorerWindow.Show();

                            newProjectMenuItem.Enabled = true;
                            newFileMenuItem.Enabled = true;

                            explorerTreeView.Nodes.Clear();
                            explorerTreeView.Nodes.Add(SolutionExplorer.Build(Workspace.Solution, solutionContextMenu, projectContextMenu, fileContextMenu));

                            break;
                        case "proj":
                            var p = Project.Load(file);

                            explorerTreeView.Nodes.Clear();
                            explorerTreeView.Nodes.Add(SolutionExplorer.Build(p, projectContextMenu, fileContextMenu));

                            break;
                        default:
                            var f = new Core.ProjectSystem.File();
                            var shortF = Path.GetFileName(file);

                            f.Name = shortF;
                            f.Src = file;
                            f.ID = Utils.GetTemplateID(shortF);

                            explorerTreeView.Nodes.Clear();
                            explorerTreeView.Nodes.Add(SolutionExplorer.Build(f, fileContextMenu));

                            ItemTemplate np = null;
                            Plugin npp = null;

                            foreach (var item in Workspace.PluginManager.Plugins)
                            {
                                foreach (var it in item.ItemTemplates)
                                {
                                    if (it.ID == f.ID)
                                    {
                                        np = it;
                                        npp = item;
                                    }
                                }
                            }

                            var raw = System.IO.File.ReadAllBytes(file);

                            npp.Events.Fire("OnCreateItem", np, f, raw);

                            var doc = new DocumentWindow(f.Name);
                            doc.Controls.Add(ViewSelector.Select(np, raw, f, np.AutoCompletionProvider as IntellisenseProvider, doc).GetView());

                            AddDocument(doc);

                            startpageDocument.Hide();

                            break;
                    }
                }
            }

            RefreshLanguage();

            addItemContextItem.Click += radMenuItem13_Click;
        }

        private void AddDocument(DocumentWindow doc)
        {
            if (!Workspace.OpenedDocuments.ContainsKey(doc.Text))
            {
                Workspace.OpenedDocuments.Add(doc.Text, doc);
                dock.AddDocument(doc);
            }
            else
            {
                dock.ActivateWindow(Workspace.OpenedDocuments[doc.Text]);
            }
        }

        private void RefreshLanguage()
        {
            startpageDocument.Text = LanguageManager._("Startpage");
            solutionExplorerWindow.Text = LanguageManager._("SolutionExplorer");
            toolboxWindow.Text = LanguageManager._("Toolbox");
            propertiesWindow.Text = LanguageManager._("Properties");
            teamExplorerWindow.Text = LanguageManager._("Team Explorer");
            consoleWindow.Text = LanguageManager._("Console");
            errorsWindow.Text = LanguageManager._("Errors");
            outputWindow.Text = LanguageManager._("Output");

            fileMenuItem.Text = LanguageManager._("File");
            newMenuItem.Text = LanguageManager._("New");
            newSolutionMenuItem.Text = LanguageManager._("Solution");
            newProjectMenuItem.Text = LanguageManager._("Project");
            newFileMenuItem.Text = LanguageManager._("File");


            editMenuItem.Text = LanguageManager._("Edit");
            viewMenuItem.Text = LanguageManager._("View");
            toolsMenuItem.Text = LanguageManager._("Tools");
            windowMenuItem.Text = LanguageManager._("Window");
            helpMenuItem.Text = LanguageManager._("Help");

            checkforUpdatesMenuItem.Text = LanguageManager._("Check for Updates");
            aboutMenuItem.Text = LanguageManager._("About");

            openSolutionMenuItem.Text = LanguageManager._("Open Solution");
            exitMenuItem.Text = LanguageManager._("Exit");
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (Updater.IsUpdateAvailable()) {
                radDesktopAlert1.ContentText = LanguageManager._("An Update is available.");
                radDesktopAlert1.Show();
            }
        }

        private void radMenuItem11_Click(object sender, EventArgs e)
        {
            if(folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Workspace.Solution = new Solution();
                var name = Prompt.Show(LanguageManager._("Please specifiy an solution name"), LanguageManager._("Name"));
                Workspace.Solution.Name = name;

                explorerTreeView.Nodes.Clear();
                explorerTreeView.Nodes.Add(SolutionExplorer.Build(Workspace.Solution, solutionContextMenu, projectContextMenu, fileContextMenu));

                Directory.CreateDirectory(folderBrowserDialog1.SelectedPath + "\\" + name);

                Workspace.SolutionPath = folderBrowserDialog1.SelectedPath + "\\" + name + "\\" + name + ".sln";
                Workspace.Solution.Save(Workspace.SolutionPath);

                newProjectMenuItem.Enabled = true;
                newFileMenuItem.Enabled = true;

                startpageDocument.Hide();
                solutionExplorerWindow.Show();
            }
        }

        private void newProjectMenuItem_Click(object sender, EventArgs e)
        {
            var np = new NewProjectDialog();
            if (np.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var f = new Project();
                f.Name = np.Filename;
                f.Type = np.Type;
                
                Workspace.Solution.Projects.Add(f);

                explorerTreeView.Nodes.Clear();
                explorerTreeView.Nodes.Add(SolutionExplorer.Build(Workspace.Solution, solutionContextMenu, projectContextMenu, fileContextMenu));

                var path = new FileInfo(Workspace.SolutionPath).Directory.FullName;

                Directory.CreateDirectory(path + "\\" + f.Name);
                Directory.CreateDirectory(path + "\\" + f.Name + "\\Properties");

                np.Plugin.Events.Fire("OnCreateProject", f);

                Workspace.Solution.Save(Workspace.SolutionPath);
            }
        }

        private void openMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Solution (*.sln)|*.sln";

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Workspace.Solution = Solution.Load(openFileDialog1.FileName);
                Workspace.SolutionPath = openFileDialog1.FileName;

                startpageDocument.Hide();
                solutionExplorerWindow.Show();

                newProjectMenuItem.Enabled = true;
                newFileMenuItem.Enabled = true;

                explorerTreeView.Nodes.Clear();
                explorerTreeView.Nodes.Add(SolutionExplorer.Build(Workspace.Solution, solutionContextMenu, projectContextMenu, fileContextMenu));
            }
        }

        private void radMenuItem14_Click(object sender, EventArgs e)
        {
            Updater.Update();
        }

        private void radMenuItem13_Click(object sender, EventArgs e)
        {
            var np = new NewItemDialog();
            if (np.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var f = new Core.ProjectSystem.File();
                f.Name = np.Filename;
                f.Src = np.Filename;
                f.ID = np.Type;
                
                Workspace.SelectedProject.Files.Add(f);

                explorerTreeView.Nodes.Clear();
                explorerTreeView.Nodes.Add(SolutionExplorer.Build(Workspace.Solution, solutionContextMenu, projectContextMenu, fileContextMenu));

                Workspace.Solution.Save(Workspace.SolutionPath);
                var fi = new FileInfo(Workspace.SolutionPath).Directory.FullName + "\\" + f.Name;

                System.IO.File.WriteAllBytes(fi, np.Template.Raw);

                np.Plugin.Events.Fire("OnCreateItem", f, np.Template.Raw);

                var doc = new DocumentWindow(f.Name);
                doc.Controls.Add(ViewSelector.Select(np.Template, System.IO.File.ReadAllBytes(fi), f, np.Template.AutoCompletionProvider as IntellisenseProvider).GetView());

                AddDocument(doc);
            }
        }

        private void radTreeView1_SelectedNodeChanged(object sender, RadTreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                if (e.Node.Tag is Core.ProjectSystem.File)
                {
                    var n = e.Node.Parent;
                    Workspace.SelectedProject = n?.Tag as Project;

                    var f = e.Node.Tag as Core.ProjectSystem.File;

                    radPropertyGrid1.SelectedObject = f;
                }
                else
                {
                    Workspace.SelectedProject = explorerTreeView.SelectedNode?.Tag as Project;
                }
            }
        }

        private void radMenuItem10_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void explorerTreeView_NodeMouseDoubleClick(object sender, RadTreeViewEventArgs e)
        {
            var p = e.Node.Tag as PropertiesView;
            var f = e.Node.Tag as Core.ProjectSystem.File;

            if (p != null)
            {
                var v = new PropertiesView();

                var doc = new DocumentWindow(e.Node.Text);
                doc.Controls.Add(v.GetView());

                AddDocument(doc);
            }
            if(f != null)
            {
                ItemTemplate np = null;
                Plugin nn = null;

                foreach (var item in Workspace.PluginManager.Plugins)
                {
                    foreach (var it in item.ItemTemplates)
                    {
                        if(it.ID == f.ID)
                        {
                            np = it;
                            nn = item;
                        }
                    }
                }

                byte[] raw = null;

                if (Workspace.SelectedProject != null) { 
                    raw = System.IO.File.ReadAllBytes(new FileInfo(Workspace.SolutionPath).Directory.FullName + "\\" + f.Src);
                }
                else
                {
                    raw = System.IO.File.ReadAllBytes(f.Src);
                }

                nn?.Events.Fire("OnCreateItem", np, f, raw);

                var doc = new DocumentWindow(f.Name);
                doc.Controls.Add(ViewSelector.Select(np, raw, f, np?.AutoCompletionProvider as IntellisenseProvider, doc).GetView());

                AddDocument(doc);
            }
        }

        private void Form1_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
            Workspace.Settings.Save();
            System.Windows.Forms.Application.Exit();
        }

        private void pluginsMenuItem_Click(object sender, EventArgs e)
        {
            var dlg = new Pluginmanager();
            dlg.ShowDialog();
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            var n = new RadAboutBox1();
            n.ShowDialog();
        }

        private void explorerTreeView_NodeExpandedChanged(object sender, RadTreeViewEventArgs e)
        {
            var proj = e.Node.Tag as Project;
            if(proj != null)
            {
                proj.Expandet = e.Node.Expanded;
                Workspace.Solution.Save(Workspace.SolutionPath);
            }
        }

        private void optionsMenuItem_Click(object sender, EventArgs e)
        {
            var od = new OptionsDialog();
            od.ShowDialog();
        }

        private void dock_DockWindowAdded(object sender, DockWindowEventArgs e)
        {
            Workspace.CurrentDocument = e.DockWindow as DocumentWindow;
        }

        private void dock_SelectedTabChanged(object sender, SelectedTabChangedEventArgs e)
        {
            Workspace.CurrentDocument = e.DockWindow as DocumentWindow;
            var v = Workspace.CurrentDocument?.Controls[0] as TextEditorControl;

            Workspace.CurrentEditor = v;
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            var edit = Workspace.CurrentDocument?.Controls[0] as TextEditorControl;
            var f = ((EditorView)edit.Tag)?.File;

            Workspace.CurrentDocument.Text = Workspace.CurrentDocument.Text.Replace(" (*)", "");

            System.IO.File.WriteAllText(f.Src, edit.Text);
        }

        private void addItemContextItem_Click(object sender, EventArgs e)
        {
            radMenuItem13_Click(sender, e);
        }

        private void dock_DockWindowClosing(object sender, DockWindowCancelEventArgs e)
        {
            if (e.NewWindow is DocumentWindow && e.NewWindow.Name != "startpageDocument")
            {
                Workspace.OpenedDocuments.Remove(e.NewWindow.Text);
            }
        }

        private void renameSolutionContextItem_Click(object sender, EventArgs e)
        {
            var newName = Prompt.Show("Please enter a new Name", "Rename");

            Workspace.Solution.Name = newName;

            explorerTreeView.Nodes.Clear();
            explorerTreeView.Nodes.Add(SolutionExplorer.Build(Workspace.Solution, solutionContextMenu, projectContextMenu, fileContextMenu));

            Workspace.Solution.Save(Workspace.SolutionPath);
        }
    }
}