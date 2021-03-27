using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace D2LibraryManager
{
    public partial class Form1 : Form
    {
        string steamPath;
        string addonName;
        string addonGamePath
        {
            get
            {
                return steamPath + "\\steamapps\\common\\dota 2 beta\\game\\dota_addons\\" + addonName;
            }
        }
        string addonContentPath
        {
            get
            {
                return steamPath + "\\steamapps\\common\\dota 2 beta\\content\\dota_addons\\" + addonName;
            }
        }

        List<string> addonList = new List<string>();
        List<LibData> libList = new List<LibData>();

        string discr_blank = "";

        public Form1()
        {
            InitializeComponent();
            string keypath = @"Software\Valve\Steam";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keypath);
            steamPath = key.GetValue("SteamPath").ToString();
            List<string> addon_list = Directory.GetDirectories(steamPath + "\\steamapps\\common\\dota 2 beta\\game\\dota_addons").ToList();
            for (int i = 0; i < addon_list.Count; i++)
            {
                if (!File.Exists(addon_list[i] + "\\readonly_tools_asset_info.bin"))
                {
                    addonList.Add(addon_list[i].Replace(steamPath + "\\steamapps\\common\\dota 2 beta\\game\\dota_addons\\", ""));
                    comboBox1.Items.Add(addonList.Last<string>());
                }
            }
            if (File.Exists(steamPath + "\\steamapps\\common\\dota 2 beta\\game\\dota_addons\\dota2cfg.cfg"))
            {
                string def_str = File.ReadAllText(steamPath + "\\steamapps\\common\\dota 2 beta\\game\\dota_addons\\dota2cfg.cfg");
                def_str = def_str.Substring(def_str.IndexOf("default"));
                def_str = def_str.Substring(def_str.IndexOf('"') + 1);
                def_str = def_str.Substring(def_str.IndexOf('"') + 1);
                addonName = def_str.Substring(0, def_str.IndexOf('"'));
                if (!Directory.Exists(addonGamePath)) addonName = addonList.First();
            }
            else
            {
                addonName = addonList.First();
            }
            comboBox1.Text = addonName;
            string libs_dir = @"libs_data\libs_data-main";
            if (Directory.Exists(libs_dir))
            {
                if (File.Exists(libs_dir + "\\version.txt"))
                {
                    string nowversion = File.ReadAllText(libs_dir + "\\version.txt");
                    string webversion;
                    HttpWebRequest myHttwebrequest = (HttpWebRequest)HttpWebRequest.Create("https://raw.githubusercontent.com/Niker323/libs_data/main/version.txt");
                    HttpWebResponse myHttpWebresponse = (HttpWebResponse)myHttwebrequest.GetResponse();
                    StreamReader strm = new StreamReader(myHttpWebresponse.GetResponseStream());
                    webversion = strm.ReadToEnd();

                    if (nowversion != webversion)
                    {
                        DialogResult result = MessageBox.Show(
                            "Libs data version does not match\nInstalled version: " + nowversion + "\nLast version: " + webversion + "\nUpdate libs data?",
                            "", MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1
                        );

                        if (result == DialogResult.Yes)
                        {
                            Directory.Delete("libs_data", true);
                            UpdateLibsData();
                        }
                    }
                }
                else
                {
                    DialogResult result = MessageBox.Show(
                        "Libs data version file not found\nUpdate libs data?",
                        "", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1
                    );

                    if (result == DialogResult.Yes)
                    {
                        Directory.Delete("libs_data", true);
                        UpdateLibsData();
                    }

                }

                LoadLibsData(libs_dir);
            }
            else
            {
                UpdateLibsData();
            }
            for (int i = 0; i < libList.Count; i++)
            {
                if (libList[i].parent == null)
                {
                    CreateNodeAndParents(libList[i], treeView1.Nodes);
                }
            }
            if (File.Exists(@"blank.html"))
            {
                discr_blank = File.ReadAllText(@"blank.html");
            }
            else
            {
                MessageBox.Show("blank.html not found!");
            }
            UpdateNodes(treeView1.Nodes);
        }

        private void UpdateLibsData()
        {
            new WebClient().DownloadFile("https://github.com/Niker323/libs_data/archive/refs/heads/main.zip", "libs_data.zip");
            ZipFile.ExtractToDirectory("libs_data.zip", "libs_data");
            if (File.Exists("libs_data.zip")) File.Delete("libs_data.zip");
        }

        private void LoadLibsData(string libs_dir)
        {
            List<string> libs_list = Directory.GetDirectories(libs_dir).ToList();
            for (int i = 0; i < libs_list.Count; i++)
            {
                if (File.Exists(libs_list[i] + "\\data.json"))
                {
                    string lib_json = File.ReadAllText(libs_list[i] + "\\data.json");
                    JObject jObject = JObject.Parse(lib_json);
                    libList.Add(jObject.ToObject<LibData>());
                }
                if (File.Exists(libs_list[i] + "\\description.html"))
                {
                    libList.Last().description = File.ReadAllText(libs_list[i] + "\\description.html");
                }
                if (File.Exists(libs_list[i] + "\\install.txt"))
                {
                    libList.Last().install = File.ReadLines(libs_list[i] + "\\install.txt").ToArray();
                }
                libList.Last().path = libs_list[i];
            }
        }

        private void CreateNodeAndParents(LibData lib, TreeNodeCollection parent)
        {
            TreeNode newNode = parent.Add(lib.name);
            newNode.Tag = lib;
            for (int i = 0; i < libList.Count; i++)
            {
                if (libList[i].parent != null)
                {
                    if (lib.name == libList[i].parent)
                    {
                        CreateNodeAndParents(libList[i], newNode.Nodes);
                    }
                }
            }
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            addonName = comboBox1.Text;
            UpdateNodes(treeView1.Nodes);
        }

        private void UpdateNodes(TreeNodeCollection nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                UpdateNode(nodes[i]);
                UpdateNodes(nodes[i].Nodes);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectNode(treeView1.SelectedNode);
        }

        private void SelectNode(TreeNode node)
        {
            UpdateNode(treeView1.SelectedNode);
            LibData selectedLibData = (LibData)treeView1.SelectedNode.Tag;
            string ready_discr = discr_blank.Replace("{name}", selectedLibData.name);
            ready_discr = ready_discr.Replace("{source}", selectedLibData.source);
            ready_discr = ready_discr.Replace("{description}", selectedLibData.description);
            webBrowser1.DocumentText = ready_discr;
            menuStrip1.Items.Clear();
            if (selectedLibData.install != null)
            {
                if (IsInstalled(selectedLibData))
                {
                    ToolStripItem button = menuStrip1.Items.Add("Uninstall");
                    button.Click += OnUninstall;
                }
                else
                {
                    ToolStripItem button = menuStrip1.Items.Add("Install");
                    button.Click += OnInstall;
                }
            }
        }

        private void UpdateNode(TreeNode node)
        {
            LibData selectedLibData = (LibData)node.Tag;
            if (selectedLibData.install != null)
            {
                if (IsInstalled(selectedLibData))
                {
                    node.ForeColor = Color.LightGreen;
                }
                else
                {
                    node.ForeColor = Color.White;
                }
            }
        }

        private void OnInstall(object sender, EventArgs e)
        {
            LibData selectedLibData = (LibData)treeView1.SelectedNode.Tag;
            if (selectedLibData.link != null && selectedLibData.install != null)
            {
                if (Directory.Exists("library")) Directory.Delete("library", true);
                if (File.Exists("library.zip")) File.Delete("library.zip");

                //ToolStripProgressBar progress = new ToolStripProgressBar();
                //progress.Name = "progress";
                //progress.Minimum = 0;
                //progress.Maximum = 100;
                //menuStrip1.Items.Add(progress);
                ToolStripLabel label = new ToolStripLabel();
                label.Name = "progress_label";
                menuStrip1.Items.Add(label);

                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                client.DownloadFileAsync(new Uri(selectedLibData.link), "library.zip");

                //new WebClient().DownloadFileAsync(new Uri(selectedLibData.link), "library.zip");
                //ZipFile.ExtractToDirectory("library.zip", "library");
                //for (int i = 0; i < selectedLibData.install.Length; i++)
                //{
                //    DoLine(selectedLibData.install[i], false);
                //}
                //if (Directory.Exists("library")) Directory.Delete("library", true);
                //if (File.Exists("library.zip")) File.Delete("library.zip");
                //UpdateNode(treeView1.SelectedNode);
            }
            else
            {
                MessageBox.Show("Install error:" + Environment.NewLine + "Link not found");
            }
        }

        void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ToolStripLabel label = (ToolStripLabel)menuStrip1.Items.Find("progress_label", false)[0];
            if (label != null)
            {
                label.Text = "Downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive;
            }
            //ToolStripProgressBar progress = (ToolStripProgressBar)menuStrip1.Items.Find("progress", false)[0];
            //if (progress != null)
            //{
            //    progress.Value = e.ProgressPercentage;
            //}
        }

        void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                MessageBox.Show("Install error:" + Environment.NewLine + e.Error.Message);
            }
            else
            {
                //ToolStripProgressBar progress = (ToolStripProgressBar)menuStrip1.Items.Find("progress", false)[0];
                //if (progress != null)
                //{
                //    progress.Value = 100;
                //}
                LibData selectedLibData = (LibData)treeView1.SelectedNode.Tag;//need fix
                ZipFile.ExtractToDirectory("library.zip", "library");
                for (int i = 0; i < selectedLibData.install.Length; i++)
                {
                    DoLine(selectedLibData.install[i], false);
                }
                if (Directory.Exists("library")) Directory.Delete("library", true);
                if (File.Exists("library.zip")) File.Delete("library.zip");
                UpdateNode(treeView1.SelectedNode);
                SelectNode(treeView1.SelectedNode);
            }
        }

        private void OnUninstall(object sender, EventArgs e)
        {
            LibData selectedLibData = (LibData)treeView1.SelectedNode.Tag;
            if (selectedLibData.install != null)
            {
                for (int i = 0; i < selectedLibData.install.Length; i++)
                {
                    DoLine(selectedLibData.install[i], true);
                }
                SelectNode(treeView1.SelectedNode);
            }
        }

        private void DoLine(string str, bool reverse)
        {
            try
            {
                string[] args = str.Split('@');
                if (reverse)
                {
                    if (args[0] == "addline")
                    {
                        File.WriteAllText(FormPath(args[1]), File.ReadAllText(FormPath(args[1])).Replace(args[2] + Environment.NewLine, ""));
                    }
                    else if (args[0] == "addlineafter")
                    {
                        File.WriteAllText(FormPath(args[1]), File.ReadAllText(FormPath(args[1])).Replace(args[3], ""));
                    }
                    else if (args[0] == "copy")
                    {
                        File.Delete(FormPath(args[2]));
                    }
                }
                else
                {
                    if (args[0] == "addline")
                    {
                        File.WriteAllText(FormPath(args[1]), args[2] + Environment.NewLine + File.ReadAllText(FormPath(args[1])));
                    }
                    else if (args[0] == "addlineafter")
                    {
                        string file = File.ReadAllText(FormPath(args[1]));
                        if (file.IndexOf(args[2]) == -1)
                        {
                            if (str.IndexOf("@else@") != -1)
                            {
                                Debug.WriteLine(str.Substring(str.IndexOf("@else@") + 6));
                                DoLine(str.Substring(str.IndexOf("@else@") + 6), reverse);
                            }
                        }
                        else
                        {
                            File.WriteAllText(FormPath(args[1]), file.Replace(args[2], args[2] + Environment.NewLine + args[3]));
                        }
                    }
                    else if (args[0] == "copy")
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(FormPath(args[2])));
                        File.Copy(FormPath(args[1]), FormPath(args[2]), false);
                    }
                }
            }
            catch
            {
                MessageBox.Show("Install error:" + Environment.NewLine + str);
            }
        }

        private string FormPath(string str)
        {
            return str.Replace("{game}", addonGamePath).Replace("{content}", addonContentPath);
        }

        private bool IsInstalled(LibData lib)
        {
            if (lib.install != null)
            {
                for (int i = 0; i < lib.install.Length; i++)
                {
                    string[] args = lib.install[i].Split('@');
                    if (args[0] == "copy")
                    {
                        if (File.Exists(FormPath(args[2])))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    public class LibData
    {
        public string name;
        public string source;
        public string link;
        public string description;
        public string path;
        public string[] install;
        public string parent;
    }
}
