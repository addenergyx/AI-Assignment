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
            //path to directory containing training data
            string pathToDir = "/Users/David/Coding/ai-assignment/AI-Assignment/training_dataset";

            int fileCount = Directory.GetFiles(pathToDir, "*.*", SearchOption.TopDirectoryOnly).Length;
            Console.WriteLine("Training data " + fileCount);

            foreach (string file in Directory.GetFiles(pathToDir))
            {
                Console.WriteLine("Filename: {0}", file);
                string government = DocGovernment(file);

                // key-value pair word frequency
                var dict = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase); // Ignores casing as as think case-sensitivity will have little/no impact on accuracy of algorithm
                WordFrequency(file, dict);

                string stopWordsFile = "/Users/David/Coding/ai-assignment/AI-Assignment/stopwords.txt"; //Stop Words look up table
                StreamReader sr = new StreamReader(stopWordsFile);
                string stopWordsText = File.ReadAllText(stopWordsFile);

                Console.WriteLine(stopWordsText);
                Console.ReadLine();

                var stopWords = stopWordsText.Split();

                foreach (var word in stopWords)
                {
                    if (dict.ContainsKey(word)){ dict.Remove(word); } //Removing stop words from dictionary
                }

                foreach (var wordFrequency in dict)
                {
                    Console.WriteLine("{0}: {1}", wordFrequency.Key, wordFrequency.Value);
                }

            }

        }

        //Count the frequency of each unique word.  
        private static void WordFrequency(string file, Dictionary<string, int> words)
        {
            var document = File.ReadAllText(file).ToLower(); //Change file to lower case

            var wordPattern = new Regex(@"\w+"); // \w+ matches any word character plus 1, this should account for apostrophes as I think these can affect algorithm performance

            foreach (Match match in wordPattern.Matches(document))
            {
                int currentCount = 0;
                words.TryGetValue(match.Value, out currentCount);

                currentCount++;
                words[match.Value] = currentCount;
            }
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

            Console.WriteLine("Government: {0}", government);
            //Regex docGovernment = new Regex("[^a-zA-Z0-9]");

            return government;
        }
    }
}
