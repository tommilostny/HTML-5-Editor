using System;
using System.Windows.Forms;

namespace HTML_5_Editor
{
    public partial class gotoLineForm : Form
    {
        public int LineNumber { get; private set; }

        public gotoLineForm() => InitializeComponent();

        public void Send()
        {
            try
            {
                LineNumber = Convert.ToInt32(textBox1.Text);
                if (LineNumber <= 0) throw new Exception();
                Close();
            }
            catch
            {
                if (MessageBox.Show("Hodnota zadaná v textovém poli není správná.", "Chyba", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                {
                    Close();
                }
            }
        }

        private void Button1_Click(object sender, EventArgs e) => Send();

        private void Button2_Click(object sender, EventArgs e) => Close();

        private void GotoLineForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) Send();
            else if (e.KeyCode == Keys.Escape) Close();
        }
    }
}