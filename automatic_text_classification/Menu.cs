using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics; //debugging

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
                //dict is a temporary dictionary where calculations are made before being moved to a category dictionary
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
                            var a = new Dictionary<string, int>(); //word frequency temp dict
                            var b = new Dictionary<string, double>(); //conditional probability temp dict

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

                        /*
                        foreach (string file in files)
                        {
                            //Building government dictionary used to keep track of number of datasets for each government
                            if (governmentDict.ContainsKey(Doc.DocGovernment(file))) { governmentDict[Doc.DocGovernment(file)]++; }
                            else { governmentDict.Add(Doc.DocGovernment(file), 1); }
                        }
                        */

                        foreach (string file in files)
                        {
                            string government = Doc.DocGovernment(file); //Government of file
                            //priorProbability = Calculations.PriorProbabilities(government, Doc.FileCount(pathToDir), governmentDict);

                            // key-value pair word frequency
                            wordCount = Calculations.WordFrequency(file, dict, stopWordsFile);

                            switch (government)
                            {
                                case nameof(Doc.Government.Conservative):
                                    conTotal += wordCount; // Total number of words in each category including repeats
                                    conDict = conDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    //conPriorProbability = priorProbability;
                                    break;
                                case nameof(Doc.Government.Coalition):
                                    coaTotal += wordCount;
                                    coaDict = coaDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    //coaPriorProbability = priorProbability;
                                    break;
                                case nameof(Doc.Government.Labour):
                                    labTotal += wordCount;
                                    labDict = labDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());
                                    //labPriorProbability = priorProbability;
                                    break;
                                default:
                                    Console.WriteLine("Could not determine government, data will be discarded");
                                    Console.ReadLine();
                                    break;
                            }
                            //uniqueDict = uniqueDict.Union(dict).GroupBy(i => i.Key, i => i.Value).ToDictionary(i => i.Key, i => i.Sum());

                        }

                        ////////////////////////////////// 

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
                        Calculations.WordFrequency(pathToTest, fileDict, stopWordsFile); // dictionary of term frequency of test doc


                        //List<double> labtfidf = new List<double>();

                        Dictionary<string, double> labTFIDF = new Dictionary<string, double>();
                        Dictionary<string, double> conTFIDF = new Dictionary<string, double>();
                        Dictionary<string, double> coaTFIDF = new Dictionary<string, double>();

                        Stopwatch stopwatch = new Stopwatch(); //MSDN documentation advises against using DateTime for benchmarking
                        stopwatch.Start();

                        ///////////////////////// new method - considerably faster than old method

                        //can't use a dictionary for arrayOfFileDictionaries and governmentDirectoryPosition because some dictionaries will have same government
                        var arrayOfFileDictionaries = new[]
                        {
                            new Dictionary<string, int>(),
                            new Dictionary<string, int>(),
                            new Dictionary<string, int>(),
                            new Dictionary<string, int>(),
                            new Dictionary<string, int>()

                        };

                        string [] governmentDirectoryPosition = new string[5];

                        for (var i = 0; i < files.Length; i++) //get word frequency of files at the start so no need to get them several times later using WordFrequency()
                        {
                            var temp = new Dictionary<string, int>();
                            governmentDirectoryPosition[i] = Doc.DocGovernment(files[i]);
                            Calculations.WordFrequency(files[i], temp, stopWordsFile);
                            arrayOfFileDictionaries[i] = temp;
                        }

                        //part a
                        foreach (var word in fileDict)
                        {
                            List<double> labWordTFIDF = new List<double>();
                            List<double> conWordTFIDF = new List<double>();
                            List<double> coaWordTFIDF = new List<double>();

                            for (var i = 0; i < governmentDirectoryPosition.Length; i++)
                            {
                                if (governmentDirectoryPosition[i] == Doc.Government.Labour.ToString())
                                {
                                    double tf = Calculations.TF(word, arrayOfFileDictionaries[i]);
                                    double idf = Calculations.IDF(word, governmentDirectoryPosition[i], governmentDirectoryPosition, arrayOfFileDictionaries); //Need government variable in IDF because it looks at the whole dataset unlike tf that just looks at the document
                                    labWordTFIDF.Add(Calculations.TFIDF(tf, idf));// repeat procedure for other appearances of word in other docs in category, will add together later 
                                }

                                if (governmentDirectoryPosition[i] == Doc.Government.Conservative.ToString())
                                {
                                    double tf = Calculations.TF(word, arrayOfFileDictionaries[i]);
                                    double idf = Calculations.IDF(word, governmentDirectoryPosition[i], governmentDirectoryPosition, arrayOfFileDictionaries);
                                    conWordTFIDF.Add(Calculations.TFIDF(tf, idf));
                                }

                                if (governmentDirectoryPosition[i] == Doc.Government.Coalition.ToString())
                                {
                                    double tf = Calculations.TF(word, arrayOfFileDictionaries[i]);
                                    double idf = Calculations.IDF(word, governmentDirectoryPosition[i], governmentDirectoryPosition, arrayOfFileDictionaries);
                                    //double idf = Calculations.IDF(files, stopWordsFile, word, governmentDirectoryPosition[i]); old
                                    coaWordTFIDF.Add(Calculations.TFIDF(tf, idf));
                                }
                            }

                            labTFIDF[word.Key] = labWordTFIDF.Sum(); //dict of tfidf value for category
                            conTFIDF[word.Key] = conWordTFIDF.Sum();
                            coaTFIDF[word.Key] = coaWordTFIDF.Sum();

                            Console.WriteLine("Labour TFIDF of " + word.Key + ": " + labTFIDF[word.Key]); //this is a slow process so printing out idf to give user feedback
                            Console.WriteLine("Conservative TFIDF of " + word.Key + ": " + conTFIDF[word.Key]); //this is a slow process so printing out idf to give user feedback
                            Console.WriteLine("Coalition TFIDF of " + word.Key + ": " + coaTFIDF[word.Key]); //this is a slow process so printing out idf to give user feedback
                            Console.WriteLine();
                        }


                        var probDict = new Dictionary<string, double>();

                        //part b
                        foreach (Doc.Government party in Enum.GetValues(typeof(Doc.Government)))
                        {
                            //var tempDict = new Dictionary<string, int>();
                            //var tempDict2 = new Dictionary<string, double>();
                            double prob = 0D;

                            if (party.ToString() == Doc.Government.Coalition.ToString()) { prob = Calculations.SumOfTFIDFInCategory(coaDict, governmentDirectoryPosition, coaTFIDF, party.ToString(), arrayOfFileDictionaries); }
                            else if (party.ToString() == Doc.Government.Conservative.ToString()) { prob = Calculations.SumOfTFIDFInCategory(conDict, governmentDirectoryPosition, conTFIDF, party.ToString(), arrayOfFileDictionaries); }
                            else if (party.ToString() == Doc.Government.Labour.ToString()) { prob = Calculations.SumOfTFIDFInCategory(labDict, governmentDirectoryPosition, labTFIDF, party.ToString(), arrayOfFileDictionaries); }

                            //double cp = Calculations.ConditionalProbability(word.Value, catTFIDF.Sum(), catTFIDF.Count());
                            //double prob = Calculations.ProbabilityTFIDF(tempDict, files, stopWordsFile, tempDict2, party.ToString());

                            probDict.Add(party.ToString(), prob);
                        }

                        stopwatch.Stop();
                        string benchmark = stopwatch.ElapsedMilliseconds.ToString();
                        foreach (var pred in probDict) { Console.WriteLine(pred.Key + ": " + pred.Value); }
                        Console.WriteLine("Time taken to process new method: " + Int32.Parse(benchmark) / 60000);
                        Console.ReadLine();


                        ////////////////////////////// old method
                        /*foreach (var word in fileDict)
                        {
                            List<double> labWordTFIDF = new List<double>();
                            List<double> conWordTFIDF = new List<double>();
                            List<double> coaWordTFIDF = new List<double>();

                            foreach (string file in files)
                            {
                                string government = "";
                                Dictionary<string, int> temp = new Dictionary<string, int>();
                                Calculations.WordFrequency(file, temp, stopWordsFile); // by having this here can remove function from TF and IDF, this could save time

                                if (Doc.DocGovernment(file) == Doc.Government.Labour.ToString())
                                {
                                    government = Doc.Government.Labour.ToString();
                                    double tf = Calculations.TF(word, temp);
                                    double idf = Calculations.IDF(files, stopWordsFile, word, government); //Need government variable in IDF because it looks at the whole dataset unlike tf that just looks at the document
                                    labWordTFIDF.Add(Calculations.TFIDF(tf, idf));// repeat procedure for other appearances of word in other docs in category, will add together later 
                                }

                                if (Doc.DocGovernment(file) == Doc.Government.Conservative.ToString()) 
                                {
                                    government = Doc.Government.Conservative.ToString();
                                    double tf = Calculations.TF(word, temp);
                                    double idf = Calculations.IDF(files, stopWordsFile, word, government);
                                    conWordTFIDF.Add(Calculations.TFIDF(tf, idf));
                                }

                                if (Doc.DocGovernment(file) == Doc.Government.Coalition.ToString()) 
                                {
                                    government = Doc.Government.Conservative.ToString();
                                    double tf = Calculations.TF(word, temp);
                                    double idf = Calculations.IDF(files, stopWordsFile, word, government);
                                    coaWordTFIDF.Add(Calculations.TFIDF(tf, idf));
                                }

                            }
                            labTFIDF[word.Key] = labWordTFIDF.Sum();
                            conTFIDF[word.Key] = conWordTFIDF.Sum();
                            coaTFIDF[word.Key] = coaWordTFIDF.Sum();

                            Console.WriteLine("Labour TFIDF of " + word.Key + ": " + labTFIDF[word.Key]); //this is a slow process so printing out idf to give user feedback
                            Console.WriteLine("Conservative TFIDF of " + word.Key + ": " + conTFIDF[word.Key]); //this is a slow process so printing out idf to give user feedback
                            Console.WriteLine("Coalition TFIDF of " + word.Key + ": " + coaTFIDF[word.Key]); //this is a slow process so printing out idf to give user feedback
                            Console.WriteLine();

                            //Console.WriteLine(word.Key + ": " + labTFIDF[word.Key]); //this is a slow process so printing out idf to give user feedback
                        }

                        var probDict2 = new Dictionary<string, double>();

                        foreach (Doc.Government party in Enum.GetValues(typeof(Doc.Government)))
                        {
                            var tempDict = new Dictionary<string, int>();
                            var tempDict2 = new Dictionary<string, double>();

                            if (party.ToString() == Doc.Government.Coalition.ToString()) { tempDict = coaDict;  tempDict2 = coaTFIDF; }
                            else if (party.ToString() == Doc.Government.Conservative.ToString()) {  tempDict = conDict;  tempDict2 = conTFIDF; }
                            else if (party.ToString() == Doc.Government.Labour.ToString()) {  tempDict = labDict;  tempDict2 = labTFIDF; }

                            //double cp = Calculations.ConditionalProbability(word.Value, catTFIDF.Sum(), catTFIDF.Count());
                            double prob = Calculations.ProbabilityTFIDF(tempDict, files, stopWordsFile, tempDict2, party.ToString());

                            probDict2.Add(party.ToString(), prob);
                        }

                        stopwatch.Stop();
                        string benchmark2 = stopwatch.ElapsedMilliseconds.ToString();
                        foreach (var pred in probDict2) { Console.WriteLine(pred.Key + ": " + pred.Value); }
                        Console.WriteLine( "Time taken to process: " + Int32.Parse(benchmark2) / 60000 );
                        Console.ReadLine();*/

                        /*
                        List<double> catTFIDF = new List<double>();

                        foreach (var word in labDict)
                        {
                            List<double> labWordTFIDF = new List<double>();

                            foreach (string file in files)
                            {
                                double tf = Calculations.TF(file, stopWordsFile, word);
                                double idf = Calculations.IDF(files, stopWordsFile, word, "Labour");
                                labWordTFIDF.Add(Calculations.TFIDF(tf, idf));
                                /*
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
                        /*
                        double nCat = catTFIDF.Sum();
                        Dictionary<string, double> labCpTFIDF = new Dictionary<string, double>();

                        foreach (var word in labTFIDF)
                        {
                            //tf-idf uses same conditional probability formula
                            double cp = Calculations.ConditionalProbability(word.Value, catTFIDF.Sum(), catTFIDF.Count());
                            labCpTFIDF.Add(word.Key, Math.Log(cp));
                        }

                        double labprob = labCpTFIDF.Sum(x => x.Value);
                        */

                        break;

                    case 4:
                        Title();
                        Console.WriteLine("All files needed for this program can be found in bin/debug. Therefore to use these files just " +
                                          "enter file path after bin/debug/. For example to use test1.txt just enter \"test_dataset/test1.txt\" " +
                                          "or for stopwords file enter \"stopwords.txt\". To use your own files such as stopwords or bayesian network " +
                                          "must enter full path to file when prompted");
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
