using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace HTML_5_Editor
{
    public partial class ErrorListForm : Form
    {
        public int LineNumber { get; private set; }

        public ErrorListForm()
        {
            InitializeComponent();
        }

        public void ClearListBox()
        {
            listBox1.Items.Clear();
        }

        public void ErrorLog(string chyba)
        {
            listBox1.Items.Add(chyba);
        }

        private void ListBox1_DoubleClick(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                string line_num = "";
                if (Regex.IsMatch(listBox1.SelectedItem.ToString(), "[0-9]"))
                {
                    foreach (char item in listBox1.SelectedItem.ToString())
                    {
                        if (item >= '0' && item <= '9') line_num += item;
                        else break;
                    }
                }
                else line_num = "1";
                LineNumber = Convert.ToInt32(line_num);
                ActiveForm.DialogResult = DialogResult.Yes;
                Close();
            }
        }

        public void ClearLineLogs(int line_number, out int errorCount)
        {
            errorCount = 0;
            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (listBox1.Items[i].ToString().StartsWith(line_number.ToString() + "."))
                {
                    listBox1.Items.RemoveAt(i);
                    errorCount++;
                    i--;
                }
            }
        }

        public List<string> ReturnList()
        {
            List<string> list = new List<string>();
            foreach (var item in listBox1.Items)
            {
                list.Add(item.ToString());
            }
            return list;
        }

        public void RemoveLog(int index)
        {
            listBox1.Items.RemoveAt(index);
        }

        public void InsertToStart(string errorMessage)
        {
            listBox1.Items.Insert(0, errorMessage);
        }
    }
}