using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WP_DZ_PR_23_2018.Models
{
    public class Knjizara
    {
        public string Naziv { get; set; }
        public string Adresa { get; set; }
        public List<Knjiga> Knjige { get; set; }
        public bool Obrisana { get; set; }
        public int BrojKnjiga { get; set; }

        public Knjizara()
        {
            Knjige = new List<Knjiga>();
            BrojKnjiga = 0;
        }

        public Knjizara(string naz, string adr, bool obrisana)
        {
            Naziv = naz;
            Adresa = adr;
            Knjige = new List<Knjiga>();
            Obrisana = obrisana;
            BrojKnjiga = 0;
        }

        public void Delete()
        {
            Obrisana = true;
            BrojKnjiga = 0;
        }

        public static Knjizara Parse(string s)
        {
            string[] tokens = s.Split('\t');
            string naziv = tokens[0];
            string adresa = tokens[1];
            string knjigeFajl = tokens[2];
            int obrisana = Int32.Parse(tokens[3]);
            bool del;
            if (obrisana == 1)
                del = true;
            else
                del = false;

            string fullPath = AppDomain.CurrentDomain.BaseDirectory + "App_Data\\" + knjigeFajl;

            Knjizara k = new Knjizara(naziv, adresa, del);

            StreamReader sr = new StreamReader(fullPath);

            List<Knjiga> knjige = new List<Knjiga>();
            string line = "";

            while ((line = sr.ReadLine()) != null)
            {
                Knjiga knjiga = Knjiga.Parse(line);
                knjige.Add(knjiga);
                k.BrojKnjiga++;
            }

            k.Knjige = knjige;

            sr.Close();

            return k;
        }
    }
}