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
        readonly string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); //Multiplatform home environment, made readonly so can't be modified

        public Menu()
        {

            int answer = 3;

            do
            {
                //By having variable initalization before switch statement but inside loop ensures variables are reset each time user is choosing between a) undertake  training or b) undertake a classification.  
                answer = DisplayMenu();
                var governmentDict = new Dictionary<string, int>();
                Dictionary<string, int> uniqueDict = new Dictionary<string, int>();
                Dictionary<string, int> labDict = new Dictionary<string, int>();
                Dictionary<string, int> coaDict = new Dictionary<string, int>();
                Dictionary<string, int> conDict = new Dictionary<string, int>();
                Dictionary<string, int> fileDict = new Dictionary<string, int>();
                var concpdict = new Dictionary<string, double>();
                var coacpdict = new Dictionary<string, double>();
                var labcpdict = new Dictionary<string, double>();
                var dict = new Dictionary<string, int>(StringComparer.CurrentCultureIgnoreCase); // Ignores casing as I think case-sensitivity will have little/no impact on accuracy of algorithm could compare results at some point
                //dict is a temporary dictionary where calculations are made before being moved to a govenment dictionary
                int labTotal = 0, conTotal = 0, coaTotal = 0, wordCount = 0, nWords = 0, fileCount = 0;
                double conPriorProbability = 0D, coaPriorProbability = 0D, labPriorProbability = 0D, priorProbability = 0D;
                string stopWordsFile, pathToTest, pathToDir;
                string[] files;

                switch (answer)
                {
                    case 1:
                        Title();

                        pathToDir = PathToDirectory(); //path to directory containing training data
                        while (!Directory.Exists(pathToDir)) //throw new ArgumentException("File doesn't exist, enter new path")
                        {
                            Console.WriteLine("Path does not exist!!! Please enter full path to training data directory");
                            pathToDir = Console.ReadLine().Trim();
                            //pathToDir = "/Users/David/Coding/ai-assignment/AI-Assignment/training_dataset/";
                            pathToDir = "training_dataset"; //gets file from debug/bin - for testing purposes at the moment
                        }

                        stopWordsFile = Doc.FileExists(PathToStopWords(), "stop words file"); //Stopwords lookup table

                        Console.WriteLine("Training datasets: " + Doc.FileCount(pathToDir));

                        files = Directory.GetFiles(pathToDir);

                        foreach (string file in files)
                        {
                            //Building government dictionary used to keep track of number of datasets for each government
                            if (governmentDict.ContainsKey(Doc.DocGovernment(file))) { governmentDict[Doc.DocGovernment(file)]++; }
                            else { governmentDict.Add(Doc.DocGovernment(file), 1); }
                        }

                        foreach (string file in files)
                        {
                            string government = Doc.DocGovernment(file); //Government of file
                            priorProbability = Calculations.PriorProbabilities(government, Doc.FileCount(pathToDir), governmentDict);
     
                            // key-value pair word frequency
                            wordCount = Calculations.WordFrequency(file, dict, stopWordsFile);

                            switch (government)
                            {
                                case nameof(Doc.Government.Conservative):
                                    conTotal += wordCount; // Total number of words in each category including repeats
                                    conDict = conDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    conPriorProbability = priorProbability;
                                    break;
                                case nameof(Doc.Government.Coalition):
                                    coaTotal += wordCount;
                                    coaDict = coaDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    coaPriorProbability = priorProbability;
                                    break;
                                case nameof(Doc.Government.Labour):
                                    labTotal += wordCount;
                                    labDict = labDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    labPriorProbability = priorProbability;
                                    break;
                                default:
                                    Console.WriteLine("Could not determine government, data will be discarded");
                                    Console.ReadLine();
                                    break;
                            }

                            //string fileName = Path.GetFileNameWithoutExtension(file);
                            /*  //Old method to remove stopwords
                            string stopWordsFile = "/Users/David/Coding/ai-assignment/AI-Assignment/stopwords.txt"; //Stop Words lookup table
                            StreamReader sr = new StreamReader(stopWordsFile);
                            string stopWordsText = File.ReadAllText(stopWordsFile);

                            var stopWords = stopWordsText.Split();

                            foreach (var word in stopWords)
                            {
                                if (dict.ContainsKey(word)) { dict.Remove(word); } //Removing stop words from dictionary
                            }*/

                            //Unique words throughtout all training data
                            uniqueDict = uniqueDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());

                        }

                        nWords = uniqueDict.Count(); //Total number of unique words throughout training documents

                        //After talk in class realised need to keep words that don't appear in a category and set value to 0
                        //To get cat[word] = 0 for words not in a category need to comapre uniqueDict to (category)Dict
                        foreach (KeyValuePair<string, int> word in uniqueDict)
                        {
                            //Expert system technique, for each word all conditions are triggered but only fired if false
                            if (!coaDict.ContainsKey(word.Key)) { coaDict.Add(word.Key, 0); }
                            if (!labDict.ContainsKey(word.Key)) { labDict.Add(word.Key, 0); }
                            if (!conDict.ContainsKey(word.Key)) { conDict.Add(word.Key, 0); }
                        }

                        foreach (KeyValuePair<string, int> fcat in conDict) //fcat refers to frequency of word in given category
                        {
                            double cp = Calculations.ConditionalProbability(fcat.Value, conTotal, nWords);
                            concpdict.Add(fcat.Key, cp); // Building conditional probability dictionary
                        }
                        foreach (KeyValuePair<string, int> fcat in coaDict)
                        {
                            double cp = Calculations.ConditionalProbability(fcat.Value, coaTotal, nWords);
                            coacpdict.Add(fcat.Key, cp);
                        }
                        foreach (KeyValuePair<string, int> fcat in labDict)
                        {
                            double cp = Calculations.ConditionalProbability(fcat.Value, labTotal, nWords);
                            labcpdict.Add(fcat.Key, cp);
                        }

                        pathToTest = Doc.FileExists(PathToTestDocument(), "test document");

                        Calculations.WordFrequency(pathToTest, fileDict, stopWordsFile);
                        Calculations.Classification(fileDict, concpdict, coacpdict, labcpdict, conPriorProbability,
                                                 coaPriorProbability, labPriorProbability);

                        string save = AskForInfoString("Do you wish to save to csv? [Y/N]");
                        if (save.ToLower().Trim().Equals("y") || save.ToLower().Trim().Equals("yes")) //'y' is not working
                        {
                            BayesianNetwork.WriteBayesianNetwork(conDict, concpdict, Doc.Government.Conservative);
                            BayesianNetwork.WriteBayesianNetwork(coaDict, coacpdict, Doc.Government.Coalition);
                            BayesianNetwork.WriteBayesianNetwork(labDict, labcpdict, Doc.Government.Labour);
                        }

                        AnykeyToContinue();
                        break;

                    case 2:
                        Title();

                        //Getting path of test document
                        pathToTest = Doc.FileExists(PathToTestDocument(), "test document");

                        stopWordsFile = Doc.FileExists(PathToStopWords(), "stop words");
                        
                        //Replaced with enum
                        //string[] governments = { "Conservative", "Labour", "Coalition" };
                        //string[] paths = new string[3];

                        //Getting network of premade csv of categories
                        foreach (Doc.Government party in Enum.GetValues(typeof(Doc.Government)))
                        {
                            //temporary dictionaries, given simple variable names to reflect this
                            var a = new Dictionary<string, int>();
                            var b = new Dictionary<string, double>();

                            string pathToBayesian = Doc.FileExists(PathToBayesianNetwork(party.ToString()), party.ToString() + " bayesian network");
                            BayesianNetwork.ReadBayesianNetwork(pathToBayesian, a, b);
                            switch (party)
                            {
                                case Doc.Government.Conservative:
                                    conDict = a; concpdict = b;
                                    break;
                                case Doc.Government.Labour:
                                    labDict = a; labcpdict = b;
                                    break;
                                case Doc.Government.Coalition:
                                    coaDict = a; coacpdict = b;
                                    break;
                            }

                            uniqueDict = uniqueDict.Union(a).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                            int govfilecount = AskForInfoInt("Number of "+party.ToString()+ " documents in test dataset: ");
                            governmentDict.Add(party.ToString(), govfilecount);
                        }

                        foreach (KeyValuePair<string, int> word in uniqueDict)
                        {
                            //Expert system technique, for each word all conditions are triggered but only fired if false
                            if (!coaDict.ContainsKey(word.Key)) { coaDict.Add(word.Key, 0); }
                            if (!labDict.ContainsKey(word.Key)) { labDict.Add(word.Key, 0); }
                            if (!conDict.ContainsKey(word.Key)) { conDict.Add(word.Key, 0); }
                        }

                        //Number of training files
                        fileCount = governmentDict.Sum(x => x.Value);

                        //Prior probability for each category (files in category/total number of training files)
                        foreach (var pair in governmentDict)
                        {
                            priorProbability = Calculations.PriorProbabilities(pair.Key,fileCount,governmentDict);

                            switch (pair.Key)
                            {
                                case nameof(Doc.Government.Conservative):
                                    conPriorProbability = priorProbability;
                                    break;
                                case nameof(Doc.Government.Coalition):
                                    coaPriorProbability = priorProbability;
                                    break;
                                case nameof(Doc.Government.Labour):
                                    labPriorProbability = priorProbability;
                                    break;
                                default:
                                    Console.WriteLine("Could not determine government");
                                    break;
                            }
                        }
                        
                        nWords = uniqueDict.Count(); //Total number of unique words throughout training documents

                        Calculations.WordFrequency(pathToTest, fileDict, stopWordsFile);

                        Calculations.Classification(fileDict, concpdict, coacpdict, labcpdict, conPriorProbability, coaPriorProbability, labPriorProbability);
                        AnykeyToContinue();
                        break;

                    case 3:
                        Title();

                        pathToDir = PathToDirectory(); //path to directory containing training data
                        while (!Directory.Exists(pathToDir)) //throw new ArgumentException("File doesn't exist, enter new path")
                        {
                            Console.WriteLine("Path does not exist!!! Please enter full path to training data directory");
                            pathToDir = Console.ReadLine().Trim();
                            //pathToDir = "/Users/David/Coding/ai-assignment/AI-Assignment/training_dataset/";
                            pathToDir = "training_dataset"; //gets file from debug/bin - for testing purposes at the moment
                        }

                        //stopWordsFile = Doc.FileExists(PathToStopWords(), "stop words file"); //Stopwords lookup table
                        stopWordsFile = "stopwords.txt"; //for testing purposes

                        Console.WriteLine("Training datasets: " + Doc.FileCount(pathToDir));

                        files = Directory.GetFiles(pathToDir);

                        foreach (string file in files)
                        {
                            //Building government dictionary used to keep track of number of datasets for each government
                            if (governmentDict.ContainsKey(Doc.DocGovernment(file))) { governmentDict[Doc.DocGovernment(file)]++; }
                            else { governmentDict.Add(Doc.DocGovernment(file), 1); }
                        }

                        foreach (string file in files)
                        {
                            string government = Doc.DocGovernment(file); //Government of file
                            priorProbability = Calculations.PriorProbabilities(government, Doc.FileCount(pathToDir), governmentDict);

                            // key-value pair word frequency
                            wordCount = Calculations.WordFrequency(file, dict, stopWordsFile);

                            switch (government)
                            {
                                case nameof(Doc.Government.Conservative):
                                    conTotal += wordCount; // Total number of words in each category including repeats
                                    conDict = conDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    conPriorProbability = priorProbability;
                                    break;
                                case nameof(Doc.Government.Coalition):
                                    coaTotal += wordCount;
                                    coaDict = coaDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    coaPriorProbability = priorProbability;
                                    break;
                                case nameof(Doc.Government.Labour):
                                    labTotal += wordCount;
                                    labDict = labDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    labPriorProbability = priorProbability;
                                    break;
                                default:
                                    Console.WriteLine("Could not determine government, data will be discarded");
                                    Console.ReadLine();
                                    break;
                            }
                            //uniqueDict = uniqueDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());

                        }

                        ////////////////////////////////// KEEP ABOVE

                        /*
                        nWords = uniqueDict.Count(); //Total number of unique words throughout training documents

                        //After talk in class realised need to keep words that don't appear in a category and set value to 0
                        //To get cat[word] = 0 for words not in a category need to comapre uniqueDict to (category)Dict
                        foreach (KeyValuePair<string, int> word in uniqueDict)
                        {
                            //Expert system technique, for each word all conditions are triggered but only fired if false
                            if (!coaDict.ContainsKey(word.Key)) { coaDict.Add(word.Key, 0); }
                            if (!labDict.ContainsKey(word.Key)) { labDict.Add(word.Key, 0); }
                            if (!conDict.ContainsKey(word.Key)) { conDict.Add(word.Key, 0); }
                        }

                        //First work out term frequency in category - have a dictionary of word and frequency
                        //only need dict of test to get the words

                        foreach (KeyValuePair<string, int> fcat in conDict) //fcat refers to frequency of word in given category
                        {
                            double cp = Calculations.ConditionalProbability(fcat.Value, conTotal, nWords);
                            concpdict.Add(fcat.Key, cp); // Building conditional probability dictionary
                        }
                        foreach (KeyValuePair<string, int> fcat in coaDict)
                        {
                            double cp = Calculations.ConditionalProbability(fcat.Value, coaTotal, nWords);
                            coacpdict.Add(fcat.Key, cp);
                        }
                        foreach (KeyValuePair<string, int> fcat in labDict)
                        {
                            double cp = Calculations.ConditionalProbability(fcat.Value, labTotal, nWords);
                            labcpdict.Add(fcat.Key, cp);
                        }
                        */
                        ///////////////////////////////////////////////////

                        //pathToTest = Doc.FileExists(PathToTestDocument(), "test document");
                        pathToTest = "test_dataset/test1.txt"; //for testing purposes
                        Calculations.WordFrequency(pathToTest, fileDict, stopWordsFile);

                        //List<double> labtfidf = new List<double>();

                        Dictionary<string, double> labTFIDF = new Dictionary<string, double>();

                        foreach (var word in fileDict)
                        {
                            List<double> labWordTFIDF = new List<double>();

                            foreach (string file in files)
                            {
                                int wordExistInFileCount = 0;

                                if (Doc.DocGovernment(file) == Doc.Government.Labour.ToString())
                                {
                                    Dictionary<string, int> tempDict = new Dictionary<string, int>();
                                    wordCount = Calculations.WordFrequency(file, tempDict, stopWordsFile);

                                    //foreach (var word in fileDict)
                                    //{

                                    // Term frequency = count how many times the term appears in this document.

                                    tempDict.TryGetValue(word.Key, out int frequency);

                                    double tf = frequency / (double)wordCount; //to get floating point arithmetic atleast one variable must be a double

                                        foreach (string doc in files)
                                        {
                                            if (Doc.DocGovernment(doc) == Doc.Government.Labour.ToString())
                                            {
                                                Dictionary<string, int> tempDict2 = new Dictionary<string, int>();
                                                Calculations.WordFrequency(doc, tempDict2, stopWordsFile);
                                                if (tempDict2.ContainsKey(word.Key)) { wordExistInFileCount++; }
                                            }

                                        }
                                    // Calculate the IDF for each word
                                    double idf = 0D;
                                    //For unseen words must use smoothed inverse document frequency techniques as cannot divide a number by 0 (add 1 to wordExistInFileCount)
                                    //idf = Math.Log(governmentDict["Labour"] / ((double)1 + wordExistInFileCount));  //if a word appears in all docs of a category then idf will be 1 (lower bound for IDF) as that word is not considered special

                                    //Term inverse document requency - number of documents in a category that word appears in
                                    // IDF is log(number of doc in category/no of doc with that term)
                                    if (wordExistInFileCount == 0) { idf = 1; } //For unseen words must use smoothed inverse document frequency techniques as cannot divide a number by 0
                                    else { idf = 1 + Math.Log(governmentDict["Labour"] / (double)wordExistInFileCount); } //if a word appears in all docs of a category then idf will be 1 (lower bound for IDF) as that word is not considered special

                                    // Calculate TFIDF
                                    double tfidf = tf * idf;
                                    labWordTFIDF.Add(tfidf);// repeat procedure for other appearances of word in other docs in category, will add together later 

                                    //labTFIDF[word.Key] = tf * idf; 
                                    //Thus tf-idf of word is:
                                    //double tfidf = tf * idf;
                                    //labtfidf.Add(tfidf);
                                    //}
                                }

                            }
                            labTFIDF[word.Key] = labWordTFIDF.Sum();
                            Console.WriteLine(word.Key + ": " + labTFIDF[word.Key]); //this is a slow process so printing out idf to give user feedback
                        }


                        List<double> catTFIDF = new List<double>();


                        foreach (var word in labDict)
                        {
                            List<double> labWordTFIDF = new List<double>();

                            foreach (string file in files)
                            {
                                int wordExistInFileCount = 0;

                                if (Doc.DocGovernment(file) == Doc.Government.Labour.ToString())
                                {
                                    Dictionary<string, int> tempDict = new Dictionary<string, int>();
                                    wordCount = Calculations.WordFrequency(file, tempDict, stopWordsFile);

                                    tempDict.TryGetValue(word.Key, out int frequency); //if not found frequency is 0

                                    double tf = frequency / (double)wordCount; //to get floating point arithmetic atleast one variable must be a double

                                    foreach (string doc in files)
                                    {
                                        if (Doc.DocGovernment(doc) == Doc.Government.Labour.ToString())
                                        {
                                            Dictionary<string, int> tempDict2 = new Dictionary<string, int>();
                                            Calculations.WordFrequency(doc, tempDict2, stopWordsFile);
                                            if (tempDict2.ContainsKey(word.Key)) { wordExistInFileCount++; }
                                        }

                                    }
                                    // Calculate the IDF for each word
                                    double idf = 0D;

                                    //Term inverse document requency - number of documents in a category that word appears in
                                    if (wordExistInFileCount == 0) { idf = 1; } 
                                    else { idf = 1 + Math.Log(governmentDict["Labour"] / (double)wordExistInFileCount); } 

                                    // Calculate TFIDF
                                    double tfidf = tf * idf;
                                    labWordTFIDF.Add(tfidf); 

                                    //labTFIDF[word.Key] = tf * idf; 
                                    //Thus tf-idf of word is:
                                    //double tfidf = tf * idf;
                                    //labtfidf.Add(tfidf);
                                    //}
                                }

                            }
                            catTFIDF.Add(labWordTFIDF.Sum()); //list containing tf-idf weights of all words in category
                            Console.WriteLine(word.Key + ": " + labWordTFIDF.Sum()); //this is a slow process so printing out idf to give user feedback
                        }

                        double nCat = catTFIDF.Sum();
                        Dictionary<string, double> catCpTFIDF = new Dictionary<string, double>();

                        foreach (var word in labTFIDF)
                        {
                            //tf-idf uses same conditional probability formula
                            double cp = Calculations.ConditionalProbability(word.Value, nCat, catTFIDF.Count());
                            catCpTFIDF.Add(word.Key, Math.Log(cp));
                        }

                        double prob = catCpTFIDF.Sum(x => x.Value);

                            /*
                            if (Doc.DocGovernment(file) == Doc.Government.Coalition.ToString())
                            {
                                foreach (var word in fileDict)
                                {

                                }
                            }

                            if (Doc.DocGovernment(file) == Doc.Government.Conservative.ToString())
                            {
                                foreach (var word in fileDict)
                                {

                                }
                            }
                        }
                        */

                        //Term inverse document requency - number of documents in a category that word appears in
                        // log(number of doc in category/no of doc with that term)


                        break;

                    case 4:
                        Title();
                        Console.WriteLine("All files needed for this program can be found in bin/debug. Therefore to use these files just " +
                                          "enter file path after bin/debug/. For example to use test1.txt just enter \"test_dataset/test1.txt\". " +
                                          "To use your own files such as stopwords or bayesian network must enter full path to file when prompted");
                        Console.ReadLine();
                        break;

                    case 0:
                        Console.WriteLine("\nExiting program... ");
                        answer = exit;
                        break;

                    default:
                        Console.WriteLine("Invalid number, press [ENTER] to restart ");
                        Console.ReadLine();
                        break;
                        
                }

            } while (answer != exit);
        }

        static int DisplayMenu()
        {
            bool validInput;

            Title();
            Console.WriteLine("Select from the following options:");
            Console.WriteLine("(1) Undertake Training");
            Console.WriteLine("(2) Undertake a Classification using word frequency");
            Console.WriteLine("(3) Undertake a Classification using TF-IDF");
            Console.WriteLine("(4) ReadMe");
            Console.WriteLine("(0) Quit");

            string userInput = Console.ReadLine();
            validInput = Int32.TryParse(userInput, out int result);

            while (!validInput)
            { 
                Console.WriteLine("Invalid Input, Enter either (0),(1),(2),(3),(4)"); 
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

        private string PathToStopWords()
        {
            return AskForInfoString("Please enter full path to stop words file");
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

        int AskForInfoInt(string message)
        {
            bool validInput;
            Console.Write(message);
            string userInput = Console.ReadLine();

            validInput = Int32.TryParse(userInput, out int result);
            while (!validInput)
            {
                Console.Write(message);
                userInput = Console.ReadLine();
                validInput = Int32.TryParse(userInput, out result);
            }
            return result;
        }
    }
}
