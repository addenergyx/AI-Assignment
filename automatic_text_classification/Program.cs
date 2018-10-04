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
            string pathToDir = "/Users/David/Coding/ai-assignment/AI-Assignment/training_dataset/Conservative27thMay2015.txt";
            StreamReader sr = new StreamReader(pathToDir);
            Hashtable hashTable = new Hashtable();
            Dictionary<string, int> dictionary = new Dictionary<string, int>();

            string doc = File.ReadAllText(pathToDir);

            Console.WriteLine(doc);
            Console.ReadLine();

            Regex reg_exp = new Regex("[^a-zA-Z0-9]");
            doc = reg_exp.Replace(doc, " "); //Removes punctuation from document


            string[] words = doc.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            var word_query = (from string word in words orderby word select word).Distinct();
            string[] result = word_query.ToArray();
            int counter = 0;
            string delim = " ,.";
            string[] fields = null;
            string line = null;

            while (!sr.EndOfStream)
            {
                line = sr.ReadLine(); 
                line.Trim();
                fields = line.Split(delim.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                counter += fields.Length;  
                foreach (string word in result)
                {
                    int count = WordFrequency(doc, word);

                    //hashtable vs dictionary
                    //hashTable.Add(word, count);
                    //dictionary.Add(word, count);

                }
            }

            sr.Close();
            Console.WriteLine("The total word count is {0}", counter);
            Console.ReadLine();
            /*
            Console.WriteLine("-------Dictionary------");
            foreach (KeyValuePair<string, int> kvp in dictionary)
            {
                Console.WriteLine(kvp.Key.ToString() + " - " + kvp.Value.ToString());
            }
            Console.Read();
            */
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
    }
}
