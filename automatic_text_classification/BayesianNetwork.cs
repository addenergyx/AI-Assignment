using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace automatic_text_classification
{
    public static class BayesianNetwork
    {
        public static void WriteBayesianNetwork(Dictionary<string, int> dict, Dictionary<string, double> cpDict, object government)
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); //Multiplatform home environment

            Console.WriteLine("\nEnter file name to save Navie Bayes table for category {0}\n[Current location: {1}]\n[Make sure to include extension '.csv']", government, home);
            string fileName = Console.ReadLine();

            //string filePath = "/Users/David/Coding/ai-assignment/AI-Assignment/test_dataset/";

            String csv = String.Join(
                Environment.NewLine,
                dict.Select(d => d.Key + "," + d.Value + "," + cpDict[d.Key])
            );
            File.WriteAllText(home + fileName, csv);
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
    }
}
