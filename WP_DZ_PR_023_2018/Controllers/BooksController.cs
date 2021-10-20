using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using WP_DZ_PR_23_2018.Models;

namespace WP_DZ_PR_23_2018.Controllers
{
    public class BooksController : Controller
    {
        private static Dictionary<string, Knjizara> knjizare;
        private static List<Knjiga> knjige;

        // GET: Books
        public ActionResult Index()
        {
            if (HttpContext.Application["admini"] == null)
            {
                var adminsFile = Server.MapPath("~/App_Data/administrators.tsv");
                HttpContext.Application["admini"] = new Admini(adminsFile);            
            }

            if (HttpContext.Application["registrovani"] == null)
            {
                var registrovaniFile = Server.MapPath("~/App_Data/registrovani.tsv");
                StreamReader sr = new StreamReader(registrovaniFile);
                List<Korisnik> registrovani = new List<Korisnik>();
                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    Korisnik k = Korisnik.Parse(line, Uloga.KUPAC);
                    registrovani.Add(k);
                }

                sr.Close();

                HttpContext.Application["registrovani"] = registrovani;
            }

            if (HttpContext.Application["knjizare"] == null)
            {
                var bookstoresFile = Server.MapPath("~/App_Data/bookstores.tsv");
                StreamReader sr = new StreamReader(bookstoresFile);

                knjizare = new Dictionary<string, Knjizara>();

                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    Knjizara k = Knjizara.Parse(line);
                    knjizare.Add(k.Naziv, k);
                }

                sr.Close();
                HttpContext.Application["knjizare"] = knjizare;
            }
            else
                knjizare = (Dictionary<string, Knjizara>)HttpContext.Application["knjizare"];

            if (HttpContext.Application["knjige"] == null)
            {
                knjige = new List<Knjiga>();

                foreach (Knjizara knjizara in knjizare.Values)
                {
                    foreach (Knjiga knjiga in knjizara.Knjige)
                    {
                        knjige.Add(knjiga);
                    }
                }
                HttpContext.Application["knjige"] = knjige;
            }
            else
                knjige = (List<Knjiga>)HttpContext.Application["knjige"];
            
            HttpContext.Application["knjige"] = knjige;
            
            return View();
        }

        public ActionResult Details()
        {
            string naziv = Request["detailsN"];
            string autor = Request["detailsA"];
            double cena = double.Parse(Request["detailsC"]);

            foreach(Knjiga k in knjige)
            {
                if (k.Naziv == naziv && k.Autor == autor && k.Cena == cena)
                {
                    ViewBag.Detalji = k;
                    break;
                }
            }

            return View();
        }

        //brisanje knjige
        public ActionResult Delete()
        {
            Korisnik kupac = (Korisnik)Session["kupac"];
            string nazivKnjizare = "";
            if(kupac.UlogaKorisnika == Uloga.ADMIN)
            {
                string naz = Request["deleteN"];
                string aut = Request["deleteA"];
                double cen = double.Parse(Request["deleteC"]);

                foreach(Knjiga k in knjige)
                {
                    if(k.Naziv == naz && k.Autor == aut && k.Cena == cen)
                    {
                        //LOGICKO BRISANJE
                        k.Delete();
                        //moramo da smanjimo broj knjiga u toj knjizari
                        foreach(Knjizara knjizara in knjizare.Values)
                        {
                            if (knjizara.Knjige.Contains(k))
                            {
                                nazivKnjizare = knjizara.Naziv;
                                knjizara.BrojKnjiga--;
                                break;
                            }
                        }
                        break;
                    }
                }
                HttpContext.Application["knjige"] = knjige;

                //brisanje iz fajl sistema
                string[] parts = nazivKnjizare.Split(' ');
                string bookstoreFile = parts[0].ToLower();

                string newLine = "";

                if (parts.Length >= 1)
                {
                    for (int i = 1; i < parts.Length; i++)
                    {
                        bookstoreFile += parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1, parts[i].Length - 1).ToLower();
                    }
                }

                bookstoreFile += ".tsv";

                string bookstoreFilePath = Server.MapPath("~/App_Data/" + bookstoreFile);
                string tempFile = Path.GetTempFileName();

                StreamReader sr = new StreamReader(bookstoreFilePath);
                StreamWriter sw = new StreamWriter(tempFile);

                string line = "";

                while ((line = sr.ReadLine()) != null)
                {
                    string[] delovi = line.Split('\t');
                    string naziv = delovi[0];
                    string autor = delovi[1];

                    if (naziv != naz || autor != aut)
                        sw.WriteLine(line);
                    else
                    {
                        //uklonimo oznaku da li je obrisana
                        newLine = line.Substring(0, line.Length - 1);
                        newLine += "1";
                        sw.WriteLine(newLine);
                    }
                }

                sr.Close();
                sw.Close();

                System.IO.File.Delete(bookstoreFilePath);
                System.IO.File.Move(tempFile, bookstoreFilePath);

            }

            return RedirectToAction("Index", "Books");
        }

        public ActionResult Bookstores()
        {
            return View();
        }

        public ActionResult AddBookstore()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddBookstoreClick()
        {
            string naziv = Request["naziv"];
            string adresa = Request["adresa"];

            if (Request["naziv"] == null || Request["naziv"] == "")
            {
                TempData["message"] = "Unesite naziv knjizare!";
                return RedirectToAction("AddBookstore", "Books");
            }

            if (Request["adresa"] == null || Request["adresa"] == "")
            {
                TempData["message"] = "Unesite adresu knjizare!";
                return RedirectToAction("AddBookstore", "Books");
            }

            foreach (Knjizara knjizara in knjizare.Values)
            {
                if(knjizara.Naziv == naziv && knjizara.Adresa == adresa)
                {
                    TempData["message"] = "Knjizara vec postoji!";
                    return RedirectToAction("AddBookstore");
                }
            }

            knjizare.Add(naziv, new Knjizara(naziv, adresa, false));
            HttpContext.Application["knjizare"] = knjizare;

            string path = Server.MapPath("~/App_Data/bookstores.tsv");
            string tempFile = Path.GetTempFileName();

            StreamReader sr = new StreamReader(path);
            StreamWriter sw = new StreamWriter(tempFile);

            string line = "";

            while ((line = sr.ReadLine()) != null)
            {
                sw.WriteLine(line);
            }

            //u slucaju da naziv ima vise reci
            string[] parts = naziv.Split(' ');
            string bookstoreFile = parts[0].ToLower();

            if (parts.Length >= 1)
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    bookstoreFile += parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1, parts[i].Length - 1).ToLower();
                }
            }

            bookstoreFile += ".tsv";

            sw.WriteLine(naziv + "\t" + adresa + "\t" + bookstoreFile + "\t" + "0");

            sr.Close();
            sw.Close();

            System.IO.File.Delete(path);
            System.IO.File.Move(tempFile, path);

            //pravimo prazan fajl za tu knjizaru da posle mozemo da dodajemo knjige
            string bookstoreFilePath = Server.MapPath("~/App_Data/" + bookstoreFile);
            System.IO.File.WriteAllText(bookstoreFilePath, String.Empty);

            return RedirectToAction("Bookstores", "Books");
        }

        public ActionResult AddBook()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddBookClick()
        {
            //dodavanje jedne knjige u listu knjige
            
            Zanr newZanr;
            switch (Request["zanr"])
            {
                case "ROMAN":
                    newZanr = Zanr.ROMAN;
                    break;
                case "DRAMA":
                    newZanr = Zanr.DRAMA;
                    break;
                case "TEEN":
                    newZanr = Zanr.TEEN;
                    break;
                case "KRIMI":
                    newZanr = Zanr.KRIMI;
                    break;
                default:
                    newZanr = Zanr.KLASIK;
                    break;
            }

            if (!ValidateKnjiga(Request))
            {
                return RedirectToAction("AddBook", "Books");
            }

            Knjiga nova = new Knjiga(Request["naziv"], Request["autor"], Int32.Parse(Request["brojStranica"]), newZanr, 
            Request["opis"], double.Parse(Request["cena"]), Int32.Parse(Request["brojKopija"]), false);

            knjige.Add(nova);

            //dodavanje u app["knjige"]
            HttpContext.Application["knjige"] = knjige;


            //cuvanje u fajlu od te knjizare
            string[] delovi = Request["knjizara"].Split(';');
            string naziv = delovi[0];
            string adresa = delovi[1];

            knjizare[naziv].Knjige.Add(nova);
            knjizare[naziv].BrojKnjiga++;
            HttpContext.Application["knjizare"] = knjizare;

            //pravimo naziv fajla knjizare da bismo u njega dodali novu knjigu
            string[] parts = naziv.Split(' ');
            string bookstoreFile = parts[0].ToLower();

            if (parts.Length >= 1)
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    bookstoreFile += parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1, parts[i].Length - 1).ToLower();
                }
            }

            bookstoreFile += ".tsv";

            string bookstoreFilePath = Server.MapPath("~/App_Data/" + bookstoreFile);
            string tempFile = Path.GetTempFileName();

            StreamReader sr = new StreamReader(bookstoreFilePath);
            StreamWriter sw = new StreamWriter(tempFile);

            string line = "";

            while ((line = sr.ReadLine()) != null)
            {
                sw.WriteLine(line);
            }

            //naziv knjige
            naziv = Request["naziv"];

            parts = naziv.Split(' ');
            string bookFilePath = parts[0].ToLower();

            if (parts.Length >= 1)
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    bookFilePath += parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1, parts[i].Length - 1).ToLower();
                }
            }

            bookFilePath += ".tsv";

            string opisFilePath = Server.MapPath("~/App_Data/" + bookFilePath);

            sw.WriteLine(nova.Naziv + "\t" + nova.Autor + "\t" + nova.BrojStranica + "\t" + nova.ZanrKnjige.ToString() + "\t"
                + bookFilePath + "\t" +  nova.Cena + "\t" + nova.BrojKopija + "\t" + "0");

            sr.Close();
            sw.Close();

            System.IO.File.Delete(bookstoreFilePath);
            System.IO.File.Move(tempFile, bookstoreFilePath);

            //pravljenje novog fajla sa opisom te knjige
            System.IO.File.WriteAllText(opisFilePath, Request["opis"]);

            return RedirectToAction("Index", "Books");
        }

        public ActionResult AddToChart()
        {
            string naziv = Request["AddChartN"];
            string autor = Request["AddChartA"];
            double cena = double.Parse(Request["AddChartC"]);

            Kupovina kupovina = (Kupovina)Session["chartItems"];
            if(kupovina == null)
            {
                kupovina = new Kupovina((Korisnik)Session["kupac"]);
                Session["chartItems"] = kupovina;
            }

            foreach(Knjiga k in knjige)
            {
                if(k.Naziv == naziv && k.Autor == autor && k.Cena == cena)
                {
                    //ova metoda menja i ukupnu cenu
                    kupovina.DodajKnjigu(k);
                    k.BrojKopija--;
                }
            }

            Session["chartItems"] = kupovina;

            return RedirectToAction("Chart", "Shopping");
        }

        [HttpPost]
        public ActionResult SearchClick()
        {
            if (Request["pretraga"] == null || Request["pretraga"] == "")
            {
                TempData["message"] = "Unesite parametar pretrage!";
                return RedirectToAction("Index", "Books");
            }

            Session["sortirani"] = null;

            string search = Request["pretraga"];
            string param = Request["pretraziPo"];
            List<Knjiga> pretrazeni = new List<Knjiga>();
            if(param == "naziv")
            {
                foreach(Knjiga k in (List<Knjiga>)HttpContext.Application["knjige"])
                {
                    if (k.Naziv.Contains(search))
                        pretrazeni.Add(k);
                }
            }
            else if(param == "zanr")
            {
                foreach (Knjiga k in (List<Knjiga>)HttpContext.Application["knjige"])
                {
                    if (k.ZanrKnjige.ToString().ToLower().Contains(search.ToLower()))
                        pretrazeni.Add(k);
                }
            }
            else
            {
                //po ceni

                string[] cene = search.Split('-');
                int min = 0;
                int max = 0;

                if(cene.Count() != 2)
                {
                    TempData["message"] = "Cena nije uneta u dobrom formatu";
                }
                else
                {
                    Int32.TryParse(cene[0], out min);
                    Int32.TryParse(cene[1], out max);
                }
 
                Regex rgx = new Regex("[0-9]");
                bool flag = false;

                if (!rgx.IsMatch(cene[0]))
                    flag = true;
                if (!rgx.IsMatch(cene[1]))
                    flag = true;

                if (flag == false)
                {
                    foreach (Knjiga k in (List<Knjiga>)HttpContext.Application["knjige"])
                    {
                        if (k.Cena > min && k.Cena < max)
                            pretrazeni.Add(k);
                    }
                }

            }

            Session["pretrazeni"] = pretrazeni;

            return RedirectToAction("Index", "Books");
        }

        [HttpPost]
        public ActionResult SortClick()
        {
            Session["pretrazeni"] = null;

            string type = Request["sortirajPo"];
            string way = Request["opadajuceRastuce"];
            List<Knjiga> sortirani = new List<Knjiga>();

            List<Knjiga> knjige = (List<Knjiga>)HttpContext.Application["knjige"];

            int[] map = new[] { 4, 1, 5, 2, 3 };

            List<Knjiga> temp = knjige;

            if (way == "rastuce")
            {
                if (Request["sortirajPo"] == "naziv")
                    knjige = temp.OrderBy(k => k.Naziv).ToList();
                else if (Request["sortirajPo"] == "zanr")
                    knjige = temp.OrderBy(c => c.ZanrKnjige.ToString()).ToList();
                else if (Request["sortirajPo"] == "cena")
                    knjige = temp.OrderBy(k => k.Cena).ToList();
            }
            else if(way == "opadajuce")
            {
                if (Request["sortirajPo"] == "naziv")
                    knjige = temp.OrderByDescending(k => k.Naziv).ToList();
                else if (Request["sortirajPo"] == "zanr")
                    knjige = temp.OrderByDescending(c => c.ZanrKnjige.ToString()).ToList();
                else if (Request["sortirajPo"] == "cena")
                    knjige = temp.OrderByDescending(k => k.Cena).ToList();
            }

            Session["sortirani"] = knjige;

            return RedirectToAction("Index", "Books");
        }

        public ActionResult DeleteBookstore()
        {
            //logika za brisanje knjizare iz fajla bookstores.tsv
            string nazivD = Request["deleteN"];
            string adresaD = Request["deleteA"];

            string path = Server.MapPath("~/App_Data/bookstores.tsv");
            string tempFile = Path.GetTempFileName();

            StreamReader sr = new StreamReader(path);
            StreamWriter sw = new StreamWriter(tempFile);

            string line = "";
            string knjigeD = "";

            while ((line = sr.ReadLine()) != null)
            {
                string[] delovi = line.Split('\t');
                string naziv = delovi[0];
                string adresa = delovi[1];
                string knjige = delovi[2];

                if (naziv != nazivD || adresa != adresaD)
                    sw.WriteLine(line);
                else
                {
                    string newLine = line.Substring(0, line.Length - 1);
                    newLine += "1";
                    sw.WriteLine(newLine);
                    knjigeD = knjige;
                }
            }

            sr.Close();
            sw.Close();

            System.IO.File.Delete(path);
            System.IO.File.Move(tempFile, path);

            //brisanje knjiga iz te knjizare
            string fullBookstorePath = Server.MapPath("~/App_Data/" + knjigeD);
            tempFile = Path.GetTempFileName();

            StreamReader srK = new StreamReader(fullBookstorePath);
            StreamWriter swK = new StreamWriter(tempFile);

            line = "";

            while ((line = srK.ReadLine()) != null)
            {
                string[] delovi = line.Split('\t');
                //uklonimo oznaku da li je obrisana
                string newLine = line.Substring(0, line.Length - 1);
                newLine += "1";
                swK.WriteLine(newLine);
                
            }

            srK.Close();
            swK.Close();

            System.IO.File.Delete(fullBookstorePath);
            System.IO.File.Move(tempFile, fullBookstorePath);

            StreamReader sr2 = new StreamReader(path);

            knjizare = new Dictionary<string, Knjizara>();

            while ((line = sr2.ReadLine()) != null)
            {
                Knjizara k = Knjizara.Parse(line);
                knjizare.Add(k.Naziv, k);
            }

            sr2.Close();
            HttpContext.Application["knjizare"] = knjizare;

            //ponovno ucitavanje spiska knjiga
            knjige = new List<Knjiga>();
            foreach (Knjizara knjizara in knjizare.Values)
            {
                foreach (Knjiga knjiga in knjizara.Knjige)
                {
                    bool flag = false;
                    foreach (Knjiga knjiga2 in knjige)
                    {
                        //provera da li smo vec ubacili istu knjigu
                        if (knjiga2.Naziv == knjiga.Naziv && knjiga2.Autor == knjiga.Autor && knjiga2.Cena == knjiga.Cena)
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag == false)
                    {
                        knjige.Add(knjiga);
                    }
                }
            }
            HttpContext.Application["knjige"] = knjige;


            return RedirectToAction("Bookstores", "Books");
        }

        [NonAction]
        private bool ValidateKnjiga(HttpRequestBase request)
        {
            if (Request["naziv"] == null || Request["naziv"] == "")
            {
                TempData["message"] = "Unesite naziv knjige!";
                return false;
            }

            if (Request["autor"] == null || Request["autor"] == "")
            {
                TempData["message"] = "Unesite autora knjige!";
                return false;
            }

            if (Request["brojStranica"] == null || Request["brojStranica"] == "")
            {
                TempData["message"] = "Unesite broj stranica knjige!";
                return false;
            }

            if (Request["opis"] == null || Request["opis"] == "")
            {
                TempData["message"] = "Unesite opis knjige!";
                return false;
            }

            if (Request["cena"] == null || Request["cena"] == "")
            {
                TempData["message"] = "Unesite cenu knjige!";
                return false;
            }

            if (Request["brojKopija"] == null || Request["brojKopija"] == "")
            {
                TempData["message"] = "Unesite brojKopija knjige!";
                return false;
            }

            if (request["naziv"].Length < 1)
            {
                TempData["message"] = "Naziv knjige mora biti barem 1 karakter!";
                return false;
            }

            if(request["autor"].Split(' ').Count() < 2)
            {
                //mora da ima ime i prezime
                TempData["message"] = "Morate uneti ime i prezime autora!";
                return false;
            }

            if (double.Parse(request["brojStranica"]) < 2 || double.Parse(request["brojStranica"]) > 10000)
            {
                //mora da ima ime i prezime
                TempData["message"] = "Broj stranica mora biti u intervalu [0-10 000]!";
                return false;
            }

            if (double.Parse(request["cena"]) < 0 || double.Parse(request["cena"]) > 100000)
            {
                //mora da ima ime i prezime
                TempData["message"] = "Cena mora biti u intervalu [0-100 000]!";
                return false;
            }

            if (double.Parse(request["brojKopija"]) < 0 || double.Parse(request["brojKopija"]) > 1000)
            {
                //mora da ima ime i prezime
                TempData["message"] = "Broj kopija na stanju mora biti u intervalu [0-1000]!";
                return false;
            }

            return true;
        }

        public ActionResult EditBook()
        {
            string naziv = Request["naziv"];
            string autor = Request["autor"];
            double cena = double.Parse(Request["cena"]);

            foreach (Knjiga k in knjige)
                if (k.Naziv == naziv && k.Autor == autor && k.Cena == cena)
                    ViewBag.Knjiga = k;

            return View();
        }

        public ActionResult EditBookClick()
        {
            Zanr newZanr;
            switch (Request["zanr"])
            {
                case "ROMAN":
                    newZanr = Zanr.ROMAN;
                    break;
                case "DRAMA":
                    newZanr = Zanr.DRAMA;
                    break;
                case "TEEN":
                    newZanr = Zanr.TEEN;
                    break;
                case "KRIMI":
                    newZanr = Zanr.KRIMI;
                    break;
                default:
                    newZanr = Zanr.KLASIK;
                    break;
            }

            if (!ValidateKnjiga(Request))
            {
                return RedirectToAction("EditBook", "Books");
            }

            Knjiga nova = new Knjiga(Request["naziv"], Request["autor"], Int32.Parse(Request["brojStranica"]), newZanr,
            Request["opis"], double.Parse(Request["cena"]), Int32.Parse(Request["brojKopija"]), false);

            string oldNaziv = Request["oldN"];
            string oldAutor = Request["oldA"];
            double oldCena = double.Parse(Request["oldC"]);

            List<Knjiga> noveKnjige = new List<Knjiga>();
            Knjiga oldKnjiga = new Knjiga();

            foreach(Knjiga k in knjige)
            {
                if(k.Naziv != oldNaziv || k.Autor != oldAutor || k.Cena != oldCena)
                {
                    noveKnjige.Add(k);
                }
                else
                {
                    oldKnjiga = k;
                    noveKnjige.Add(nova);
                }
            }

            HttpContext.Application["knjige"] = noveKnjige;
            knjige = noveKnjige;

            bool flag = false;
            string oldBookstoreFile = "";
            string[] parts;

            //uklanjamo tu staru knjigu iz spiska knjiga odgovarajuce knjizare
            foreach (Knjizara knjizara in knjizare.Values)
            {
                foreach(Knjiga knjiga in knjizara.Knjige)
                {
                    if(knjiga.Naziv == oldNaziv && knjiga.Autor == oldAutor && knjiga.Cena == oldCena)
                    {
                        flag = true;
                        knjizara.Knjige.Remove(knjiga);
                        break;
                    }
                }
                if(flag == true)
                {
                    knjizara.Knjige.Add(nova);

                    //pravimo naziv fajla knjizare da bismo u njega dodali novu knjigu
                    parts = knjizara.Naziv.Split(' ');
                    oldBookstoreFile = parts[0].ToLower();

                    if (parts.Length >= 1)
                    {
                        for (int i = 1; i < parts.Length; i++)
                        {
                            oldBookstoreFile += parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1, parts[i].Length - 1).ToLower();
                        }
                    }

                    oldBookstoreFile += ".tsv";
                    break;
                }
            }

            HttpContext.Application["knjizare"] = knjizare;

            //izmena u fajl sistemu
            string bookstoreFilePath = Server.MapPath("~/App_Data/" + oldBookstoreFile);
            string tempFile = Path.GetTempFileName();

            StreamReader sr = new StreamReader(bookstoreFilePath);
            StreamWriter sw = new StreamWriter(tempFile);

            //naziv novog fajla knjige
            parts = nova.Naziv.Split(' ');
            string newbookFilePath = parts[0].ToLower();

            if (parts.Length >= 1)
            {
                for (int i = 1; i < parts.Length; i++)
                {
                    newbookFilePath += parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1, parts[i].Length - 1).ToLower();
                }
            }

            newbookFilePath += ".tsv";

            string line = "";
            string newLine = nova.Naziv + "\t" + nova.Autor + "\t" + nova.BrojStranica + "\t" + nova.ZanrKnjige.ToString() + "\t" + newbookFilePath + "\t" + nova.Cena + "\t" + nova.BrojKopija + "\t" + "0";

            while ((line = sr.ReadLine()) != null)
            {
                parts = line.Split('\t');
                if(oldKnjiga.Naziv == parts[0] && oldKnjiga.Autor == parts[1])
                {
                    //umesto ovog reda prepisati nove podatke
                    sw.WriteLine(newLine);
                }
                else
                    sw.WriteLine(line);
            }
           
            string opisFilePath = Server.MapPath("~/App_Data/" + newbookFilePath);

            sr.Close();
            sw.Close();

            System.IO.File.Delete(bookstoreFilePath);
            System.IO.File.Move(tempFile, bookstoreFilePath);

            //pravljenje novog fajla sa opisom te knjige
            System.IO.File.WriteAllText(opisFilePath, Request["opis"]);

            return RedirectToAction("Index", "Books");
        }

        public ActionResult DetailsBookstore()
        {
            string naziv = Request["detailsN"];
            string adresa = Request["detailsA"];
            Knjizara knjizara = null;

            foreach(Knjizara k in ((Dictionary<string, Knjizara>)HttpContext.Application["knjizare"]).Values)
            {
                if (k.Naziv == naziv && k.Adresa == adresa)
                    knjizara = k;
            }

            ViewBag.Detalji = knjizara;

            int brojKnjiga = 0;
            int brojKopija = 0;

            foreach (Knjiga k in knjizara.Knjige)
            {
                if (k.Obrisana == false)
                {
                    brojKnjiga++;
                    brojKopija += k.BrojKopija;
                }
            }
            ViewBag.BrojKnjiga = brojKnjiga;
            ViewBag.BrojKopija = brojKopija;

            return View();
        }
    }
}