using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace HTML_5_Editor
{
    class HTMLtag
    {
        public string Nazev { get; }
        public bool Parovy { get; }
        public string ParovyToString { get; }
        public string[] Atributy { get; }
        public string Popisek { get; }
        public string[] VsechnyAtributy { get; }
        public string Global_Event { get; }

        public HTMLtag(string nazev, bool parovy, string parovy_slovy, string[] atributy, string popisek,
            string[] vsechny_atributy, string global_event)
        {
            Nazev = nazev;
            Parovy = parovy;
            Atributy = atributy;
            Popisek = popisek;
            ParovyToString = parovy_slovy;
            VsechnyAtributy = vsechny_atributy;
            Global_Event = global_event;
        }

        public HTMLtag() { }

        public List<HTMLtag> CreateDatabase()
        {
            StreamReader sr = new StreamReader("html5data.txt");
            List<HTMLtag> htmlTags = new List<HTMLtag>();
            while (!sr.EndOfStream)
            {
                string[] data = sr.ReadLine().Split(';'); //rozdělění dat z jednoho řádku souboru

                bool parovost;
                if (data[2] == "ano") parovost = true;
                else parovost = false;

                string[] atributy_tagu = data[3].Split(','); //obsahuje seznam atributů pro jednolivé tagy
                string[] vsechny_atributy = atributy_tagu.Union(data[4].Split(',')).ToArray();
                Array.Sort(vsechny_atributy);

                htmlTags.Add(new HTMLtag(data[0], parovost, data[2], atributy_tagu, data[1], vsechny_atributy, data[5]));
            }
            sr.Close();
            return htmlTags;
        }
    }
}