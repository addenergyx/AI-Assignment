using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualBasic;



namespace automatic_text_classification
{
    class MainClass
    {

        private static void Main(string[] args)
        {

            Menu display = new Menu();
            Console.ReadLine();
        }

        public enum Government { Labour, Conservative, Coalition };
        //public Government party;

        public static void Classification(Dictionary<string, int> testDict, Dictionary<string, double> concpdict, 
                                          Dictionary<string, double> coacpdict, Dictionary<string, double> labcpdict, 
                                          double conPriorProbability, double coaPriorProbability, double labPriorProbability)
        {

            double conProb = 0D, coaProb = 0D, labProb = 0D;

            foreach (var word in testDict.Keys)
            {
                if (concpdict.ContainsKey(word)) { conProb = conProb + (concpdict[word] * testDict[word]); }
                if (coacpdict.ContainsKey(word)) { coaProb = coaProb + (coacpdict[word] * testDict[word]); }
                if (labcpdict.ContainsKey(word)) { labProb = conProb + (labcpdict[word] * testDict[word]); }
            }

            //conProb = Math.Pow(10, conProb) * conPriorProbability; //inverse of log, c# doesn't have power (^) operator, 
            conProb = conProb * conPriorProbability;
            coaProb = coaProb * coaPriorProbability;
            labProb = labProb * labPriorProbability;


            var predDict = new Dictionary<object, double>
                        {
                            {Government.Labour, labProb },
                            {Government.Conservative, conProb},
                            {Government.Coalition, coaProb}
                        };

            Console.Clear();
            foreach (KeyValuePair<object,double> pred in predDict)
            {
                Console.WriteLine("Probability of {0}: {1:00.00}%", pred.Key, pred.Value*100); //string formating to display percentage to 2dp
            }

            var best = predDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key; //selects key with highest value by comparing
            Console.WriteLine("---------------------");
            Console.WriteLine("This document is predicted to be " + best + "\n");
        }

        public static double PriorProbabilities( string government, int fileCount, Dictionary<string, int> governmentDict) 
        {
            double priorProbability = governmentDict[government] / (double)fileCount;
            return priorProbability;
        }

        //Count the frequency of each unique word.  
        public static int WordFrequency(string file, Dictionary<string, int> words, string stopWordsFile)
        {

            //string stopWordsFile = "/Users/David/Coding/ai-assignment/AI-Assignment/stopwords.txt"; //Stop Words look up table
            StreamReader sr = new StreamReader(stopWordsFile);
            string stopWordsText = File.ReadAllText(stopWordsFile);
            //Console.ReadLine();

            var stopWords = stopWordsText.Split();

            var document = File.ReadAllText(file).ToLower(); //Change file to lower case

            foreach (var word in stopWords) { document = Regex.Replace(document, "\\b"+word+"\\b", ""); }

            int wordCount = document.Split(' ').Length; //Total number of words in each doc including repeats. This method of counting words takes a considerable amount of time

            var wordPattern = new Regex(@"\w+"); // \w+ matches any word character plus 1, this should account for apostrophes as I think these can affect algorithm performance

            foreach (Match match in wordPattern.Matches(document))
            {
                words.TryGetValue(match.Value, out int currentCount);
                currentCount++;
                words[match.Value] = currentCount;
            }

            return wordCount;
        }

        public static double ConditionalProbability(int fcat, int ncat, int nWords)
        {
            //Console.WriteLine("{0},{1},{2}",fcat,ncat,nWords);
            double top = fcat + 1;
            double bottom = ncat + nWords;
            //double conditionalProbability = Math.Log10(top / bottom); //Taking log of probability to avoid floating-point overflow errors
            double conditionalProbability = top / bottom;
            //Console.WriteLine(conditionalProbability);
            return conditionalProbability;
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

        public static void WriteBayesianNetwork (Dictionary<string, int> dict, Dictionary<string, double> cpDict, object government)
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); //Multiplatform home environment

            Console.WriteLine("\nEnter file name to save Navie Bayes table for category {0}\n[Current location: {1}]\n[Make sure to include extension '.csv']", government,home);
            string fileName = Console.ReadLine();

            //string filePath = "/Users/David/Coding/ai-assignment/AI-Assignment/test_dataset/";

            String csv = String.Join(
                Environment.NewLine,
                dict.Select(d => d.Key + "," + d.Value + "," + cpDict[d.Key])
            );
            File.WriteAllText( home + fileName, csv);
        }

        public static void ReadBayesianNetwork(string file, Dictionary<string, int> a, Dictionary<string, double> b)
        {
            using (StreamReader sr = new StreamReader(file))
            {
                string data = sr.ReadLine();

                while (!sr.EndOfStream)
                {
                    Console.WriteLine(data);
                    data = sr.ReadLine();
                    string[] values = data.Split(',');
                    Console.WriteLine(values[0]);
                    Console.WriteLine(values[1]);
                    Console.WriteLine(values[2]);

                    string word = values[0];
                    int frequency = Int32.Parse(values[1]);
                    double conditionalprobability = Double.Parse(values[2]);

                    a.Add(word, frequency);
                    b.Add(word, conditionalprobability);
                }
            }
                //var dict = File.ReadLines(file).Select(line => line.Split(',')).ToDictionary(line => line[0], line => line[1]);
                //var bndict = new Dictionary<string, Dictionary<int, float>>();



        }

        public static string Stemming(string word)
        {
            /*Following Porter's stemming algorithm
             * rules in order
             * 1a)
             * sses -> ss
             * ies -> i - stemming can cause some words to be incorrect for example duties would become duti 
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
                if (word.EndsWith("sses", StringComparison.CurrentCultureIgnoreCase)) { Regex.Replace(word, "sses$", "es"); }
                //if (word.EndsWith("ies", StringComparison.CurrentCultureIgnoreCase)) { Regex.Replace(word, "ies$", "y"); }

                if (word.EndsWith("ies", StringComparison.CurrentCultureIgnoreCase) || word.EndsWith("ied", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Regex.IsMatch(word, @"^[a-z]ie[sd]")) { Regex.Replace(word, ".$", ""); }
                    else { Regex.Replace(word, @"ie[sd]$", "y"); }
                }

                if (Regex.IsMatch(word, @"([aeiouy][a-r][t-z])s$")) //delete s if preceding word part contains a vowel not immediately before the s
                {
                    word = Regex.Replace(word, "s$", "");
                }

            }

            //step 1b
            if (Regex.IsMatch(word, @"ee.*[dly]$")) { Regex.Replace(word, @"[l]?[dy]$", ""); }

            if(Regex.IsMatch(word, @"(ed|edly|ingly|ing)$"))
            {
                if (Regex.IsMatch(word, @".*[aeiouy].*(ed|edly|ingly|ing)$")) 
                { 
                    Regex.Replace(word, @"(ed|edly|ingly|ing)$", "");
                    if (Regex.IsMatch(word, @"(at|bl|iz)$")) { word += "e"; }//append 'e' to word
                    else if (Regex.IsMatch(word, @"(.)\1$")) { Regex.Replace(word, @".$", ""); }
                    else if (word.Length < 4) { word += "e"; }
                }
            }

            //if (word.EndsWith("ed") || word.EndsWith("edly") || word.EndsWith("ing") || word.EndsWith("ingly"))

            //step 1c
            if (Regex.IsMatch(word, @"[^aeiouy]y$")) { Regex.Replace(word, @"y$", "i"); }

            //step 2
            if (word.EndsWith("tional")) { Regex.Replace(word, @"tional$", "tion"); }
            else if (word.EndsWith("enci")) { Regex.Replace(word, @"enci$", "ence"); }
            else if (word.EndsWith("anci")) { Regex.Replace(word, @"anci$","ance"); }
            else if (word.EndsWith("abli")) { Regex.Replace(word,@"abli$","able"); }
            else if (word.EndsWith("entli")) { Regex.Replace(word,@"entli$", "ent"); }
            else if (word.EndsWith("iser") || word.EndsWith("isation")) { Regex.Replace(word,@"(iser|isation)$","ize"); }
            else if (word.EndsWith("ational") || word.EndsWith("ation") || word.EndsWith("ator")) { Regex.Replace(word, @"(ational|ation|ator)$", "ate"); }
            else if (word.EndsWith("alism") || word.EndsWith("aliti") || word.EndsWith("alli")) { Regex.Replace(word, @"(alism|aliti|alli)$", "al"); }
            else if (word.EndsWith("fulness")) { Regex.Replace(word, @"(fulness)$", "ful"); }
            else if (word.EndsWith("ousli") || word.EndsWith("ousness")) { Regex.Replace(word, @"(ousli|ousness)$", "ous"); }
            else if (word.EndsWith("iveness") || word.EndsWith("iviti")) { Regex.Replace(word, @"(iveness|iviti)$", "ive"); }
            else if (word.EndsWith("biliti") || word.EndsWith("bli")) { Regex.Replace(word, @"(biliti|bli)$", "ble"); }
            else if (Regex.IsMatch(word, @"logi$")) { Regex.Replace(word, @"logi$", "og"); }
            else if (word.EndsWith("lessli")) { Regex.Replace(word, @"lessli$", "less"); }

            return word;
        }
    }
}
