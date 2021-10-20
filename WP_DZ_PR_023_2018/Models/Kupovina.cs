using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WP_DZ_PR_23_2018.Models
{
    public class Kupovina
    {
        public Korisnik Kupac { get; set; }
        public List<Knjiga> Knjige { get; set; }
        public DateTime DatumKupovine { get; set; }
        public double UkupnaCena { get; set; }

        public Kupovina()
        {
            Knjige = new List<Knjiga>();
            UkupnaCena = 0;
        }

        //videti jos kako ce se koristiti ova klasa pa na osnovu toga izmeniti konstruktor
        public Kupovina(Korisnik kup)
        {
            Kupac = kup;
            Knjige = new List<Knjiga>();
            UkupnaCena = 0;
        }

        public void DodajKnjigu(Knjiga k)
        {
            Knjige.Add(k);
            UkupnaCena += k.Cena;
        }

        public void Kupi()
        {
            DatumKupovine = DateTime.Now;
            Kupac.IzvrseneKupovine++;
        }
    }
}