using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace WP_DZ_PR_23_2018.Models
{
    public class Admini
    {
        public Dictionary<string, Korisnik> list { get; set; }

        public Admini(string path)
        {
            list = new Dictionary<string, Korisnik>();
            //FileStream fs = new FileStream(path);
            StreamReader sr = new StreamReader(path);

            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                Korisnik k = Korisnik.Parse(line, Uloga.ADMIN);
                list.Add(k.KorisnickoIme, k);
            }
            sr.Close();
            //fs.Close();
        }
    }
}