using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace automatic_text_classification
{
    class MainClass
    {
        private static void Main(string[] args)
        {

            int labTotal = 0, conTotal = 0, coaTotal = 0, wordCount = 0;
            //path to directory containing training data
            string pathToDir = "/Users/David/Coding/ai-assignment/AI-Assignment/training_dataset/";

            double fileCount = Directory.GetFiles(pathToDir, "*.*", SearchOption.TopDirectoryOnly).Length;
            Console.WriteLine("Training data " + fileCount);

            string[] files = Directory.GetFiles(pathToDir);

            var governmentDict = new Dictionary<string, int>();

            foreach (string file in files)
            {
                if (governmentDict.ContainsKey(DocGovernment(file))) { governmentDict[DocGovernment(file)]++; }
                else { governmentDict.Add(DocGovernment(file), 1); }
                //Console.WriteLine(file);
            }

            foreach (var govFrequency in governmentDict)
            {
                //Console.WriteLine("{0}: {1}", govFrequency.Key, govFrequency.Value);
            }

            Console.ReadLine();
            Dictionary<string, int> uniqueDict = new Dictionary<string, int>();
            Dictionary<string, int> labDict = new Dictionary<string, int>();
            Dictionary<string, int> coaDict = new Dictionary<string, int>();
            Dictionary<string, int> conDict = new Dictionary<string, int>();

            foreach (string file in files)
            {
                //Console.WriteLine("Filename: {0}", file);
                string government = DocGovernment(file);
                double priorProbability = PriorProbabilities(government, fileCount, governmentDict);
                //Console.WriteLine(priorProbability);
                //Console.ReadLine();
                // key-value pair word frequency
                var dict = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase); // Ignores casing as as think case-sensitivity will have little/no impact on accuracy of algorithm
                wordCount = WordFrequency(file, dict);

                switch (government)
                {
                    case "Conservative":
                        conTotal += wordCount; // Total number of words in each category including repeats
                        conDict = conDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                        //conDict.Add("Prior Probability", priorProbability); can't put double in the dictionary
                        double conPriorProbability = priorProbability;
                        //Console.WriteLine(conPriorProbability);
                        foreach (var wordFrequency in conDict)
                        {
                         //  Console.WriteLine("{0}: {1}", wordFrequency.Key, wordFrequency.Value);
                        }
                        //Console.ReadLine();
                        break;
                    case "Coalition":
                        coaTotal += wordCount; 
                        coaDict = coaDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                        double coaPriorProbability = priorProbability;
                        break;
                    case "Labour":
                        labTotal += wordCount; 
                        labDict = labDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                        double labPriorProbability = priorProbability;
                        break;
                }

                //Console.WriteLine("{0}, {1}, {2}", coaTotal, conTotal, labTotal);
                //Console.ReadLine();
                string fileName = Path.GetFileNameWithoutExtension(file);

                /*
                string stopWordsFile = "/Users/David/Coding/ai-assignment/AI-Assignment/stopwords.txt"; //Stop Words look up table
                StreamReader sr = new StreamReader(stopWordsFile);
                string stopWordsText = File.ReadAllText(stopWordsFile);

                var stopWords = stopWordsText.Split();

                foreach (var word in stopWords)
                {
                    if (dict.ContainsKey(word)) { dict.Remove(word); } //Removing stop words from dictionary
                }*/

                foreach (var wordFrequency in dict)
                {
                  //  Console.WriteLine("{0}: {1}", wordFrequency.Key, wordFrequency.Value);
                }

                //Unique words over all training data
                uniqueDict = uniqueDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                /*
                String csv = String.Join(
                    Environment.NewLine,
                    dict.Select(d => d.Key + "," + d.Value)
                );
                File.WriteAllText(pathToDir + fileName + ".csv", csv);*/
            }

           // Console.WriteLine("\nUnique dict\n");
            foreach (var wordFrequency in uniqueDict)
            {
                //Console.WriteLine("{0}: {1}", wordFrequency.Key, wordFrequency.Value);
            }

            int nWords = uniqueDict.Count(); //Total number of unique words throughout training documents

            var concpdict = new Dictionary<string, float>();
            var coacpdict = new Dictionary<string, float>();
            var labcpdict = new Dictionary<string, float>();


            foreach (KeyValuePair<string, int> fcat in conDict)
            {
                //Console.WriteLine(fcat.Value); Console.ReadLine();
                int ffcat = fcat.Value;
                float cp = ConditionalProbability(ffcat, conTotal, nWords);
                concpdict.Add(fcat.Key, cp); // Building conditional probability table
            }
            foreach (KeyValuePair<string, int> fcat in coaDict)
            {
                float cp = ConditionalProbability(fcat.Value, coaTotal, nWords);
                coacpdict.Add(fcat.Key, cp); 
            }
            foreach (KeyValuePair<string, int> fcat in labDict)
            {
                float cp = ConditionalProbability(fcat.Value, labTotal, nWords);
                labcpdict.Add(fcat.Key, cp); 
            }

            foreach (var wordFrequency in concpdict)
            {
                Console.WriteLine("{0}: {1}", wordFrequency.Key, wordFrequency.Value);
            }
            Console.ReadLine();
        }

        public static double PriorProbabilities( string government, double fileCount, Dictionary<string, int> governmentDict) 
        {
            double priorProbability = governmentDict[government] / fileCount;
            return priorProbability;
        }

        //Count the frequency of each unique word.  
        private static int WordFrequency(string file, Dictionary<string, int> words)
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

        public static float ConditionalProbability(int fcat, int ncat, int nWords)
        {
            //Console.WriteLine("{0},{1},{2}",fcat,ncat,nWords);
            float top = fcat + 1;
            float bottom = ncat + nWords;
            float conditionalProbability = top / bottom;
            Console.WriteLine(conditionalProbability);
            return conditionalProbability;
        }

        public static string DocGovernment(string fileName)
        {
            //get government from filename
            string government = "";

            if (fileName.ToLower().Contains("conservative"))
            {
                government = "Conservative";
            }
            else if (fileName.ToLower().Contains("labour"))
            {
                government = "Labour";
            }
            else if (fileName.ToLower().Contains("coalition"))
            {
                government = "Coalition";
            }
            else
            {
                Console.WriteLine("Couldn't determine government\nPlease ensure filename of " +
                                  "training data contains government");
                Console.ReadLine();
            }
            //Console.WriteLine("Government: {0}", government);
            //Regex docGovernment = new Regex("[^a-zA-Z0-9]");
            return government;
        }
    }
}
