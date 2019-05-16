using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace HTML_5_Editor
{
    public partial class MainForm : Form
    {
        private List<string> fileList = new List<string>(); //Seznam všech otevřených souborů v projektu
        private List<string> openList = new List<string>(); //Seznam právě otevřených souborů
        private List<RichTextBox> htmlRTBs = new List<RichTextBox>(); //Seznam všech RichTextboxů

        private List<HTMLtag> htmlTags = new HTMLtag().CreateDatabase(); //Databáze HTML tagů
        private string[] global_atributy = new string[15];
        private string[] event_atributy = new string[72];

        [DllImport("user32.dll")]
        public static extern bool LockWindowUpdate(int hWndLock);

        public MainForm()
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            LoadStartUpListBoxes();

            imageList1.Images.Add(Image.FromFile("icons8-folder-23.png"));          //ikona 0 - složky
            imageList1.Images.Add(Image.FromFile("icons8-source-code-64.png"));     //ikona 1 - html
            imageList1.Images.Add(Image.FromFile("css.png"));                       //ikona 2 - CSS
            imageList1.Images.Add(Image.FromFile("js.png"));                        //ikona 3 - JavaScript
            imageList1.Images.Add(Image.FromFile("icons8-picture-64.png"));         //ikona 4 - obrázky
            imageList1.Images.Add(Image.FromFile("file.png"));                      //ikona 5 - další soubory

            StreamReader sr = new StreamReader("global.txt");   //Načtení obecných atributů
            for (int i = 0; !sr.EndOfStream; i++)
            {
                global_atributy[i] = sr.ReadLine();
            }
            sr.Close();
            sr = new StreamReader("event.txt");    //Načtení událostných atributů
            for (int i = 0; !sr.EndOfStream; i++)
            {
                event_atributy[i] = sr.ReadLine();
            }
            sr.Close();
        }

        private string NodeName(string cesta)
        {
            string nazev = "";
            for (int i = cesta.Length - 1; cesta[i] != '\\'; i--) nazev = cesta[i] + nazev;
            return nazev;
        }

        private void LoadStartUpListBoxes() //načtení souborů a složek do listboxů
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();

            StreamReader sr = new StreamReader("lastfiles.txt");
            while (!sr.EndOfStream) listBox2.Items.Add(sr.ReadLine());
            sr.Close();

            sr = new StreamReader("lastprojects.txt");
            while (!sr.EndOfStream) listBox1.Items.Add(sr.ReadLine());
            sr.Close();
        }

        int filecount = 0, foldercount = 0;

        private void GetTreeNodeFiles(TreeNode treeNode, string cesta) //Načíst soubory do stromové struktury
        {
            var files = Directory.GetFiles(cesta, "*.*");
            foreach (string file in files)
            {
                fileList.Add(file);
                filecount++;
                string filename = NodeName(file).ToLower();
                TreeNode tree = new TreeNode(NodeName(file));
                if (filename.Contains(".html"))
                {
                    tree.ImageIndex = 1;
                    tree.SelectedImageIndex = 1;
                }
                else if (filename.Contains(".css")) { tree.ImageIndex = 2; tree.SelectedImageIndex = 2; }
                else if (filename.Contains(".js")) { tree.ImageIndex = 3; tree.SelectedImageIndex = 3; }
                else if (filename.Contains(".png") || filename.Contains(".jpg") || filename.Contains(".gif") || filename.Contains(".jfif") || filename.Contains(".bmp") || filename.Contains(".svg") || filename.Contains(".tiff") || filename.Contains(".wmf"))
                { tree.ImageIndex = 4; tree.SelectedImageIndex = 4; }
                else { tree.ImageIndex = 5; tree.SelectedImageIndex = 5; }
                treeNode.Nodes.Add(tree);
            }
        }

        private void GetTreeNodeFolders(TreeNode treeNode, string item) //Načíst složky do stromové struktury
        {
            var dir = Directory.GetDirectories(item);
            int i = 0;
            foreach (var subdir in dir)
            {
                try
                {
                    TreeNode tree = new TreeNode(NodeName(subdir));
                    treeNode.Nodes.Add(tree);
                    foldercount++;
                    var dir2 = Directory.GetDirectories(subdir);
                    if (dir2.Count() > 0) GetTreeNodeFolders(treeNode.Nodes[i], subdir);
                    GetTreeNodeFiles(treeNode.Nodes[i], subdir);
                    i++;
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Nepovolený přístup do složky \"" + subdir + "\"", "Chyba při načítační", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateLatestFiles(string cesta)
        {
            List<string> soubory = new List<string> { cesta };
            StreamReader sr = new StreamReader("lastfiles.txt");
            for (int i = 0; i < 31 && !sr.EndOfStream; i++)
            {
                string line = sr.ReadLine();
                bool zapsat = true;
                foreach (string item in soubory)
                {
                    if (item == line) { zapsat = false; break; }
                }
                if (zapsat) soubory.Add(line);
            }
            sr.Close();

            StreamWriter sw = new StreamWriter("lastfiles.txt");
            foreach (string item in soubory)
            {
                sw.WriteLine(item);
            }
            sw.Close();
        }

        private void LoadFile(string cesta, string parent) //načtení souboru a vytvoření RichTextBoxu
        {
            bool is_open = false;
            foreach (string item in openList)
            {
                if (item == cesta) { is_open = true; break; }
            }
            if (!is_open)
            {
                Cursor.Current = Cursors.WaitCursor;
                validationTimer.Stop();
                if (parent == "") tabControl1.TabPages.Add(NodeName(cesta));
                else tabControl1.TabPages.Add(parent + "/" + NodeName(cesta));
                RichTextBox richTextBox = NewRichTextBox(cesta, parent + "/" + NodeName(cesta));
                tabControl1.TabPages[tabControl1.TabCount - 1].Controls.Add(richTextBox);
                openList.Add(cesta);
                LineCounter();
                UpdateLatestFiles(cesta);
                ValidateRichTextBox();
                if (!flowLayoutPanel1.Visible) flowLayoutPanel1.Visible = true;
                htmlRTBs[tabControl1.SelectedIndex].SelectionStart = 0;
                htmlRTBs[tabControl1.SelectedIndex].ScrollToCaret();
                tabControl1.SelectTab(tabControl1.TabCount - 1);
                richTextBox.Focus();
                validationTimer.Start();
                Cursor.Current = Cursors.Default;
            }
        }

        private void OpenNode() //Otevření souboru ze stromové struktury
        {
            if (treeView1.SelectedNode != null)
            {
                foreach (string item in fileList)
                {
                    if (item.Contains(treeView1.SelectedNode.Parent.Text + "\\" + treeView1.SelectedNode.Text))
                    {
                        if (treeView1.SelectedNode.ImageIndex >= 1 && treeView1.SelectedNode.ImageIndex <= 3)
                        {
                            if (treeView1.SelectedNode.Parent != treeView1.Nodes[0])
                            {
                                LoadFile(item, treeView1.SelectedNode.Parent.Text);
                            }
                            else LoadFile(item, "");
                        }
                        else if (treeView1.SelectedNode.ImageIndex == 4 || treeView1.SelectedNode.ImageIndex == 5)
                        {
                            if (tabControl1.TabCount == 0) panelStart.Visible = true;
                            try { Process.Start(item); }
                            catch (System.ComponentModel.Win32Exception)
                            {
                                MessageBox.Show("Chyba při otevírání souboru " + item, "",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        break;
                    }
                }
            }
        }

        private void Save()
        {
            if (tabControl1.TabCount > 0)
            {
                ValidateRichTextBox();
                if (!labelShowErrors.Text.Contains("žádné")) //Validace při ukládání
                {
                    if (MessageBox.Show("Soubor \"" + tabControl1.SelectedTab.Text + "\" není validní.\nPřesto uložit?", "",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                    {
                        contextMenuSave.Items[0].Enabled = true;
                        return;
                    }
                }
                StreamWriter sw = new StreamWriter(openList[tabControl1.SelectedIndex], false);
                RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                foreach (string line in rtb.Lines) sw.WriteLine(line);
                sw.Close();
                if (tabControl1.SelectedTab.Text.Contains('*'))
                {
                    tabControl1.SelectedTab.Text = tabControl1.SelectedTab.Text.Remove(tabControl1.SelectedTab.Text.Length - 1);
                }
                contextMenuSave.Items[0].Enabled = false;
            }
        }

        string lastPath;

        private void SaveAs()
        {
            if (tabControl1.TabCount > 0)
            {
                ValidateRichTextBox();
                if (!labelShowErrors.Text.Contains("žádné")) //Validace při ukládání
                {
                    if (MessageBox.Show("Soubor \"" + tabControl1.SelectedTab.Text + "\" není validní.\nPřesto uložit?", "",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                        return;
                }
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    StreamWriter sw = new StreamWriter(saveFileDialog1.FileName, false);
                    RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                    foreach (string line in rtb.Lines) sw.WriteLine(line);
                    sw.Close();
                    if (tabControl1.SelectedTab.Text.Contains('*'))
                    {
                        tabControl1.SelectedTab.Text = tabControl1.SelectedTab.Text.Remove(tabControl1.SelectedTab.Text.Length - 1);
                    }
                    tabControl1.SelectedTab.Text = NodeName(saveFileDialog1.FileName);
                    openList.RemoveAt(tabControl1.SelectedIndex);
                    openList.Insert(tabControl1.SelectedIndex, saveFileDialog1.FileName);
                    UpdateLatestFiles(saveFileDialog1.FileName);

                    try
                    {
                        Cursor.Current = Cursors.WaitCursor;
                        fileList = new List<string>();
                        filecount = 0;
                        foldercount = 0;
                        treeView1.Nodes.Clear();
                        string cesta = lastPath;
                        TreeNode newNode = new TreeNode(NodeName(cesta));
                        treeView1.Nodes.Add(newNode);
                        GetTreeNodeFolders(newNode, cesta);
                        GetTreeNodeFiles(newNode, cesta);
                        labelFileFolderCount.Text = "Složky: " + foldercount.ToString() + "   |   Soubory: " + filecount.ToString();
                        fileList.Reverse();
                        treeView1.Nodes[0].Expand();
                    }
                    catch { }
                    finally { Cursor.Current = Cursors.Default; }
                }
            }
        }

        private void OpenFolder(string cesta)
        {
            Cursor.Current = Cursors.WaitCursor;
            if (tabControl1.TabCount > 0)
            {
                if (MessageBox.Show("Zavřít právě otevřené soubory?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    foreach (TabPage item in tabControl1.TabPages)
                        CloseFileCheck(item.Text);
                }
            }
            if (tabControl1.TabCount == 0)
            {
                fileList = new List<string>();
                openList = new List<string>();
                htmlRTBs = new List<RichTextBox>();
                tabControl1.TabPages.Clear();
            }
            treeView1.Nodes.Clear();
            foldercount = 0;
            filecount = 0;

            TreeNode newNode = new TreeNode(NodeName(cesta));
            treeView1.Nodes.Add(newNode);
            GetTreeNodeFolders(newNode, cesta);
            GetTreeNodeFiles(newNode, cesta);
            fileList.Reverse();
            treeView1.Nodes[0].Expand();
            foldercount++;
            labelFileFolderCount.Text = "Složky: " + foldercount.ToString() + "   |   Soubory: " + filecount.ToString();

            List<string> projekty = new List<string> { cesta };
            StreamReader sr = new StreamReader("lastprojects.txt");
            for (int i = 0; i < 31 && !sr.EndOfStream; i++)
            {
                string line = sr.ReadLine();
                bool zapsat_ = true;
                foreach (string item in projekty)
                {
                    if (item == line) { zapsat_ = false; break; }
                }
                if (zapsat_) projekty.Add(line);
            }
            sr.Close();

            StreamWriter sw = new StreamWriter("lastprojects.txt");
            foreach (string item in projekty)
            {
                sw.WriteLine(item);
            }
            sw.Close();

            LoadStartUpListBoxes();
            Cursor.Current = Cursors.Default;
        }

        private void OpenFile()
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                panelStart.Visible = false;
                LoadFile(openFileDialog1.FileName, "");
                tabControl1.SelectTab(tabControl1.TabCount - 1);
            }
        }

        private void ShowPreview()
        {
            if (tabControl1.TabCount > 0)
            {
                if (tabControl1.SelectedTab.Text.Contains('*') || tabControl1.SelectedTab.Text.Contains("Nový soubor"))
                {
                    if (MessageBox.Show("Soubor \"" + tabControl1.SelectedTab.Text + "\" nebyl uložen a nezobrazí se poslední úpravy."
                        + "\nUložit nyní?", "Uložit...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        if (tabControl1.SelectedTab.Text.Contains("Nový soubor")) SaveAs();
                        else Save();
                    }
                }
                if (File.Exists(openList[tabControl1.SelectedIndex])) Process.Start(openList[tabControl1.SelectedIndex]);
            }
        }

        private void CloseFileCheck(string tab) //Kontrola uložení souboru; zavření souboru
        {
            validationTimer.Stop();
            if (tab.Contains('*'))
            {
                tab = tab.Remove(tab.Length - 1);
                if (MessageBox.Show("Soubor \"" + tab + "\" nebyl uložen.\nUložit nyní?", "Uložit...",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (tab.Contains("Nový soubor")) SaveAs();
                    else Save();
                }
            }
            openList.RemoveAt(tabControl1.SelectedIndex);
            htmlRTBs.RemoveAt(tabControl1.SelectedIndex);
            tabControl1.TabPages.Remove(tabControl1.SelectedTab);
            labelTabCount.Text = "Otevřené soubory: " + tabControl1.TabCount.ToString();
            LineCounter();
            if (tabControl1.TabCount == 0)
            {
                panelStart.Visible = true;
                flowLayoutPanel1.Visible = false;
                LoadStartUpListBoxes();
            }
            else validationTimer.Start();
        }

        private RichTextBox NewRichTextBox(string cesta, string tabName)
        {
            RichTextBox rtb = new RichTextBox
            {
                Width = tabControl1.SelectedTab.Width,
                Height = tabControl1.SelectedTab.Height,
                ForeColor = Color.FromArgb(23, 23, 23),
                Font = new Font("Consolas", 11),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                WordWrap = false,
                AcceptsTab = true,
                ShortcutsEnabled = true
            };

            StreamReader sr = new StreamReader(cesta);
            while (!sr.EndOfStream) rtb.Text += sr.ReadLine() + "\n";
            sr.Close();

            if (cesta.Contains(".htm") || cesta.Contains(".php") || tabName == "Nový soubor")
                MatchAllHtml(rtb);
            rtb.Click += HtmlRichTextBox_Click;
            rtb.MouseDown += HtmlRichTextBox_MouseDown;
            rtb.MouseUp += HtmlRichTextBox_MouseUp;
            rtb.KeyUp += HtmlRichTextBox_KeyUp;
            rtb.KeyDown += HtmlRichTextBox_KeyDown;
            rtb.TextChanged += RichTextBox1_TextChanged;
            rtb.VScroll += HtmlRichTextBox_VScroll;

            htmlRTBs.Add(rtb);
            labelTabCount.Text = "Otevřené soubory: " + tabControl1.TabCount.ToString();
            return rtb;
        }

        private void NewFile()
        {
            validationTimer.Stop();
            tabControl1.TabPages.Add("Nový soubor");
            openList.Add("Nový soubor");
            RichTextBox rtb = NewRichTextBox("template.html", "Nový soubor");
            tabControl1.TabPages[tabControl1.TabCount - 1].Controls.Add(rtb);
            tabControl1.SelectTab(tabControl1.TabCount - 1);
            rtb.SelectionStart = 137;
            LineCounter();
            if (panelStart.Visible) panelStart.Visible = false;
            if (!flowLayoutPanel1.Visible) flowLayoutPanel1.Visible = true;
            rtb.Focus();
            validationTimer.Start();
        }

        private readonly insertTagForm insertTagForm1 = new insertTagForm();

        private void InsertTag()
        {
            if (tabControl1.TabCount > 0)
            {
                if (tabControl1.SelectedTab.Text.Contains(".htm") || tabControl1.SelectedTab.Text.Contains(".php")
                    || tabControl1.SelectedTab.Text.Contains("Nový soubor"))
                {
                    if (insertTagForm1.ShowDialog() == DialogResult.OK) //Otevření okna pro vybrání a vložení tagu
                    {
                        RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                        rtb.KeyUp -= HtmlRichTextBox_KeyUp;

                        string html_tag = htmlTags[insertTagForm1.SelectedTagIndex].Nazev;
                        string[] atributy = htmlTags[insertTagForm1.SelectedTagIndex].Atributy;

                        int select = rtb.SelectionStart;
                        if (html_tag != "<!--  -->")
                        {
                            rtb.SelectedText += '<' + html_tag;
                            if (insertTagForm1.VlozitAtributy() && atributy[0] != "nn")
                            {
                                foreach (string item in atributy)
                                {
                                    rtb.SelectedText += ' ';
                                    if (item.EndsWith("-")) rtb.SelectedText += item.Remove(item.Length - 1);
                                    else rtb.SelectedText += item + "=\"\"";
                                }
                            }
                            rtb.SelectedText += '>';
                        }
                        else //Vložení poznámky
                        {
                            rtb.SelectedText += html_tag;
                            rtb.SelectionStart = select + 5;
                            rtb.SelectionLength = 0;
                            MatchPartHtml(rtb);
                        }
                        rtb.Focus();
                        rtb.KeyUp += HtmlRichTextBox_KeyUp;
                    }
                }
                else MessageBox.Show("Do tohoto souboru nelze vložit HTML značku.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MatchAllHtml(RichTextBox rtb)
        {
            int selectionAt = rtb.SelectionStart;
            int selectionRange = rtb.SelectionLength;
            LockWindowUpdate(rtb.Handle.ToInt32());

            //Označení HTML tagů
            MatchCollection m = Regex.Matches(rtb.Text, @"(?></?\w+)(?>(?:[^>'""]+|'[^']*'|""[^""]*"")*)>",
                RegexOptions.IgnoreCase);
            foreach (Match match in m) HighLightTags(match, 0, rtb);
            rtb.SelectionStart = selectionAt;
            rtb.SelectionLength = selectionRange;
            rtb.SelectionColor = Color.Black;

            try //Označení poznámky nakonec
            {
                string text = rtb.Text;
                bool poznamka_nalezena = false;
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '-' && text[i - 1] == '!' && text[i - 2] == '<')
                    {
                        poznamka_nalezena = true;
                        rtb.SelectionStart = i - 3;
                    }
                    if (poznamka_nalezena && text[i] == '>' && text[i - 1] == '-' && text[i - 2] == '-')
                    {
                        rtb.SelectionLength = i - rtb.SelectionStart + 1;
                        rtb.SelectionColor = Color.Green;
                        poznamka_nalezena = false;
                    }
                }
            }
            catch { }
            LockWindowUpdate(0);
        }

        private void MatchPartHtml(RichTextBox rtb) //Označení HTMl tagu při psaní
        {
            bool tag = false;
            int startSelectionAt = rtb.SelectionStart;
            int StartSelctionRange = rtb.SelectionLength;
            int searchSelectionStart = startSelectionAt;

            for (int i = startSelectionAt - 1; i >= 0; i--) //Nalezení začátku HTML tagu, start indexu
            {
                if (rtb.Text[i] == '<' && rtb.Text.Length > i + 1)
                {
                    searchSelectionStart = i;
                    if (rtb.Text[i + 1] != '!') tag = true;
                    break;
                }
            }
            int searchSelectionRange = rtb.Text.Length - searchSelectionStart;
            for (int i = Math.Max(0, startSelectionAt - 1); i < rtb.Text.Length; i++) //Nalezení konce HTML tagu, délky tagu
            {
                if (rtb.Text[i] == '>')
                {
                    searchSelectionRange = i - searchSelectionStart + 1;
                    break;
                }
            }
            LockWindowUpdate(rtb.Handle.ToInt32());
            rtb.SelectionStart = searchSelectionStart;
            rtb.SelectionLength = searchSelectionRange;

            if (tag) //Označení HTML značky
            {
                Match match = Regex.Match(rtb.SelectedText,
                    @"(?></?\w+)(?>(?:[^>'""]+|'[^']*'|""[^""]*"")*)>",
                    RegexOptions.IgnoreCase);
                HighLightTags(match, rtb.SelectionStart, rtb);
            }
            else //Označení poznámky
            {
                try
                {
                    string text = rtb.Text;
                    bool poznamka_nalezena = false;
                    for (int i = 2; i < text.Length; i++)
                    {
                        if (text[i] == '-' && text[i - 1] == '!' && text[i - 2] == '<')
                        {
                            poznamka_nalezena = true;
                            rtb.SelectionStart = i - 3;
                        }
                        if (poznamka_nalezena && text[i] == '>' && text[i - 1] == '-' && text[i - 2] == '-')
                        {
                            rtb.SelectionLength = i - rtb.SelectionStart + 1;
                            rtb.SelectionColor = Color.Green;
                            poznamka_nalezena = false;
                        }
                    }
                }
                catch { }
            }

            rtb.SelectionStart = startSelectionAt;
            rtb.SelectionLength = StartSelctionRange;
            rtb.SelectionColor = Color.Black;

            LockWindowUpdate(0);
        }

        private void HighLightTags(Match match, int selectionStart, RichTextBox rtb)
        {
            int start = match.Index + selectionStart;
            int lenght = match.Length;
            rtb.SelectionStart = start;

            //začátek tagu: <, </
            if (rtb.Text[start + 1] == '/')
            {
                rtb.SelectionLength = 2;
                rtb.SelectionColor = Color.FromArgb(100, 100, 100);
                start += 2;
                lenght -= 2;
            }
            else
            {
                rtb.SelectionLength = 1;
                rtb.SelectionColor = Color.FromArgb(100, 100, 100);
                start++;
                lenght--;
            }
            //konec tagu: >, />
            if (rtb.Text[start + lenght - 2] == '/')
            {
                rtb.SelectionStart = start + lenght - 2;
                rtb.SelectionLength = 2;
                rtb.SelectionColor = Color.FromArgb(100, 100, 100);
                lenght -= 2;
            }
            else
            {
                rtb.SelectionStart = start + lenght - 1;
                rtb.SelectionLength = 1;
                rtb.SelectionColor = Color.FromArgb(100, 100, 100);
                lenght--;
            }
            try
            {
                //označení modře mezi < >
                rtb.SelectionStart = start;
                rtb.SelectionLength = lenght;
                rtb.SelectionColor = Color.FromArgb(60, 64, 198);
            }
            catch
            {
                rtb.SelectionLength = 0;
            }

            //označení dat atributů mezi " "
            for (int i = start; i <= (start + lenght); i++)
            {
            pokus:
                if (rtb.Text[i] == '\"')
                {
                    int tempStart = i;
                    i++;
                    for (; i <= (start + lenght); i++)
                    {
                        if (rtb.Text[i] == '\"')
                        {
                            rtb.SelectionStart = tempStart;
                            rtb.SelectionLength = i - tempStart + 1;
                            rtb.SelectionColor = Color.FromArgb(225, 112, 85);
                            i++;
                            goto pokus;
                        }
                    }
                }
            }
            rtb.SelectionStart = selectionStart;
        }

        private void LineCounter()
        {
            try
            {
                RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                int firstcharindex = rtb.GetFirstCharIndexOfCurrentLine();
                int currentline = rtb.GetLineFromCharIndex(firstcharindex) + 1;
                labelLineCounter.Text = "Řádek: " + currentline.ToString() + "/";
                if (rtb.Lines.Count() > 0)
                    labelLineCounter.Text += rtb.Lines.Count().ToString();
                else labelLineCounter.Text += (rtb.Lines.Count() + 1).ToString();
            }
            catch { labelLineCounter.Text = "Řádek: 0/0"; }
        }

        private void SelectLine(int line_number)
        {
            RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
            int row = 1;
            int charCount = 0;
            foreach (string line in rtb.Lines)
            {
                charCount += line.Length + 1;
                row++;
                if (row == line_number)
                {
                    rtb.SelectionStart = charCount;
                    rtb.ScrollToCaret();
                }
            }
        }

        private void GoToLine()
        {
            if (tabControl1.TabCount > 0)
            {
                gotoLineForm gtLF = new gotoLineForm();
                gtLF.ShowDialog();
                int select_line = gtLF.LineNumber;
                RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];

                rtb.KeyUp -= HtmlRichTextBox_KeyUp;

                if (select_line > rtb.Lines.Length)
                {
                    if (MessageBox.Show(select_line.ToString() + " je mimo rozsah vybraného souboru.", "Chyba",
                        MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry)
                    {
                        GoToLine();
                    }
                }
                else
                {
                    SelectLine(select_line);
                    LineCounter();
                    rtb.Focus();
                }
                rtb.KeyUp += HtmlRichTextBox_KeyUp;
            }
        }

        private void StartOpenFolder() //Otevřít složku z listboxu
        {
            string path = listBox1.SelectedItem.ToString();
            if (Directory.Exists(path))
            {
                lastPath = path;
                OpenFolder(lastPath);
            }
            else
            {
                MessageBox.Show("Složka \"" + path + "\" neexistuje.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StreamWriter sw = new StreamWriter("lastprojects.txt");
                foreach (string item in listBox1.Items)
                {
                    if (item != path) sw.WriteLine(item);
                }
                sw.Close();
                LoadStartUpListBoxes();
            }
        }

        private void StartOpenFile() //Otevřít soubor z listboxu
        {
            string path = listBox2.SelectedItem.ToString();
            if (File.Exists(path))
            {
                panelStart.Visible = false;
                LoadFile(path, "");
                LineCounter();
            }
            else
            {
                MessageBox.Show("Soubor \"" + path + "\" neexistuje.", "Chyba", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StreamWriter sw = new StreamWriter("lastfiles.txt");
                foreach (string item in listBox2.Items)
                {
                    if (item != path) sw.WriteLine(item);
                }
                sw.Close();
                LoadStartUpListBoxes();
            }

        }

        public int pocet_chyb;
        private readonly ErrorListForm errorForm = new ErrorListForm();

        private void ValidateLine(int i) //Validace jednoho řádku
        {
            RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
            string tag_ke_kontrole = "";
            bool zapisovat = false;
            errorForm.ClearLineLogs(i + 1, out int errorCount);
            pocet_chyb -= errorCount;

            for (int y = 0; y < rtb.Lines[i].Length; y++)
            {
                if (zapisovat) tag_ke_kontrole += rtb.Lines[i][y];
                if (rtb.Lines[i][y] == '>' || (zapisovat && y + 1 < rtb.Lines[i].Length && rtb.Lines[i][y + 1] == '<') 
                    || (tag_ke_kontrole != "" && y + 1 == rtb.Lines[i].Length))
                {
                    Regex regex = new Regex(@"(?></?\w+)(?>(?:[^>'""]+|'[^']*'|""[^""]*"")*)>", RegexOptions.IgnoreCase);
                    if (!regex.IsMatch(tag_ke_kontrole) && !tag_ke_kontrole.Contains("<!DOCTYPE") && tag_ke_kontrole.Contains("<")
                        && !tag_ke_kontrole.Contains("<!--") && !tag_ke_kontrole.Contains("-->"))
                    {
                        errorForm.ErrorLog((i + 1).ToString() + ". řádek: chyba zápisu " + tag_ke_kontrole);
                        pocet_chyb++;
                    }
                    else if (!tag_ke_kontrole.Contains("<!--") && !tag_ke_kontrole.Contains("-->"))
                    {
                        string nazev = "";
                        for (int j = 1; j < tag_ke_kontrole.Length; j++)
                        {
                            if (tag_ke_kontrole[j] == '/') continue;
                            if (tag_ke_kontrole[j] == ' ' || tag_ke_kontrole[j] == '>') break;
                            else nazev += tag_ke_kontrole[j];
                        }
                        bool valid = false;
                        foreach (HTMLtag item in htmlTags)
                        {
                            if (nazev == item.Nazev) { valid = true; break; }
                        }
                        if (!valid)
                        {
                            errorForm.ErrorLog((i + 1).ToString() + ". řádek: nevalidní značka " + tag_ke_kontrole);
                            pocet_chyb++;
                        }
                    }
                    rtb.SelectionLength = 0;
                    zapisovat = false;
                }
                if (rtb.Lines[i][y] == '<')
                {
                    zapisovat = true;
                    tag_ke_kontrole = "<";
                }
            }
        }

        private void UpdateValidationStatus(int start) //Zobrazení stavu validace (počet chyb, možnost zobrazit dialog s chybovými logy)
        {
            RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
            if (pocet_chyb > 0)
            {
                if (pocet_chyb == 1) labelShowErrors.Text = "Nalezena 1 chyba.";
                else if (pocet_chyb >= 2 && pocet_chyb <= 4) labelShowErrors.Text = "Nalezeny " + pocet_chyb.ToString() + " chyby.";
                else labelShowErrors.Text = "Nalezeno " + pocet_chyb.ToString() + " chyb.";
                labelValidImg.ImageIndex = 1;
                labelShowErrors.Cursor = Cursors.Hand;
            }
            else
            {
                labelShowErrors.Text = "Nenašly se žádné chyby.";
                labelValidImg.ImageIndex = 0;
                labelShowErrors.Cursor = Cursors.Default;
            }
            rtb.SelectionLength = 0;
            rtb.SelectionStart = start;
            rtb.Focus();
        }

        private void ValidateRichTextBox() //Validace celého RichTextBoxu
        {
            if (tabControl1.TabCount > 0)
            {
                Cursor.Current = Cursors.WaitCursor;
                RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                LockWindowUpdate(rtb.Handle.ToInt32());
                int startStart = rtb.SelectionStart;
                rtb.SelectionStart = 0;
                pocet_chyb = 0;
                errorForm.ClearListBox();

                for (int i = 0; i < rtb.Lines.Count(); i++)
                {
                    ValidateLine(i);
                }
                ValidateRequiredTags();
                LockWindowUpdate(0);
                UpdateValidationStatus(startStart);
                Cursor.Current = Cursors.Default;
            }
        }

        private void ValidatePartRTB() //Validace jednoho řádku při psaní - validationTimer tick
        {
            RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
            int firstcharindex = rtb.GetFirstCharIndexOfCurrentLine();
            int currentline = rtb.GetLineFromCharIndex(firstcharindex);
            int start = rtb.SelectionStart;
            ValidateLine(currentline);
            UpdateValidationStatus(start);
        }

        private void ValidateRequiredTags()
        {
            RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
            bool doctype = false, html_start = false, html_end = false;

            for (int i = 0; i < rtb.Lines.Count(); i++) //Nalezení !DOCTYPE a <html> od začátku souboru
            {
                if (rtb.Lines[i].Contains("<!DOCTYPE"))
                {
                    if (!doctype) doctype = true;
                    else
                    {
                        errorForm.ErrorLog((i + 1).ToString() + " řádek: definici dokumentu <!DOCTYPE> lze použít pouze jednou.");
                        pocet_chyb++;
                    }
                }
                if (rtb.Lines[i].Contains("<html")) html_start = true;

                if (doctype && html_start) break;
            }

            List<string> temp_list = errorForm.ReturnList();
            for (int i = 0; i < 3 && temp_list.Count > i; i++)
            {
                if (temp_list[i].StartsWith("Nesprávný") || temp_list[i].StartsWith("Chybí"))
                {
                    errorForm.RemoveLog(i);
                    pocet_chyb--;
                    i--;
                }
                temp_list = errorForm.ReturnList();
            }

            if (!doctype)
            {
                errorForm.InsertToStart("Nesprávný formát dokumentu. Doplňte na začátek \"<!DOCTYPE html>\".");
                pocet_chyb++;
            }
            if (!html_start)
            {
                errorForm.InsertToStart("Chybí element <html> vymezující html dokument.");
                pocet_chyb++;
            }

            for (int i = rtb.Lines.Count() - 1; i >= 0 && !html_end; i--) //Nalezení </html> od konce souboru
            {
                if (rtb.Lines.Length > 0)
                {
                    if (rtb.Lines[i].Contains("</html>"))
                    {
                        html_end = true;
                        break;
                    }
                }
            }
            if (!html_end)
            {
                errorForm.InsertToStart("Chybí koncová značka </html> vymezující html dokument.");
                pocet_chyb++;
            }
        }


        /*****************************************************************************************************************************************************/

        private void TreeView1_KeyDown(object sender, KeyEventArgs e) //Výběr souboru ze stromové struktury pomocí Enteru
        {
            try
            {
                if (e.KeyCode == Keys.Enter) OpenNode();
            }
            catch { }
        }

        private void TreeView1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            panelStart.Visible = false;
            OpenNode();
            LineCounter();
        }

        private void ButtonVyberKOtevreni_Click(object sender, EventArgs e) //Button pro výběr složky nebo souboru k otevření
        {
            contextMenuOpen.Show(Cursor.Position);
        }

        private void OtevritSlozkuToolStripMenuItem2_Click(object sender, EventArgs e) //Otevřít složku
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                lastPath = folderBrowserDialog1.SelectedPath;
                OpenFolder(lastPath);
            }
        }

        private void OtevritSouborToolStripMenuItem1_Click(object sender, EventArgs e) //Otevřít soubor
        {
            OpenFile();
        }

        private void ButtonNovyDokument_Click(object sender, EventArgs e) //Tlačítko pro vytvoření nového HTML dokumentu
        {
            NewFile();
        }

        private void UložitJakoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }

        private void UložitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void SaveButton_Click(object sender, EventArgs e) //Save Button
        {
            contextMenuSave.Show(Cursor.Position);
        }

        private void NahledButton_Click(object sender, EventArgs e) //Zobrazení náhledu HTML souboru
        {
            ShowPreview();
            //Form2 form2 = new Form2(openList[tabControl1.SelectedIndex]);
            //form2.ShowDialog();
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.TabCount > 0)
            {
                if (!tabControl1.SelectedTab.Text.Contains("Nový soubor") && tabControl1.SelectedTab.Text.Contains('*'))
                {
                    contextMenuSave.Items[0].Enabled = true;
                }
                else contextMenuSave.Items[0].Enabled = false;
            }
            else contextMenuSave.Items[0].Enabled = false;

            LineCounter();
            ValidateRichTextBox();
        }

        private void ZavritZalozkuToolStripMenuItem_Click(object sender, EventArgs e) //Zavřít aktuální záložku
        {
            CloseFileCheck(tabControl1.SelectedTab.Text);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            tabControl1.SelectedIndexChanged -= TabControl1_SelectedIndexChanged;
            foreach (TabPage item in tabControl1.TabPages) CloseFileCheck(item.Text);
        }

        bool backspacePressed = false;
        bool kontextovaNapoveda = false;
        int start_nap = 0;
        string hledano = "";
        int sem_vlozit = 0;
        int texChanCount = 0;
        bool napoveda_atribut = false;
        bool blockTextChanged = false;

        private void RichTextBox1_TextChanged(object sender, EventArgs e)
        {
            if (!blockTextChanged)
            {
                RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                if (!tabControl1.SelectedTab.Text.Contains('*'))
                {
                    tabControl1.SelectedTab.Text += '*';
                }
                if (!tabControl1.SelectedTab.Text.Contains("Nový soubor"))
                {
                    contextMenuSave.Items[0].Enabled = true;
                }
                try
                {
                    //Automatické vkládání koncových značek
                    bool koncovyTag = false;
                    bool tag_s_atributem = false;
                    if (rtb.Text[rtb.SelectionStart - 1] == '>' && !backspacePressed)
                    {
                        listBoxIntellisense.Items.Clear();
                        int start = rtb.SelectionStart;
                        int start1 = start;
                        int length = 0;
                        for (int i = rtb.SelectionStart; rtb.Text[i] != '<'; i--)
                        {
                            if (rtb.Text[i] == ' ')
                            {
                                start1 = i;
                                length = 0;
                                tag_s_atributem = true;
                            }
                            if (rtb.Text[i] == '/' && rtb.Text[i - 1] == '<')
                            {
                                koncovyTag = true;
                                break;
                            }
                            length++;
                        }
                        if (!koncovyTag)
                        {
                            rtb.SelectionStart = start1 - length;
                            rtb.SelectionLength = length;

                            string tag = rtb.SelectedText;
                            if (tag_s_atributem) tag += ">";
                            tag = tag.Remove(0, 1);

                            foreach (HTMLtag item in htmlTags)
                            {
                                if (tag.Remove(tag.Length - 1, 1) == item.Nazev)
                                {
                                    if (item.Parovy)
                                    {
                                        rtb.SelectionStart = start;
                                        rtb.SelectionLength = 0;
                                        rtb.SelectedText += "</" + tag;
                                        MatchPartHtml(rtb);
                                    }
                                    break;
                                }
                            }
                            rtb.SelectionLength = 0;
                            rtb.SelectionStart = start;
                        }
                        kontextovaNapoveda = false;
                    }

                    //Automatické vkládání uvozovek
                    if (rtb.Text[rtb.SelectionStart - 1] == '=' && rtb.Text[rtb.SelectionStart - 2] != ' ' && !backspacePressed && rtb.Text[rtb.SelectionStart] != '"')
                    {
                        rtb.SelectedText += "\"\"";
                        rtb.SelectionStart--;
                    }

                    //Kontextová nápověda (Intellisense)
                    if (rtb.Text[rtb.SelectionStart - 1] == '<' && texChanCount < 1)
                    {
                        kontextovaNapoveda = true;
                        start_nap = rtb.SelectionStart;
                    }

                    if (kontextovaNapoveda && texChanCount < 1)
                    {
                        listBoxIntellisense.Items.Clear();
                        int start = rtb.SelectionStart;
                        int length = 0;
                        for (int i = rtb.SelectionStart; rtb.Text[i] != '<'
                            && rtb.SelectionStart >= start_nap; i--)
                        {
                            length++;
                        }

                        rtb.SelectionStart = start - length + 1;
                        rtb.SelectionLength = 0;
                        if (length > 0) rtb.SelectionLength = length - 1;
                        hledano = rtb.SelectedText;
                        rtb.SelectionStart = start;
                        sem_vlozit = start;
                        rtb.SelectionLength = 0;

                        napoveda_atribut = false;
                        foreach (HTMLtag item in htmlTags)
                        {
                            if (item.Nazev.StartsWith(hledano) && !hledano.Contains(' '))
                                listBoxIntellisense.Items.Add(item.Nazev);
                            else
                            {
                                string[] split = hledano.Split(' ');
                                if (item.Nazev == split[0])
                                {
                                    hledano = split.Last();
                                    foreach (string atribut in item.VsechnyAtributy) //speciální atributy tagu
                                    {
                                        if (atribut.StartsWith(hledano) && atribut != "nn")
                                        {
                                            napoveda_atribut = true;
                                            listBoxIntellisense.Items.Add(atribut);
                                        }
                                    }
                                    if (item.Global_Event == "global" || item.Global_Event == "both") //obecné atributy nebo obojí
                                    {
                                        foreach (string atribut in global_atributy)
                                        {
                                            if (atribut.StartsWith(hledano))
                                            {
                                                napoveda_atribut = true;
                                                listBoxIntellisense.Items.Add(atribut);
                                            }
                                        }
                                    }
                                    if (item.Global_Event == "event" || item.Global_Event == "both") //událostné atributy nebo obojí
                                    {
                                        foreach (string atribut in event_atributy)
                                        {
                                            if (atribut.StartsWith(hledano))
                                            {
                                                napoveda_atribut = true;
                                                listBoxIntellisense.Items.Add(atribut);
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }
                texChanCount++;
            }
        }
        //Přístup pomocní šipek v RTB keydown
        private void ListBoxIntellisense_Leave(object sender, EventArgs e)
        {
            if (tabControl1.TabCount > 0)
            {
                RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                rtb.SelectionStart = sem_vlozit;
                bool doplnit_rovnitko = true;

                if (listBoxIntellisense.SelectedItem != null)
                {
                    string selected = listBoxIntellisense.SelectedItem.ToString();
                    if (selected.StartsWith("<"))
                    {
                        rtb.SelectedText += selected.Remove(0, 1);
                        rtb.SelectionStart -= 4;
                    }
                    else if (selected.EndsWith("-"))
                    {
                        rtb.SelectedText += selected.Remove(0, hledano.Length).Remove(selected.Length - 1, 1);
                        doplnit_rovnitko = false;
                    }
                    else rtb.SelectedText += selected.Remove(0, hledano.Length);
                }
                if (napoveda_atribut && doplnit_rovnitko)
                {
                    rtb.SelectedText += "=\"\"";
                    rtb.SelectionStart--;
                }
                rtb.Focus();
                listBoxIntellisense.Items.Clear();
            }
        }

        private void HtmlRichTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            validationTimer.Stop();
            try
            {
                RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete) backspacePressed = true;
                else backspacePressed = false;

                if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.Control && !e.Alt && !e.Shift)
                    blockTextChanged = true;
                else blockTextChanged = false;

                if (e.KeyCode == Keys.Enter || ((e.KeyCode == Keys.Down || e.KeyCode == Keys.Up) && !kontextovaNapoveda)) ValidatePartRTB();

                if (backspacePressed && rtb.Text[rtb.SelectionStart - 1] == '<')
                {
                    kontextovaNapoveda = false;
                    listBoxIntellisense.Items.Clear();
                }

                if ((e.KeyCode == Keys.Down || e.KeyCode == Keys.Up) && kontextovaNapoveda)
                {
                    listBoxIntellisense.Focus();
                    sem_vlozit = rtb.SelectionStart;
                }
            }
            catch { }
        }

        int pocet_zmacknuti_sipky = 0;

        private void HtmlRichTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
            MatchPartHtml(rtb);
            LineCounter();

            if (e.KeyCode == Keys.Enter) //formátování při odřádkování - vložení tabulátorů nebo mezer
            {
                try
                {
                    int firstcharindex = rtb.GetFirstCharIndexOfCurrentLine();
                    int currentline = rtb.GetLineFromCharIndex(firstcharindex);
                    string pre_linetext = rtb.Lines[currentline - 1];

                    int tabcount = pre_linetext.Length - pre_linetext.TrimStart((char)9).Length;
                    int spacecount = pre_linetext.Length - pre_linetext.TrimStart(' ').Length;

                    for (int i = 0; i < spacecount; i++) rtb.SelectedText += " ";
                    for (int i = 0; i < tabcount; i++) rtb.SelectedText += (char)9;
                }
                catch { }
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Enter:
                    if (!napoveda_atribut && pocet_zmacknuti_sipky > 0)
                    {
                        kontextovaNapoveda = false;
                        listBoxIntellisense.Items.Clear();
                    }
                    else napoveda_atribut = false;
                    pocet_zmacknuti_sipky++;
                    break;
                default: pocet_zmacknuti_sipky = 0; break;
            }
            texChanCount = 0;
            validationTimer.Start();
        }

        private void Button5_Click(object sender, EventArgs e) //Vložit HTML značku
        {
            InsertTag();
        }

        private void LabelLineCounter_Click(object sender, EventArgs e) //Kliknutí na label s počítadlem řádků
        {
            try
            {
                RichTextBox rtb = htmlRTBs[tabControl1.SelectedIndex];
                rtb.TextChanged -= RichTextBox1_TextChanged;
                GoToLine();
                rtb.TextChanged += RichTextBox1_TextChanged;
            }
            catch { }
        }

        private void HtmlRichTextBox_Click(object sender, EventArgs e) //Spočítání řádků při překliknutí záložky
        {
            kontextovaNapoveda = false;
            listBoxIntellisense.Items.Clear();
            LineCounter();
        }

        private void HtmlRichTextBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (htmlRTBs[tabControl1.SelectedIndex].SelectionLength == 0)
            {
                ValidateRichTextBox();
                validationTimer.Start();
            }
        }

        private void HtmlRichTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            validationTimer.Stop();
        }

        private void Button6_Click(object sender, EventArgs e) //Zobrazit úvodní dialog
        {
            if (tabControl1.TabCount > 0)
            {
                if (MessageBox.Show("Zavřít aktuální projekt?", "", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    treeView1.Nodes.Clear();
                    foreach (TabPage item in tabControl1.TabPages) CloseFileCheck(item.Text);
                    openList.Clear();
                    labelFileFolderCount.Text = "Složky: 0   |   Soubory: 0";
                }
                listBox1.Items.Clear();
                listBox2.Items.Clear();
                LoadStartUpListBoxes();
                panelStart.Visible = true;
            }
        }

        private void Button11_Click(object sender, EventArgs e) //Reset button pro listbox se soubory
        {
            listBox2.Items.Clear();
            StreamWriter sw = new StreamWriter("lastfiles.txt");
            sw.Close();
        }

        private void Button8_Click(object sender, EventArgs e) //Reset button pro listbox s projekty
        {
            listBox1.Items.Clear();
            StreamWriter sw = new StreamWriter("lastprojects.txt");
            sw.Close();
        }

        private void ListBox1_SelectedIndexChanged(object sender, EventArgs e) //Vybrání složky z listboxu
        {
            if (listBox1.SelectedItem != null) StartOpenFolder();
        }

        private void ListBox2_SelectedIndexChanged(object sender, EventArgs e) //Vybrání souboru z listboxu
        {
            if (listBox2.SelectedItem != null) StartOpenFile();
        }

        private void TabControl1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle) CloseFileCheck(tabControl1.SelectedTab.Text);
        }

        private void LabelShowErrors_Click(object sender, EventArgs e)
        {
            if (!labelShowErrors.Text.Contains("žádné"))
            {
                if (errorForm.ShowDialog() == DialogResult.Yes)
                {
                    SelectLine(errorForm.LineNumber);
                    LineCounter();
                }
            }
        }

        private int tickCounter = 0;

        private void ValidationTimer_Tick(object sender, EventArgs e) //Validace po uplynutí časového intervalu
        {
            if (!kontextovaNapoveda)
            {
                tickCounter++;
                LockWindowUpdate(htmlRTBs[tabControl1.SelectedIndex].Handle.ToInt32());
                ValidatePartRTB();
                if (tickCounter >= 5)
                {
                    ValidateRequiredTags();
                    tickCounter = 0;
                }
                LockWindowUpdate(0);
            }
        }

        private void HtmlRichTextBox_VScroll(object sender, EventArgs e)
        {
            validationTimer.Stop();
            validationTimer.Stop();
        }

        private void Main_KeyDown(object sender, KeyEventArgs e) //klávesové zkratky
        {
            try
            {
                if (e.Control && !e.Alt)
                {
                    blockTextChanged = true;
                    if (e.Shift) //Zkratky Ctrl+Shift+...
                    {
                        blockTextChanged = false;
                        switch (e.KeyCode)
                        {
                            case Keys.O:
                                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                                    OpenFolder(folderBrowserDialog1.SelectedPath);       //Ctrl+Shift+O - otevřít složku
                                break;
                            case Keys.S: SaveAs(); break;                               //Ctrl+Shift+S - uložit soubor jako
                            case Keys.V: InsertTag(); break;                                //Ctrl+Shift+V - vložení HTML tagu
                        }
                    }
                    else //Zkratky Ctrl+...
                    {
                        switch (e.KeyCode)
                        {
                            case Keys.N: NewFile(); break;                               //Ctrl+N - vytvořit nový soubor
                            case Keys.O: OpenFile(); break;                            //Ctrl+O - otevřít soubor
                            case Keys.S:                                                    //Ctrl+S - uložit soubor
                                if (!tabControl1.SelectedTab.Text.Contains("Nový soubor")) Save();
                                else SaveAs();
                                break;
                            case Keys.W:                                                    //Ctrl+W - zavřít záložku
                                if (tabControl1.TabCount > 0) CloseFileCheck(tabControl1.SelectedTab.Text);
                                break;
                            case Keys.T:                                                    //Ctrl+T - přepínání záložek zleva doprava
                                if (tabControl1.TabCount > 0)
                                {
                                    if (tabControl1.SelectedIndex == tabControl1.TabCount - 1) tabControl1.SelectTab(0);
                                    else tabControl1.SelectTab(tabControl1.SelectedIndex + 1);
                                }
                                break;
                            case Keys.G: GoToLine(); break;                                 //Ctrl+G - přejít na řádek
                        }
                    }
                }
                else if (e.KeyCode == Keys.F5) ShowPreview();                             //F5 - spustit náhled souboru
                if (!e.Control) blockTextChanged = false;
            }
            catch { }
        }
    }
}