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
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); //Multiplatform home environment

        public Menu()
        {
            int labTotal = 0, conTotal = 0, coaTotal = 0, wordCount = 0;
            double conPriorProbability = 0D, coaPriorProbability = 0D, labPriorProbability = 0D;
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

                        while (!Directory.Exists(pathToDir)) //throw new ArgumentException("File doesn't exist, enter new path")
                        {
                            Console.WriteLine("Path does not exist!!! Please enter full path to training data directory");
                            pathToDir = Console.ReadLine().Trim(); 
                            pathToDir = "/Users/David/Coding/ai-assignment/AI-Assignment/training_dataset/";
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
                            double priorProbability = MainClass.PriorProbabilities(government, fileCount, governmentDict);
     
                            // key-value pair word frequency
                            var dict = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase); // Ignores casing as as think case-sensitivity will have little/no impact on accuracy of algorithm
                            wordCount = MainClass.WordFrequency(file, dict);

                            switch (government)
                            {
                                case nameof(MainClass.Government.Conservative):
                                    conTotal += wordCount; // Total number of words in each category including repeats
                                    conDict = conDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    conPriorProbability = priorProbability;
                                    break;
                                case nameof(MainClass.Government.Coalition):
                                    coaTotal += wordCount;
                                    coaDict = coaDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    coaPriorProbability = priorProbability;
                                    break;
                                case nameof(MainClass.Government.Labour):
                                    labTotal += wordCount;
                                    labDict = labDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    labPriorProbability = priorProbability;
                                    break;
                                default:
                                    Console.WriteLine("Could not determine government");
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

                        var concpdict = new Dictionary<string, double>();
                        var coacpdict = new Dictionary<string, double>();
                        var labcpdict = new Dictionary<string, double>();

                        foreach (KeyValuePair<string, int> fcat in conDict)
                        {
                            //Console.WriteLine(fcat.Value); Console.ReadLine();
                            double cp = MainClass.ConditionalProbability(fcat.Value, conTotal, nWords);
                            concpdict.Add(fcat.Key, cp); // Building conditional probability table
                        }
                        foreach (KeyValuePair<string, int> fcat in coaDict)
                        {
                            double cp = MainClass.ConditionalProbability(fcat.Value, coaTotal, nWords);
                            coacpdict.Add(fcat.Key, cp);
                        }
                        foreach (KeyValuePair<string, int> fcat in labDict)
                        {
                            double cp = MainClass.ConditionalProbability(fcat.Value, labTotal, nWords);
                            labcpdict.Add(fcat.Key, cp);
                        }

                        //Tables
                        //condict and concpdict

                        string pathToTest = PathToTestDocument();

                        while (!File.Exists(pathToTest)) 
                        {
                            Console.WriteLine("File does not exist!!! Please enter full path to test document");
                            pathToTest = Console.ReadLine().Trim(); 
                            pathToTest = "/Users/David/Coding/ai-assignment/AI-Assignment/test_dataset/test1.txt";
                        }

                        Dictionary < string, int> testDict = new Dictionary<string, int>();
                        MainClass.WordFrequency(pathToTest, testDict);
                        MainClass.Classification(testDict, concpdict, coacpdict, labcpdict, conPriorProbability,
                                                 coaPriorProbability, labPriorProbability);

                        MainClass.WriteBayesianNetwork (conDict, concpdict);
                        MainClass.WriteBayesianNetwork (coaDict, coacpdict);
                        MainClass.WriteBayesianNetwork (labDict, labcpdict);

                        AnykeyToContinue();
                        Console.Clear();
                        break;

                    case 2:
                        Console.Clear();
                        string pathToFile = PathToTestDocument();
                        //Dictionary<string, string> govPathDict = new Dictionary<string, string>();

                        string[] governments = { "Conservative", "Labour", "Coalition" };
                        string[] paths = new string[3];

                        var a = new Dictionary<string, int>();
                        var b = new Dictionary<string, double>();

                        foreach (MainClass.Government party in Enum.GetValues(typeof(MainClass.Government)))
                        {
                            string pathToBayesian = PathToBayesianNetwork(nameof(party));
                            MainClass.ReadBayesianNetwork(pathToBayesian, a, b);
                            switch (party)
                            {
                                case MainClass.Government.Conservative:
                                    conDict = a; concpdict = b;
                                    break;
                                case MainClass.Government.Labour:
                                    labDict = a; labcpdict = b;
                                    break;
                                case MainClass.Government.Coalition:
                                    coaDict = a; coacpdict = b;
                                    break;
                            }
                        }

                        while (!File.Exists(pathToFile))
                        {
                            Console.WriteLine("File does not exist!!! Please enter full path to test document");
                            pathToFile = Console.ReadLine().Trim();
                        }

                        Dictionary<string, int> fileDict = new Dictionary<string, int>();
                        MainClass.WordFrequency(pathToFile, fileDict);

                        //MainClass.Classification(testDict, concpdict, coacpdict, labcpdict, conPriorProbability,
                        //coaPriorProbability, labPriorProbability);
                        AnykeyToContinue();
                        Console.Clear();
                        break;

                    case 0:
                        Console.WriteLine("\nExiting program... ");
                        answer = exit;
                        break;

                    default:
                        Console.Clear();
                        Console.WriteLine("Enter a valid number to continue... ");
                        break;
                        
                }

            } while (answer != exit);
        }

        static int DisplayMenu()
        {
            bool validInput;

            Console.WriteLine("Queen's Speech Automatic Text Classification");
            Console.WriteLine("");
            Console.WriteLine("Select from the following options:");
            Console.WriteLine("(1) Undertake Training");
            Console.WriteLine("(2) Undertake a Classification");
            Console.WriteLine("(0) Quit");

            string userInput = Console.ReadLine();
            validInput = Int32.TryParse(userInput, out int result);

            while (!validInput)
            { 
                Console.WriteLine("Invalid Input, Enter either (0),(1),(2)"); 
                userInput = Console.ReadLine(); 
                validInput = Int32.TryParse(userInput, out result); 
            }
           
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

        private string PathToBayesianNetwork(string government)
        {
            return AskForInfoString("Please enter full path to Bayesian Network for " + government);
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
