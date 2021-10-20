using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WP_DZ_PR_23_2018.Models;

namespace WP_DZ_PR_23_2018.Controllers
{
    public class ShoppingController : Controller
    {
        private static Kupovina kupovina;
        private static Korisnik kupac;
        private static List<Kupovina> kupovine;

        // GET: Shopping
        //istorja kupovina
        public ActionResult Index()
        {
            kupac = (Korisnik)Session["kupac"];

            if (kupovina == null)
            {
                if (Session["chartItems"] == null)
                    Session["chartItems"] = new Kupovina();
            }
            kupovina = (Kupovina)Session["chartItems"];

            kupovine = new List<Kupovina>();

            //za svakog korisnika iz registrovanih ucitati sve njegove kupovine u Session["kupovine"]
            if(kupac != null)
            {
                if(kupac.IzvrseneKupovine > 0)
                {
                    for (int i = 1; i <= kupac.IzvrseneKupovine; i++)
                    {
                        //indeks podesen da bude dobar
                        string path = Server.MapPath("~/App_Data/Shoppings/" + kupac.KorisnickoIme + i + ".tsv");
                        StreamReader sr = new StreamReader(path);
                        Kupovina kup = new Kupovina(kupac);
                        string firstLine = sr.ReadLine();
                        string[] parts = firstLine.Split('\t');
                        kup.DatumKupovine = Convert.ToDateTime(parts[0]);
                        kup.UkupnaCena = double.Parse(parts[1]);

                        while((firstLine = sr.ReadLine()) != null)
                        {
                            parts = firstLine.Split('\t');
                            Knjiga knjiga = new Knjiga();
                            knjiga.Naziv = parts[0];
                            knjiga.Autor = parts[1];
                            knjiga.Cena = double.Parse(parts[2]);
                            kup.Knjige.Add(knjiga);
                        }

                        sr.Close();

                        kupovine.Add(kup);

                    }
                }
            }

            Session["kupovine"] = kupovine;

            return View();
        }

        public ActionResult Chart()
        {
            kupac = (Korisnik)Session["kupac"];

            if (kupovina == null)
            {
                if (Session["chartItems"] == null)
                    Session["chartItems"] = new Kupovina();
            }
            kupovina = (Kupovina)Session["chartItems"];

            return View();
        }

        //obavi kupovinu knjiga
        public ActionResult Buy()
        {
            //NAPOMENA:
            //iz korpe se ne moze javiti nikakva greska jer su korisniku ogranicene akcije na taj nacin da ne moze da napravi gresku
            if(kupovina.Knjige.Count == 0)
            {
                TempData["message"] = "Ne mozete izvrsiti kupovinu kada je korpa prazna!";
                return RedirectToAction("Chart", "Shopping");
            }
            kupovina.Kupi();

            //upis u fajl sistem 
            //cuvamo svaku kupovinu u formatu username+redniBrKupovine (npr. pera3.tsv)
            string shoppingPath = Server.MapPath("~/App_Data/Shoppings/" + kupac.KorisnickoIme + kupac.IzvrseneKupovine + ".tsv");
            StreamWriter sw = new StreamWriter(shoppingPath);
            sw.WriteLine(kupovina.DatumKupovine.ToString("dd'/'MM'/'yyyy HH:mm:ss") + "\t" + kupovina.UkupnaCena);
            foreach (Knjiga k in kupovina.Knjige)
            {
                sw.WriteLine(k.Naziv + "\t" + k.Autor + "\t" + k.Cena);
            }

            sw.Close();

            //brisanje knjiga iz knjizara tj smanjivanje kolicine
            List<Knjiga> temp = kupovina.Knjige;
            Dictionary<string, Knjizara> knjizare = (Dictionary<string, Knjizara>)HttpContext.Application["knjizare"];
            foreach (Knjizara knjizara in knjizare.Values)
            {
                foreach (Knjiga knjiga in knjizara.Knjige)
                {
                    if (temp.Contains(knjiga))
                    {
                        knjiga.BrojKopija--;
                        temp.Remove(knjiga);
                        //smanjivanje broja tih knjiga na zalihama u knjizari

                        //pravimo naziv fajla knjizare da bismo u njemu smanjili kolicinu knjige
                        string[] parts = knjizara.Naziv.Split(' ');
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

                        StreamReader srK = new StreamReader(bookstoreFilePath);
                        StreamWriter swK = new StreamWriter(tempFile);

                        string line = "";
                        string newLine = "";

                        while ((line = srK.ReadLine()) != null)
                        {
                            parts = line.Split('\t');
                            if (parts[0] == knjiga.Naziv && parts[1] == knjiga.Autor && double.Parse(parts[5]) == knjiga.Cena)
                            {
                                int kolicina = Int32.Parse(parts[6]);
                                //nema potrebe da se proverava da kolicina ne ode u minus
                                //jer se korisnicima ne prikazuju knjige kojih nema na stanju
                                //pa nemaju ni opciju da ih kupe
                                kolicina--;
                                newLine = parts[0] + "\t" + parts[1] + "\t" + parts[2] + "\t" + parts[3] + "\t" + parts[4]
                                    + "\t" + parts[5] + "\t" + kolicina + "\t" + parts[7];
                                swK.WriteLine(newLine);
                            }
                            else
                            {
                                swK.WriteLine(line);
                            }
                        }

                        srK.Close();
                        swK.Close();

                        System.IO.File.Delete(bookstoreFilePath);
                        System.IO.File.Move(tempFile, bookstoreFilePath);
                    }
                    if (temp.Count == 0)
                        break;
                }
                if (temp.Count == 0)
                    break;
            }
            HttpContext.Application["knjizare"] = knjizare;

            //ponovo ucitaj knjige iz fajl sistema
            ReloadBooks();

            kupovina = new Kupovina(kupac);

            Session["chartItems"] = null;

            AddShopping();

            return RedirectToAction("Index", "Shopping");
        }

        [NonAction]
        public void AddShopping()
        {
            //povecaj broj kupovina kod kupca
            string path = Server.MapPath("~/App_Data/registrovani.tsv");
            string tempFile = Path.GetTempFileName();

            StreamReader sr = new StreamReader(path);
            StreamWriter sw = new StreamWriter(tempFile);
            string line = "";

            while((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split('\t');
                if (parts[0] == kupac.KorisnickoIme)
                {
                    int broj = Int32.Parse(parts[7]);
                    broj++;
                    string newLine = parts[0] + "\t" + parts[1] + "\t" + parts[2] + "\t" + parts[3] + "\t" + parts[4]
                        + "\t" + parts[5] + "\t" + parts[6] + "\t" + broj + "\t" + parts[8];
                    sw.WriteLine(newLine);
                }
                else
                    sw.WriteLine(line);
            }
            sr.Close();
            sw.Close();

            System.IO.File.Delete(path);
            System.IO.File.Move(tempFile, path);
        }

        [NonAction]
        private void ReloadBooks()
        {
            var bookstoresFile = Server.MapPath("~/App_Data/bookstores.tsv");
            StreamReader sr = new StreamReader(bookstoresFile);

            Dictionary<string, Knjizara> knjizare = new Dictionary<string, Knjizara>();

            string line = "";
            while ((line = sr.ReadLine()) != null)
            {
                Knjizara k = Knjizara.Parse(line);
                knjizare.Add(k.Naziv, k);
            }

            sr.Close();
            HttpContext.Application["knjizare"] = knjizare;

            var knjige = new List<Knjiga>();

            foreach (Knjizara knjizara in knjizare.Values)
            {
                foreach (Knjiga knjiga in knjizara.Knjige)
                {
                    knjige.Add(knjiga);
                }
            }
            HttpContext.Application["knjige"] = knjige;

        }

        public ActionResult RemoveItem()
        {
            string naziv = Request["naziv"];
            string autor = Request["autor"];
            double cena = double.Parse(Request["cena"]);

            foreach (Knjiga k in kupovina.Knjige)
            {
                if(k.Naziv == naziv && k.Autor == autor && k.Cena == cena)
                {
                    //uklanjanje iz korpe
                    k.BrojKopija++;
                    kupovina.Knjige.Remove(k);
                    kupovina.UkupnaCena -= k.Cena;
                    break;
                }
            }
            Session["chartItems"] = kupovina;

            return RedirectToAction("Chart", "Shopping");
        }
    }
}