using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HTML_5_Editor
{
    public partial class insertTagForm : Form
    {
        public int SelectedTagIndex { get; private set; }

        private List<HTMLtag> htmlTags = new HTMLtag().CreateDatabase();

        public insertTagForm()
        {
            InitializeComponent();

            int i = 0;
            foreach (HTMLtag item in htmlTags)
            {
                listView1.Items.Add(item.Nazev).SubItems.Add(item.Popisek); //vložení značky a popisku do listview
                listView1.Items[i].SubItems.Add(item.ParovyToString); //vložení párovosti tagu do listview
                i++;
            }

            listView1.Focus();
            listView1.Items[0].Focused = true;
        }

        private void SendTag()
        {
            try
            {
                SelectedTagIndex = listView1.SelectedItems[0].Index;

                ActiveForm.DialogResult = DialogResult.OK;
                Close();
            }
            catch (ArgumentOutOfRangeException)
            {
                if (MessageBox.Show("Žádná značka nebyla vybrána.\nPřejete si vybrat nyní?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.No)
                {
                    ActiveForm.DialogResult = DialogResult.Cancel;
                    Close();
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e) => SendTag();

        private void InsertTagForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) SendTag();
            else if (e.KeyCode == Keys.Escape)
            {
                ActiveForm.DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        public bool VlozitAtributy() => checkBox1.Checked;

        private void InsertTagForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (ActiveForm.DialogResult != DialogResult.OK)
            {
                ActiveForm.DialogResult = DialogResult.Cancel;
            }
        }
    }
}