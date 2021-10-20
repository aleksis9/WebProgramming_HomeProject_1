using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using WP_DZ_PR_23_2018.Models;

namespace WP_DZ_PR_23_2018.Controllers
{
    public class AccountsController : Controller
    {
        private static List<Korisnik> registrovani;
        private static Admini admini;

        // GET: Accounts
        //otvara stranicu za registraciju
        public ActionResult Index()
        {
            if (HttpContext.Application["admini"] == null)
            {
                var adminsFile = Server.MapPath("~/App_Data/administrators.tsv");
                HttpContext.Application["admini"] = new Admini(adminsFile);
                admini = (Admini)HttpContext.Application["admini"];
            }

            if (registrovani == null)
                registrovani = new List<Korisnik>();

            if (HttpContext.Application["registrovani"] == null)
            {
                var registrovaniFile = Server.MapPath("~/App_Data/registrovani.tsv");
                StreamReader sr = new StreamReader(registrovaniFile);

                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    Korisnik k = Korisnik.Parse(line, Uloga.KUPAC);
                    registrovani.Add(k);
                }

                sr.Close();

                HttpContext.Application["registrovani"] = registrovani;
            }
            else
            {
                registrovani = (List<Korisnik>)HttpContext.Application["registrovani"];
            }

            return View();
        }

        //otvara stranicu za logovanje
        public ActionResult LogIn()
        {
            if (HttpContext.Application["admini"] == null)
            {
                var adminsFile = Server.MapPath("~/App_Data/administrators.tsv");
                HttpContext.Application["admini"] = new Admini(adminsFile);
                admini = (Admini)HttpContext.Application["admini"];
            }
            else
            {
                admini = (Admini)HttpContext.Application["admini"];
            }

            if (registrovani == null)
                registrovani = new List<Korisnik>();

            if (HttpContext.Application["registrovani"] == null)
            {
                var registrovaniFile = Server.MapPath("~/App_Data/registrovani.tsv");
                StreamReader sr = new StreamReader(registrovaniFile);

                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    Korisnik k = Korisnik.Parse(line, Uloga.KUPAC);
                    registrovani.Add(k);
                }

                sr.Close();

                HttpContext.Application["registrovani"] = registrovani;
            }
            else
            {
                registrovani = (List<Korisnik>)HttpContext.Application["registrovani"];
            }

            return View();
        }

        //poziva se prilikom registracije
        [HttpPost]
        public ActionResult RegisterClick()
        {
            if (registrovani == null)
                registrovani = new List<Korisnik>();

            if (HttpContext.Application["registrovani"] == null)
            {
                HttpContext.Application["registrovani"] = registrovani;
            }
            else
            {
                registrovani = (List<Korisnik>)HttpContext.Application["registrovani"];
            }

            Korisnik k = new Korisnik();

            if (ValidateRegistration(Request))
            {
                k = CreateKorisnik(Request);
                registrovani.Add(k);
                HttpContext.Application["registrovani"] = registrovani;
                TempData["message"] = "Uspesno ste dodali korisnika " + k.Ime + " " + k.Prezime;
            }
            else
            {
                return RedirectToAction("Index", "Accounts");
            }

            //upis u fajl sistem
            string registrovaniFullPath = Server.MapPath("~/App_Data/registrovani.tsv");
            string tempFile = Path.GetTempFileName();

            StreamReader sr = new StreamReader(registrovaniFullPath);
            StreamWriter sw = new StreamWriter(tempFile);

            string line;
            //korisnicko ime, lozinka, ime, prezime, pol, email, datum, broj kupovina, obrisan
            string newLine = k.KorisnickoIme + "\t" + k.Lozinka + "\t" + k.Ime + "\t" + k.Prezime + "\t" 
                + k.PolKorisnika.ToString() + "\t" + k.Email + "\t" + k.DatumRodjenja.ToString("dd'/'MM'/'yyy") + "\t" + "0" + "\t" + "0";

            while ((line = sr.ReadLine()) != null)
            {
                sw.WriteLine(line);
            }
            sw.WriteLine(newLine);

            sr.Close();
            sw.Close();

            System.IO.File.Delete(registrovaniFullPath);
            System.IO.File.Move(tempFile, registrovaniFullPath);

            return RedirectToAction("Login", "Accounts");
        }

        public ActionResult LogoutClick()
        {
            Korisnik kupac;
            if (Session["kupac"] != null)
            {
                kupac = (Korisnik)Session["kupac"];
                kupac.LogOut();
                Session["kupac"] = null;
                Session["chartItems"] = null;
                Session["kupovine"] = null;
            }

            return RedirectToAction("Index", "Books");
        }

        //poziva se prilikom logovanja
        [HttpPost]
        public ActionResult LoginClick()
        {
            if(Request["korisnickoIme"] == null || Request["korisnickoIme"] == "")
            {
                TempData["message"] = "Unesite korisnicko ime!";
                return RedirectToAction("Login", "Accounts");
            }

            if (Request["lozinka"] == null || Request["lozinka"] == "")
            {
                TempData["message"] = "Unesite lozinku!";
                return RedirectToAction("Login", "Accounts");
            }

            bool flag = false;

            foreach (Korisnik k in registrovani)
            {
                if (k.KorisnickoIme == Request["korisnickoIme"] && k.Lozinka == Request["lozinka"] && k.Deleted == false)
                {
                    k.LogIn();
                    Session["kupac"] = k;
                    //sesija istice posle 1h
                    flag = true;
                    TempData["message"] = "Uspesno ste se ulogovali kao " + k.Ime + " " + k.Prezime;
                    Session["chartItems"] = new Kupovina(k);
                    Session["kupovine"] = new List<Kupovina>();
                    break;
                }
            }
            if (flag == false)
            {
                foreach (Korisnik k in admini.list.Values)
                {
                    if (k.KorisnickoIme == Request["korisnickoIme"] && k.Lozinka == Request["lozinka"] && k.Deleted == false)
                    {
                        k.LogIn();
                        Session["kupac"] = k;
                        //sesija istice posle 1h
                        flag = true;
                        TempData["message"] = "Uspesno ste se ulogovali kao " + k.Ime + " " + k.Prezime;
                        break;
                    }
                }
            }

            if (flag == false)
            {
                TempData["message"] = "Pogresno uneti podaci!";
                return RedirectToAction("Login", "Accounts");
            }

            return RedirectToAction("Index", "Books");
        }

        [NonAction]
        private bool ValidateRegistration(HttpRequestBase request)
        {
            string ime = request["ime"];
            string prezime = request["prezime"];
            //jedinstveno i min 3 karaktera
            string korisnickoIme = request["korisnickoIme"];
            //min 8 karaktera, samo slova i brojevi
            string lozinka = request["lozinka"];
            string email = request["email"];
            string datum = request["datumRodjenja"];

            if(ime == null || ime == "")
            {
                TempData["message"] = "Unesite ime!";
                return false;
            }

            if (prezime == null || prezime == "")
            {
                TempData["message"] = "Unesite prezime!";
                return false;
            }

            if (korisnickoIme == null || korisnickoIme == "")
            {
                TempData["message"] = "Unesite korisnicko ime!";
                return false;
            }

            if (lozinka == null || lozinka == "")
            {
                TempData["message"] = "Unesite lozinku!";
                return false;
            }

            if (email == null || email == "")
            {
                TempData["message"] = "Unesite email!";
                return false;
            }

            if (datum == null || datum == "")
            {
                TempData["message"] = "Unesite datum!";
                return false;
            }

            if (korisnickoIme.Length < 3)
            {
                TempData["message"] = "Korisnicko ime mora imati bar 3 karaktera!";
                return false;
            }

            //ovu validaciju sam dodala sama, nije zadata u postavci zadatka
            if(korisnickoIme.Split(' ').Count() != 1)
            {
                TempData["message"] = "Korisnicko ime ne sme da sadrzi razmak!";
                return false;
            }

            foreach(Korisnik k in registrovani)
                if (k.KorisnickoIme == korisnickoIme)
                {
                    TempData["message"] = "Korisnicko ime vec postoji!";
                    return false;
                }

            if (lozinka.Length < 8)
            {
                TempData["message"] = "Lozinka mora imati bar 8 karaktera!";
                return false;
            }

            Regex rgx = new Regex("[A-Za-z0-9]");
            if (!rgx.IsMatch(lozinka))
            {
                TempData["message"] = "Loznika moze da sadrzi samo slova i brojeve!";
                return false;
            }

            int dan = int.Parse(datum.Substring(0, 2));
            int mesec = int.Parse(datum.Substring(3, 2));
            int godina = int.Parse(datum.Substring(6, 4));

            if (godina < 1900 || godina > DateTime.Today.Year)
            {
                TempData["message"] = "Pogresan datum!";
                return false;
            }

            if (mesec < 1 || mesec > 12)
            {
                TempData["message"] = "Pogresan datum!";
                return false;
            }

            if (mesec == 1 || mesec == 3 || mesec == 5 || mesec == 7 || mesec == 8 || mesec == 10 || mesec == 12)
            {
                if (dan < 1 || dan > 31)
                {
                    TempData["message"] = "Pogresan datum!";
                    return false;
                }
            }
            else if (mesec == 4 || mesec == 6 || mesec == 9 || mesec == 11)
            {
                if (dan < 1 || dan > 30)
                {
                    TempData["message"] = "Pogresan datum!";
                    return false;
                }
            }
            else if (mesec == 2)
            {
                if (godina % 4 == 0)
                {
                    if (dan < 1 || dan > 29)
                    {
                        TempData["message"] = "Pogresan datum!";
                        return false;
                    }
                }
                else if (dan < 1 || dan > 28)
                {
                    TempData["message"] = "Pogresan datum!";
                    return false;
                }
            }

            return true;
        }

        [NonAction]
        private Korisnik CreateKorisnik(HttpRequestBase request)
        {
            string ime = request["ime"];
            string prezime = request["prezime"];
            string korisnickoIme = request["korisnickoIme"];
            string lozinka = request["lozinka"];
            string email = request["email"];

            Pol p;
            if (Request["pol"] == "M")
                p = Pol.M;
            else
                p = Pol.Z;

            //preskacemo "/"
            int dan = Int32.Parse(Request["datumRodjenja"].Substring(0, 2));
            int mesec = Int32.Parse(Request["datumRodjenja"].Substring(3, 2));
            int godina = Int32.Parse(Request["datumRodjenja"].Substring(6, 4)); ;
            DateTime datum = new DateTime(godina, mesec, dan);

            //za ispis datuma koristiti:
            //string formattedDate = dd.ToString("dd/MM/yyyy") ; 

            Korisnik k = new Korisnik(korisnickoIme, lozinka, ime, prezime, p, email, datum, Uloga.KUPAC, 0, false);

            return k;
        }

        public ActionResult ListAccounts()
        {
            return View();
        }

        public ActionResult Delete()
        {
            string user = Request["username"];
            bool flag = false;

            foreach(Korisnik k in registrovani)
            {
                if(k.KorisnickoIme == user)
                {
                    if (k.UlogaKorisnika == Uloga.KUPAC)
                    {
                        flag = true;
                        k.Delete();

                        //logicko brisanje iz fajl sistema
                        string registeredFilePath = Server.MapPath("~/App_Data/registrovani.tsv");
                        string tempFile = Path.GetTempFileName();

                        string newLine = k.KorisnickoIme + "\t" + k.Lozinka + "\t" + k.Ime + "\t" + k.Prezime + "\t"
                            + k.PolKorisnika.ToString() + "\t" + k.Email + "\t" + k.DatumRodjenja.ToString("dd'/'MM'/'yyy") + "\t" 
                            + k.IzvrseneKupovine + "\t" + "1";

                        StreamReader sr = new StreamReader(registeredFilePath);
                        StreamWriter sw = new StreamWriter(tempFile);

                        string line = "";
                        while((line = sr.ReadLine()) != null)
                        {
                            string[] parts = line.Split('\t');
                            if(parts[0] != user)
                                sw.WriteLine(line);
                            else
                                sw.WriteLine(newLine);
                        }

                        sr.Close();
                        sw.Close();

                        System.IO.File.Delete(registeredFilePath);
                        System.IO.File.Move(tempFile, registeredFilePath);

                        break;
                    }
                }
            }

            if(flag == false)
            {
                TempData["message"] = "Ne mozete da obrisete admina";
            }

            return RedirectToAction("ListAccounts", "Accounts");
        }

        public ActionResult Details()
        { 
            string username = Request["username"];

            foreach (Korisnik k in (List<Korisnik>)HttpContext.Application["registrovani"])
            {
                if (k.KorisnickoIme == username)
                {
                    ViewBag.Detalji = k;
                    break;
                }
            }

            foreach (Korisnik k in ((Admini)HttpContext.Application["admini"]).list.Values)
            {
                if (k.KorisnickoIme == username)
                {
                    ViewBag.Detalji = k;
                    break;
                }
            }

            return View();
        }
    }
}