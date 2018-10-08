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
            Console.ReadLine();

                foreach (string fileName in Directory.GetFiles(pathToDir))
                {
                    Console.WriteLine("Filename: {0}", fileName);
                    string government = DocGovernment(fileName);

                    StreamReader sr = new StreamReader(fileName);
                    string doc = File.ReadAllText(fileName).ToLower(); // Changing all text to lower as think case-sensitivity will have little/no impact on accuracy of algorithm 

                    Console.WriteLine(doc);
                    Console.ReadLine();

                    doc = Regex.Replace(doc, @"[\p{P}-[']]", ""); //Removes all punctuation apart from apostrophe
                    
                    Console.WriteLine(doc);
                    Console.ReadLine();
                    
                    Dictionary<string, int> dict = new Dictionary<string, int>(); // key-value pair word frequency
                    var words = doc.Split(' '); //lists all words in document

                    foreach (var word in words)
                    {
                        if (dict.ContainsKey(word))
                            dict[word]++;
                        else
                            dict.Add(word, 1);
                    }

                    foreach (var wordFrequency in dict)
                    {
                        Console.WriteLine("{0}: {1}", wordFrequency.Key, wordFrequency.Value);
                    }
                    Console.ReadLine();

                }

            Console.ReadLine();

        }

        //Count the frequency of each unique word.  
        public static int WordFrequency(string doc, string word)
        {
            int count = 0;
            int i = 0;
            while ((i = doc.IndexOf(word, i)) != -1)
            {
                i += word.Length; 
                count++;
            }
            Console.WriteLine("{0} {1}", count, word);
            return count;
        }

        public static string DocGovernment(string fileName){
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
