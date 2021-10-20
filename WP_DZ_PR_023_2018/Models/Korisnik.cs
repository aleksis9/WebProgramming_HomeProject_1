using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WP_DZ_PR_23_2018.Models
{
    public enum Pol { M, Z };
    public enum Uloga { ADMIN, KUPAC }

    public class Korisnik
    {
        public string KorisnickoIme { get; set; }
        public string Lozinka { get; set; }
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public Pol PolKorisnika { get; set; }
        public string Email { get; set; }
        public DateTime DatumRodjenja { get; set; }
        public Uloga UlogaKorisnika { get; set; }
        public bool LoggedIn { get; set; }
        public bool Deleted { get; set; }
        public int IzvrseneKupovine { get; set; }

        public Korisnik() { }

        public Korisnik(string ki, string loz, string im, string prz, Pol pol, string em, DateTime dr, Uloga ul, int kupovine, bool del) {
            KorisnickoIme = ki;
            Lozinka = loz;
            Ime = im;
            Prezime = prz;
            PolKorisnika = pol;
            Email = em;
            DatumRodjenja = dr;
            UlogaKorisnika = ul;
            LoggedIn = false;
            IzvrseneKupovine = kupovine;
            Deleted = del;
        }

        //kada ucitavamo podatke o adminu iz .txt fajla
        public static Korisnik Parse(string s, Uloga ul)
        {
            string[] tokens = s.Split('\t');
            string username = tokens[0];
            string lozinka = tokens[1];
            string ime = tokens[2];
            string prezime = tokens[3];
            Pol pol;
            if (tokens[4] == "M")
                pol = Pol.M;
            else
                pol = Pol.Z;
            string email = tokens[5];
            DateTime datum = DateTime.Parse(tokens[6]);
            int kupovine = Int32.Parse(tokens[7]);
            int obrisan = Int32.Parse(tokens[8]);
            bool del;
            if (obrisan == 1)
                del = true;
            else
                del = false;

            Uloga uloga = ul;

            Korisnik p = new Korisnik(username, lozinka, ime, prezime, pol, email, datum, uloga, kupovine, del);
            return p;
        }

        public void LogIn()
        {
            LoggedIn = true;
        }

        public void LogOut()
        {
            LoggedIn = false;
        }

        public void Delete()
        {
            Deleted = true;
        }
    }
}