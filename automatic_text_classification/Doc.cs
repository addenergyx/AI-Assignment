using System;
using System.IO;
using System.Text.RegularExpressions;


namespace automatic_text_classification
{
    public static class Doc
    {

        public enum Government { Labour, Conservative, Coalition };

        public static int FileCount(string pathToDir)
        {
            return Directory.GetFiles(pathToDir, "*.*", SearchOption.TopDirectoryOnly).Length;
        }

        public static string FileExists(string file, string message)
        {
            while (!File.Exists(file))
            {
                Menu.Title();
                Console.WriteLine("File does not exist!!! Please enter full path to " + message);
                file = Console.ReadLine().Trim();
            }
            return file;
        }

        public static string DocGovernment(string fileName)
        {
            //get government from filename
            string government = "";

            string[] parties = Enum.GetNames(typeof(Government));

            int q = 0;

            while (!fileName.ToLower().Contains(parties[q].ToLower())) //alot neater than if statement below
            {
                q++;
            }

            government = parties[q];

            /*
            foreach (string party in parties)
            {
                Console.WriteLine
                if (fileName.ToLower().Contains(party.ToLower()))
                {
                    government = party; //As of C#6 the best way to get the name of an enum is the new nameof operator instead of .toString
                }
                else
                {
                    Console.WriteLine("Couldn't determine government\nPlease ensure all filenames of " +
                                      "training data contain a political party");
                    Console.ReadLine();
                }
            }


            if (fileName.ToLower().Contains("conservative"))
            {
                government = nameof(Government.Conservative); //As of C#6 the best way to get the name of an enum is the new nameof operator instead of .toString

            }
            else if (fileName.ToLower().Contains("labour"))
            {
                government = nameof(Government.Labour);
            }
            else if (fileName.ToLower().Contains(nameof(Government.Coalition).ToLower()))
            {
                government = nameof(Government.Coalition);
            }
            else
            {
                Console.WriteLine("Couldn't determine government\nPlease ensure filename of " +
                                  "training data contains government");
                Console.ReadLine();
            }*/

            //Console.WriteLine("Government: {0}", government);
            //Regex docGovernment = new Regex("[^a-zA-Z0-9]");
            //Console.WriteLine(government);
            return government;
        }

        public static string Stemming(string word)
        {
            /*Following Porter's stemming algorithm
             * rules in order
             * 1a)
             * sses -> ss
             * ies -> y - stemming can cause some words to be incorrect for example duties would become duti 
             * instead of duty however this should have little affect on the classification result
             * ss -> ss
             * s -> '' 
             * 
             * 1b) 
             * (*v*)ing -> ''
             * (*v*)ed -> ''
             * 
             * 2)
             * ational -> ate
             * iser -> ise
             * izer -> ize
             * ator ->ate
             * 
             * 3) 
             * al -> ''
             * (*)able -> ''
             * (*)ate -> ''
             * 
            */

            // In porter stemming y is considered a vowel

            //step 1a
            if (word.EndsWith("s", StringComparison.CurrentCultureIgnoreCase))
            {
                if (word.EndsWith("sses", StringComparison.CurrentCultureIgnoreCase)) { word = Regex.Replace(word, "sses$", "es"); }
                //if (word.EndsWith("ies", StringComparison.CurrentCultureIgnoreCase)) { Regex.Replace(word, "ies$", "y"); }

                if (word.EndsWith("ies", StringComparison.CurrentCultureIgnoreCase) || word.EndsWith("ied", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Regex.IsMatch(word, @"^[a-z]ie[sd]")) { word = Regex.Replace(word, ".$", ""); }
                    else { word = Regex.Replace(word, @"ie[sd]$", "i"); }
                }

                if (Regex.IsMatch(word, @"[^aeiouy]s$")) //delete s if preceding word part contains a vowel not immediately before the s
                {
                    word = Regex.Replace(word, "s$", "");
                }

            }

            //step 1b
            if (Regex.IsMatch(word, @"ee.*[dly]$")) { word = Regex.Replace(word, @"[l]?[dy]$", ""); }

            if (Regex.IsMatch(word, @"(ed|edly|ingly|ing)$"))
            {
                if (Regex.IsMatch(word, @".*[aeiouy].*(ed|edly|ingly|ing)$"))
                {
                    Regex.Replace(word, @"(ed|edly|ingly|ing)$", "");
                    if (Regex.IsMatch(word, @"(at|bl|iz)$")) { word += "e"; }//append 'e' to word
                    else if (Regex.IsMatch(word, @"(.)\1$")) { word = Regex.Replace(word, @".$", ""); }
                    else if (word.Length < 4) { word += "e"; }
                }
            }

            //if (word.EndsWith("ed") || word.EndsWith("edly") || word.EndsWith("ing") || word.EndsWith("ingly"))

            //step 1c
            if (Regex.IsMatch(word, @"[^aeiouy]y$")) { word = Regex.Replace(word, @"y$", "i"); }

            //step 2
            if (word.EndsWith("tional")) { word = Regex.Replace(word, @"tional$", "tion"); }
            else if (word.EndsWith("enci")) { word = Regex.Replace(word, @"enci$", "ence"); }
            else if (word.EndsWith("anci")) { word = Regex.Replace(word, @"anci$", "ance"); }
            else if (word.EndsWith("abli")) { word = Regex.Replace(word, @"abli$", "able"); }
            else if (word.EndsWith("entli")) { word = Regex.Replace(word, @"entli$", "ent"); }
            else if (word.EndsWith("iser") || word.EndsWith("isation")) { word = Regex.Replace(word, @"(iser|isation)$", "ize"); }
            else if (word.EndsWith("ational") || word.EndsWith("ation") || word.EndsWith("ator")) { word = Regex.Replace(word, @"(ational|ation|ator)$", "ate"); }
            else if (word.EndsWith("alism") || word.EndsWith("aliti") || word.EndsWith("alli")) { word = Regex.Replace(word, @"(alism|aliti|alli)$", "al"); }
            else if (word.EndsWith("fulness")) { word = Regex.Replace(word, @"(fulness)$", "ful"); }
            else if (word.EndsWith("ousli") || word.EndsWith("ousness")) { word = Regex.Replace(word, @"(ousli|ousness)$", "ous"); }
            else if (word.EndsWith("iveness") || word.EndsWith("iviti")) { word = Regex.Replace(word, @"(iveness|iviti)$", "ive"); }
            else if (word.EndsWith("biliti") || word.EndsWith("bli")) { word = Regex.Replace(word, @"(biliti|bli)$", "ble"); }
            else if (Regex.IsMatch(word, @"logi$")) { word = Regex.Replace(word, @"logi$", "og"); }
            else if (word.EndsWith("lessli")) { word = Regex.Replace(word, @"lessli$", "less"); }

            return word;
        }
    }
}
