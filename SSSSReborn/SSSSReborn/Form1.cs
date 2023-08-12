using SSSSReborn.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace SSSSReborn
{
    public partial class Form1 : Form
    {
        public class Folders
        {
            public string Source { get; private set; }
            public string Target { get; private set; }

            public Folders(string source, string target)
            {
                Source = source;
                Target = target;
            }
        }

        string SSSSPath;
        string resourcepath;
        
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if(Settings.Default.resourcepath == "") 
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.Description = "Select SSSS Path";

                if(fbd.ShowDialog() == DialogResult.OK && Directory.Exists(fbd.SelectedPath))
                {
                    Settings.Default.resourcepath = fbd.SelectedPath + @"/resource";
                    Settings.Default.Save();

                    if (!Directory.Exists(Settings.Default.resourcepath))
                        Directory.CreateDirectory(Settings.Default.resourcepath);
                }
            }

            resourcepath = Properties.Settings.Default.resourcepath;
            SSSSPath = Path.Combine(resourcepath, @"../");

            string SSSSPatcherPath = SSSSPath + @"/xinput1_3.dll";

            if (!File.Exists(SSSSPatcherPath) && MessageBox.Show("SSSSPatcher not found, do you want the tool to install it automatically?", "SSSSPatcher not found", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("SSSSReborn.Blobs.xinput1_3.dll"))
                {
                    using (var file = new FileStream(SSSSPatcherPath, FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(file);
                    }
                }
            }

            loadLvItems();
        }
        private void loadLvItems()
        {
            if (Properties.Settings.Default.modlist == null)
            {
                Properties.Settings.Default.modlist = new StringCollection();
            }

            this.lvMods.Items.AddRange((from i in Properties.Settings.Default.modlist.Cast<string>()
                                        select new ListViewItem(i.Split('|'))).ToArray());
        }
        private void saveLvItems()
        {
            Properties.Settings.Default.modlist = new StringCollection();
            Properties.Settings.Default.modlist.AddRange((from i in this.lvMods.Items.Cast<ListViewItem>()
                                                          select string.Join("|", from si in i.SubItems.Cast<ListViewItem.ListViewSubItem>()
                                                                                  select si.Text)).ToArray());
            Properties.Settings.Default.Save();
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        private void clearInstallationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you really want to clear your mod installation?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)==DialogResult.Yes)
            {
                Directory.Delete(resourcepath, true );
                Settings.Default.Reset();
                this.Close();
            }
        }
        private void installmod(string arg)
        {
            Clean();

            string temp = Properties.Settings.Default.resourcepath + @"\temp";

            if (Directory.Exists(temp) == false)
            {
                Directory.CreateDirectory(temp);
            }

            ZipFile.ExtractToDirectory(arg, temp);

            string xmlfile = temp + "//modinfo.xml";

            if (File.Exists(xmlfile))
            {
                string modname = File.ReadLines(xmlfile).First();
                string modauthor = File.ReadAllLines(xmlfile)[1];
                var lineCount = File.ReadLines(xmlfile).Count();
                string Modid = File.ReadAllLines(xmlfile).Last();
                var files = Directory.EnumerateFiles(temp, "*.*", SearchOption.AllDirectories);


                if (Directory.Exists(Properties.Settings.Default.resourcepath + @"\installed") == false)
                {
                    Directory.CreateDirectory(Properties.Settings.Default.resourcepath + @"\installed");
                    File.WriteAllLines(Properties.Settings.Default.resourcepath + @"\installed\" + modname + @".xml", files);
                }
                else
                {
                    File.WriteAllLines(Properties.Settings.Default.resourcepath + @"\installed\" + modname + @".xml", files);
                }

                string text = File.ReadAllText(Properties.Settings.Default.resourcepath + @"\installed\" + modname + @".xml");
                text = text.Replace(@"\temp", "");
                string txt = text;
                string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                string[] row = { modname, modauthor, "Replacer" };
                ListViewItem lvi = new ListViewItem(row);
                if (lvMods.Items.Contains(lvi) == false)
                {
                    foreach (string line in lines)
                    {
                        if (File.Exists(line))
                        {
                            if (MessageBox.Show("A mod containing file \"" + line + "\" is already installed, do you want to replace that file with the new one? \n\nWARNING: THIS COULD CORRUPT YOUR MODS INSTALLATION, ALWAYS KNOW WHAT YOU'RE DOING WHEN REPLACING STUFF", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                            {

                            }
                            else
                            {
                                if (File.Exists(Properties.Settings.Default.resourcepath + @"\installed\" + modname + @".xml"))
                                    File.Delete(Properties.Settings.Default.resourcepath + @"\installed\" + modname + @".xml");
                                Clean();
                                return;
                            }
                        }
                    }

                    lvMods.Items.Add(lvi);

                    string text2 = File.ReadAllText(Properties.Settings.Default.resourcepath + @"\installed\" + modname + @".xml");
                    text2 = text2.Replace(@"\temp", "");
                    File.WriteAllText(Properties.Settings.Default.resourcepath + @"\installed\" + modname + @".xml", text2);

                    MoveDirectory(temp, Properties.Settings.Default.resourcepath);
                }
                else
                {
                    Clean();
                    MessageBox.Show("A Mod with that name is already installed", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Clean();
                saveLvItems();
                MessageBox.Show("Installation Completed", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        public static void MoveDirectory(string source, string target)
        {
            var stack = new Stack<Folders>();
            stack.Push(new Folders(source, target));

            while (stack.Count > 0)
            {
                var folders = stack.Pop();
                Directory.CreateDirectory(folders.Target);
                foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
                {
                    string targetFile = Path.Combine(folders.Target, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }

                foreach (var folder in Directory.GetDirectories(folders.Source))
                {
                    stack.Push(new Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                }
            }
            Directory.Delete(source, true);
        }
        private void Clean()
        {
            if (File.Exists(Properties.Settings.Default.resourcepath + "//modinfo.xml"))
            {
                File.Delete(Properties.Settings.Default.resourcepath + "//modinfo.xml");
            }

            if (Directory.Exists(Properties.Settings.Default.resourcepath + "//temp"))
            {
                Directory.Delete(Properties.Settings.Default.resourcepath + "//temp", true);
            }
        }
        private void installModtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = ".ssssmod files | *.ssssmod";
            ofd.Title = "Install Mod";
            ofd.Multiselect = true;


            if (ofd.ShowDialog() == DialogResult.OK && ofd.FileNames.Length > 0)
            {
                foreach (string file in ofd.FileNames)
                {
                    if (MessageBox.Show("Do you want to install \"" + Path.GetFileNameWithoutExtension(file) + "\" ?", "Mod Installation", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        installmod(file);
                    }
                }
            }
            else
            {
                return;
            }
        }

        private void uninstallModToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indices = lvMods.SelectedIndices;
            if (indices.Count > 0)
            {
                Process p = new Process();
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = "cmd.exe";
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;
                info.RedirectStandardInput = true;
                info.UseShellExecute = false;

                if (File.Exists(Properties.Settings.Default.resourcepath + @"\installed\" + lvMods.SelectedItems[0].Text + @".xml"))
                {
                    string[] lines = File.ReadAllLines(Properties.Settings.Default.resourcepath + @"\installed\" + lvMods.SelectedItems[0].Text + @".xml");

                    foreach (string line in lines)
                    {
                        File.Delete(line);
                    }
                }

                processDirectory(Properties.Settings.Default.resourcepath);

                lvMods.Items.Remove(lvMods.SelectedItems[0]);
                saveLvItems();

                MessageBox.Show("Mod uninstalled Successfully", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        private static void processDirectory(string startLocation)
        {
            foreach (var directory in Directory.GetDirectories(startLocation))
            {
                processDirectory(directory);
                if (Directory.GetFiles(directory).Length == 0 &&
                    Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, false);
                }
            }
        }
    }
}
