using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace automatic_text_classification
{
    public class Menu
    {

        const int exit = 0;
        public Menu()
        {
            int labTotal = 0, conTotal = 0, coaTotal = 0, wordCount = 0;
            float conPriorProbability = 0.0f, coaPriorProbability = 0.0f, labPriorProbability = 0.0f;
            int answer = 3;

            do
            {
                answer = DisplayMenu();
                switch (answer)
                {
                    case 1:
                        Console.Clear();
                        Title();
                        string pathToDir = PathToDirectory(); //path to directory containing training data
                        //string pathToDir = "/Users/David/Coding/ai-assignment/AI-Assignment/training_dataset/";

                        while (!Directory.Exists(pathToDir)) //throw new ArgumentException("File doesn't exist, enter new path")
                        {
                            Console.WriteLine("Path does not exist!!! Please enter full path to training data directory");
                            pathToDir = Console.ReadLine().Trim();
                        }

                        double fileCount = Directory.GetFiles(pathToDir, "*.*", SearchOption.TopDirectoryOnly).Length;
                        Console.WriteLine("Training data: " + fileCount);

                        string[] files = Directory.GetFiles(pathToDir);

                        var governmentDict = new Dictionary<string, int>();
                        Dictionary<string, int> uniqueDict = new Dictionary<string, int>();
                        Dictionary<string, int> labDict = new Dictionary<string, int>();
                        Dictionary<string, int> coaDict = new Dictionary<string, int>();
                        Dictionary<string, int> conDict = new Dictionary<string, int>();

                        foreach (string file in files)
                        {
                            if (governmentDict.ContainsKey(MainClass.DocGovernment(file))) { governmentDict[MainClass.DocGovernment(file)]++; }
                            else { governmentDict.Add(MainClass.DocGovernment(file), 1); }
                            //Console.WriteLine(file);
                        }

                        foreach (string file in files)
                        {
                            string government = MainClass.DocGovernment(file);
                            float priorProbability = MainClass.PriorProbabilities(government, fileCount, governmentDict);
     
                            // key-value pair word frequency
                            var dict = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase); // Ignores casing as as think case-sensitivity will have little/no impact on accuracy of algorithm
                            wordCount = MainClass.WordFrequency(file, dict);

                            switch (government)
                            {
                                case "Conservative":
                                    conTotal += wordCount; // Total number of words in each category including repeats
                                    conDict = conDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    //conDict.Add("Prior Probability", priorProbability); can't put double in the dictionary
                                    conPriorProbability = priorProbability;
                                    break;
                                case "Coalition":
                                    coaTotal += wordCount;
                                    coaDict = coaDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    coaPriorProbability = priorProbability;
                                    break;
                                case "Labour":
                                    labTotal += wordCount;
                                    labDict = labDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    labPriorProbability = priorProbability;
                                    break;
                            }

                            string fileName = Path.GetFileNameWithoutExtension(file);

                            /*  //Old method to remove stopwords
                            string stopWordsFile = "/Users/David/Coding/ai-assignment/AI-Assignment/stopwords.txt"; //Stop Words look up table
                            StreamReader sr = new StreamReader(stopWordsFile);
                            string stopWordsText = File.ReadAllText(stopWordsFile);

                            var stopWords = stopWordsText.Split();

                            foreach (var word in stopWords)
                            {
                                if (dict.ContainsKey(word)) { dict.Remove(word); } //Removing stop words from dictionary
                            }*/

                            //Unique words over all training data
                            uniqueDict = uniqueDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());


                        }

                        int nWords = uniqueDict.Count(); //Total number of unique words throughout training documents

                        var concpdict = new Dictionary<string, float>();
                        var coacpdict = new Dictionary<string, float>();
                        var labcpdict = new Dictionary<string, float>();


                        foreach (KeyValuePair<string, int> fcat in conDict)
                        {
                            //Console.WriteLine(fcat.Value); Console.ReadLine();
                            int ffcat = fcat.Value;
                            float cp = MainClass.ConditionalProbability(ffcat, conTotal, nWords);
                            concpdict.Add(fcat.Key, cp); // Building conditional probability table
                        }
                        foreach (KeyValuePair<string, int> fcat in coaDict)
                        {
                            float cp = MainClass.ConditionalProbability(fcat.Value, coaTotal, nWords);
                            coacpdict.Add(fcat.Key, cp);
                        }
                        foreach (KeyValuePair<string, int> fcat in labDict)
                        {
                            float cp = MainClass.ConditionalProbability(fcat.Value, labTotal, nWords);
                            labcpdict.Add(fcat.Key, cp);
                        }

                        string pathToFile = PathToTestDocument();
                        //string pathToFile = "/Users/David/Coding/ai-assignment/AI-Assignment/test_dataset/test1.txt";

                        while (!File.Exists(pathToFile)) 
                        {
                            Console.WriteLine("File does not exist!!! Please enter full path to test document");
                            pathToFile = Console.ReadLine().Trim();
                        }

                        Dictionary < string, int> testDict = new Dictionary<string, int>();
                        MainClass.WordFrequency(pathToFile, testDict);
                        MainClass.Classification(testDict, concpdict, coacpdict, labcpdict, conPriorProbability,
                                                 coaPriorProbability, labPriorProbability);

                        AnykeyToContinue();
                        Console.Clear();
                        break;

                    case 2:
                        Console.Clear();
                        string pathToTest = PathToTestDocument();
                        AnykeyToContinue();
                        Console.Clear();
                        break;

                    case 0:
                        Console.WriteLine("\nExiting program... ");
                        answer = exit;
                        break;

                    default:
                        Console.Clear();
                        Console.WriteLine("\nEnter a valid number to continue. ");
                        break;
                        
                }

            } while (answer != exit);
        }

        static int DisplayMenu()
        {
            int result = 0;
            bool validInput;

            Console.WriteLine("Queen's Speech Automatic Text Classification");
            Console.WriteLine("");
            Console.WriteLine("Select from the following options:");
            Console.WriteLine("(1) Undertake Training");
            Console.WriteLine("(2) Undertake a Classification");
            Console.WriteLine("(0) Quit");

            string userInput = Console.ReadLine();
            validInput = Int32.TryParse(userInput, out result);

            if (validInput) { return result; }
            else { Console.WriteLine("Invalid Input"); }
           
            return result;
        }

        private string PathToDirectory()
        {
            return AskForInfoString("Please enter full path to training data directory");
        }

        private string PathToTestDocument()
        {
            return AskForInfoString("Please enter full path to test document");
        }

        public static void AnykeyToContinue()
        {
            Console.WriteLine("\nPress any key to continue.");
            Console.ReadKey();
        }

        public static void Title()
        {
            Console.Clear();
            Console.WriteLine("Queen's Speech Automatic Text Classification\n");
        }

        string AskForInfoString(string message)
        {
            string userInput;

            Console.WriteLine(message);
            userInput = Console.ReadLine().Trim();

            return userInput;
        }
    }
}
