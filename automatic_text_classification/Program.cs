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

            var best = predDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            Console.WriteLine("---------------------");
            Console.WriteLine("This document is predicted to be " + best + "\n");
        }

        public static double PriorProbabilities( string government, int fileCount, Dictionary<string, int> governmentDict) 
        {
            double priorProbability = governmentDict[government] / (double)fileCount;
            return priorProbability;
        }

        //Count the frequency of each unique word.  
        public static int WordFrequency(string file, Dictionary<string, int> words)
        {
            var document = File.ReadAllText(file).ToLower(); //Change file to lower case
            string stopWordsFile = "/Users/David/Coding/ai-assignment/AI-Assignment/stopwords.txt"; //Stop Words look up table
            StreamReader sr = new StreamReader(stopWordsFile);
            string stopWordsText = File.ReadAllText(stopWordsFile);
            //Console.ReadLine();

            var stopWords = stopWordsText.Split();

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
            Console.WriteLine(conditionalProbability);
            return conditionalProbability;
        }

        public static string DocGovernment(string fileName)
        {
            //get government from filename
            string government = "";

            string[] parties = Enum.GetNames(typeof(Government));

            int q = 0;

            while (!fileName.ToLower().Contains(parties[q].ToLower())) //alot neater then if statement below
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
            StreamReader sr = new StreamReader(file);
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

                //var dict = File.ReadLines(file).Select(line => line.Split(',')).ToDictionary(line => line[0], line => line[1]);
                //var bndict = new Dictionary<string, Dictionary<int, float>>();



        }
    }
}
