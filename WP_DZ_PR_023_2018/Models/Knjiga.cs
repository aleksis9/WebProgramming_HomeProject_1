using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WP_DZ_PR_23_2018.Models
{
    public enum Zanr { ROMAN, DRAMA, TEEN, KRIMI, KLASIK };
   
    public class Knjiga
    {
        public string Naziv { get; set; }
        public string Autor { get; set; }
        public int BrojStranica { get; set; }
        public Zanr ZanrKnjige { get; set; }
        public string Opis { get; set; }
        public double Cena { get; set; }
        public int BrojKopija { get; set; }
        public bool Obrisana { get; set; }

        public Knjiga() { }

        public Knjiga(string naz, string aut, int bs, Zanr z, string op, double c, int bk, bool obr)
        {
            Naziv = naz;
            Autor = aut;
            BrojStranica = bs;
            ZanrKnjige = z;
            Opis = op;
            Cena = c;
            BrojKopija = bk;
            Obrisana = obr;
        }

        public void Delete()
        {
            Obrisana = true;
        }

        public static Knjiga Parse(string s)
        {
            string[] tokens = s.Split('\t');
            string naziv = tokens[0];
            string autor = tokens[1];
            int brStranica = int.Parse(tokens[2]);
            string zanrStr = tokens[3];
            Zanr zanr;
            switch (zanrStr)
            {
                case "ROMAN":
                    zanr = Zanr.ROMAN;
                    break;
                case "DRAMA":
                    zanr = Zanr.DRAMA;
                    break;
                case "TEEN":
                    zanr = Zanr.TEEN;
                    break;
                case "KRIMI":
                    zanr = Zanr.KRIMI;
                    break;
                case "KLASIK":
                    zanr = Zanr.KLASIK;
                    break;
                default:
                    zanr = Zanr.KLASIK;
                    break;
            }
            string opisPath = tokens[4];
            double cena = double.Parse(tokens[5]);
            int brKopija = int.Parse(tokens[6]);
            int obrisana = int.Parse(tokens[7]);
            bool del;
            if (obrisana == 1)
                del = true;
            else del = false;

            string opisFullPath = AppDomain.CurrentDomain.BaseDirectory + "App_Data\\" + opisPath;

            StreamReader sr = new StreamReader(opisFullPath);
            string line = "";
            string opis = "";
            while ((line = sr.ReadLine()) != null)
            {
                opis += line;
            }

            Knjiga k = new Knjiga(naziv, autor, brStranica, zanr, opis, cena, brKopija, del);

            sr.Close();

            return k;
        }
    }
}