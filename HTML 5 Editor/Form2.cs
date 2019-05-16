using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
//using Microsoft.Toolkit.Win32.UI.Controls.WinForms;
//using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;

namespace HTML_5_Editor
{
    public partial class Form2 : Form
    {
        public Form2(string get_url)
        {
            InitializeComponent();
            //webView1.Navigate(new Uri("ms-appdata:///C:/Users/tommi/Desktop/web/index.html"));
            //webView2.Navigate(new Uri("file:///C:/Users/tommi/Desktop/web/WWW%20projekt/CSS/index.html"));
            //webView2.Navigate(new Uri("https://www.microsoft.com"));

            //Uri localUri = new Uri(@"C:\Users\tommi\Desktop\web\index.html");
            //webView1.NavigateToString(url);

            //Text = get_url;
            //get_url = @"\" + get_url;
            //webView1.NavigateToLocal(get_url);
            //string url = @"C:\Users\tommi\Desktop\web\WWW projekt\CSS\index.html";
            //Uri uri = new Uri(get_url);
            //webView1.Navigate(new Uri("https://www.microsoft.com"));

            //Uri uri = new Uri("C:/Users/tommi/Desktop/web/index.html");
            //webView2.NavigateToLocalStreamUri(uri, new Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.IUriToStreamResolver);

            //webView2.NavigateToString("<html><head><style>body{font-family:'Segoe UI Light';padding:32px}h1{text-align:center}</style></head><body><h1>Náhled stránky?</h1><p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nam consequat tellus mi, sed ultrices quam tempor ut. Duis pulvinar leo nec metus gravida, at sodales ante efficitur. Nunc quis aliquet ipsum. Fusce et molestie magna, facilisis auctor velit. Nulla sed erat ante. Donec tincidunt neque tincidunt lorem efficitur eleifend. Pellentesque egestas tincidunt nulla, ac aliquet orci viverra ac. Praesent tincidunt non orci a accumsan. Vivamus mattis, leo eget scelerisque mattis, lacus odio auctor dui, sed consequat massa felis et augue. Ut ornare neque turpis, eget sollicitudin nulla aliquam eu.</p><body></html>");
        }
    }
}