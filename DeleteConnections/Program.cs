using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeleteConnections
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Recherche des connexions dépassées");

            using (var dbContext = new DiagnostiquesEntities())
            {
                DateTime Outdated = DateTime.Now.AddHours(-4);
                IQueryable<connexions> listeConn = dbContext.connexions.Where(m => m.dateheure < Outdated || m.dateheure == null);

                Console.WriteLine("Connexions à supprimer : " + listeConn.Count());

                Console.WriteLine("Début suppression");

                foreach(connexions c in listeConn)
                {
                    dbContext.connexions.Remove(c);
                }

                dbContext.SaveChanges();

                Console.WriteLine("Suppression OK");
            }
        }
    }
}
