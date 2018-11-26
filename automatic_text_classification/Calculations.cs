using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace automatic_text_classification
{
    public static class Calculations
    {
        public static double PriorProbabilities(string government, int fileCount, Dictionary<string, int> governmentDict)
        {
            return governmentDict[government] / (double)fileCount;
        }

        public static double ConditionalProbability(double fCat, double nCat, int nWords)
        {
            //Conditional probability P(word / cata) = (fcata[word]) + 1)  / (Ncata + Nwords)
            double top = fCat + 1;
            double bottom = nCat + nWords;
            double conditionalProbability = top / bottom;
            return conditionalProbability;
        }

        public static double TF(KeyValuePair<string,int> word, Dictionary<string, int> temp)
        {
            temp.TryGetValue(word.Key, out int frequency);
            double wordCount = temp.Sum(x => x.Value);
            // Term frequency = count how many times the term appears in a document / total number of terms in the document
            return frequency / wordCount; // To get floating point arithmetic atleast one variable must be a double
        }

        public static double IDF(KeyValuePair<string, int> word, string government, List<string> governmentDirectoryPosition, List<Dictionary<string, int>> listOfFileDictionaries)
        {
            // Calculates the IDF for each word
            int wordExistInFileCount = 0;
            int fileCount = 0;
            double idf = 0D;

            // Looping through predetermined wordfrequency, quicker then calling WordFrequency() many times
            for (int i = 0; i < governmentDirectoryPosition.Count; i++)
            {
                if (governmentDirectoryPosition[i] == government)
                {
                    if (listOfFileDictionaries[i].ContainsKey(word.Key)) { wordExistInFileCount++; } //Number of documents with term in it
                    fileCount++;
                }
            }

            // Term inverse document frequency is the number of documents in a category that term appears in
            // IDF is log(number of doc in category/no of doc with that term)
            idf = wordExistInFileCount == 0 ? 1 : 1 + Math.Log(fileCount / (double)wordExistInFileCount); // 1+ to avoid the "divided by 0" error, if a word appears in all docs of a category then idf will be 1 (lower bound for IDF) as that word is not considered special
            //https://stackoverflow.com/questions/16648599/tfidf-calculating-confusion

            return idf;
        }

        public static double TFIDF (double tf, double idf) //Algorithm from http://www.tfidf.com/ and https://monkeylearn.com/blog/practical-explanation-naive-bayes-classifier/
        {
            return tf * idf;
        }

        public static double SumOfTFIDFInCategory(Dictionary<string, int> categoryWordFrequency, List<string> governmentDirectoryPosition, Dictionary<string, double> categoryTFIDF, string government, List<Dictionary<string, int>> listOfFileDictionaries)
        {
            List<double> catTFIDF = new List<double>();

            foreach (var word in categoryWordFrequency)
            {
                List<double> tempCategoryWordTFIDF = new List<double>();


                for (int i = 0; i < listOfFileDictionaries.Count; i++)
                {
                    double tf = TF(word, listOfFileDictionaries[i]);
                    double idf = IDF(word, government, governmentDirectoryPosition, listOfFileDictionaries);
                    tempCategoryWordTFIDF.Add(TFIDF(tf, idf));
                }

                catTFIDF.Add(tempCategoryWordTFIDF.Sum()); //list containing tf-idf weights of all words in category
                Console.WriteLine(government + ": " + word.Key + ": " + tempCategoryWordTFIDF.Sum()); //this is a slow process so printing out idf to give user feedback
            }

            double nCat = catTFIDF.Sum();
            Dictionary<string, double> catCpTFIDF = new Dictionary<string, double>();

            foreach (var word in categoryTFIDF)
            {
                //tf-idf uses same conditional probability formula
                double cp = ConditionalProbability(word.Value, catTFIDF.Sum(), catTFIDF.Count());
                catCpTFIDF.Add(word.Key, Math.Log(cp));
            }

            return catCpTFIDF.Sum(x => x.Value);
        }

        public static void Classification(Dictionary<string, int> testDict, Dictionary<string, double> concpdict,
                                         Dictionary<string, double> coacpdict, Dictionary<string, double> labcpdict,
                                         double conPriorProbability, double coaPriorProbability, double labPriorProbability)
        {

            double conLogProb = 0D, coaLogProb = 0D, labLogProb = 0D;

            //Taking log of probability to avoid floating-point overflow errors
            foreach (var word in testDict.Keys)
            {
                //can't use inverse due to overflow so must keep in log form
                if (concpdict.ContainsKey(word)) { conLogProb += Math.Log(Math.Pow(concpdict[word], testDict[word])); } //Addition of logs is the same as multiplication of real numbers
                if (coacpdict.ContainsKey(word)) { coaLogProb += Math.Log(Math.Pow(coacpdict[word], testDict[word])); }
                if (labcpdict.ContainsKey(word)) { labLogProb += Math.Log(Math.Pow(labcpdict[word], testDict[word])); }
            }

            conLogProb += Math.Log(conPriorProbability); //The logarithm of a positive number may be negative or zero. log of a decimal will probably give a negative number
            coaLogProb += Math.Log(coaPriorProbability);
            labLogProb += Math.Log(labPriorProbability);

            var predLogDict = new Dictionary<string, double>
                        {
                            {Doc.Government.Labour.ToString(), labLogProb },
                            {Doc.Government.Conservative.ToString(), conLogProb},
                            {Doc.Government.Coalition.ToString(), coaLogProb}
                        };

            BestGovernment(predLogDict);

        }

        //Count the frequency of each unique term in a file
        public static int WordFrequency(string file, Dictionary<string, int> words, string stopWordsFile)
        {
            StreamReader sr = new StreamReader(stopWordsFile);

            string stopWordsText = File.ReadAllText(stopWordsFile); // Stop Words look up table

            sr.Close();

            var stopWords = stopWordsText.Split();

            var document = File.ReadAllText(file).ToLower(); // Change file to lower case

            foreach (var word in stopWords) { document = Regex.Replace(document, "\\b" + word + "\\b", ""); }

            // Removing stopwords messes with the whitespacing of the text
            document = Regex.Replace(document, @"\s+", " "); // Replaces multiple spaces with one, needed for word families
            
            //Lemmatizing document
            var regex = new Regex(@"\b[\s,\.\-:;\(\)]*"); //ignore punctuation
            foreach (string word in regex.Split(document).Where(x => !string.IsNullOrEmpty(x)))
            {
                document = Regex.Replace(document, word, Doc.Lemmatizing(word));
            }

            var wordPattern = new Regex(@"\w+"); // \w+ matches any word character plus 1, this should account for apostrophes as I think these can affect algorithm results

            foreach (Match match in wordPattern.Matches(document))
            {
                words.TryGetValue(match.Value, out int currentCount);
                currentCount++;
                words[match.Value] = currentCount;
            }

            int wordCount = words.Sum(x => x.Value); //Total number of words in each doc including repeats, more efficient then int wordCount = document.Split(' ').Length 

            //Using n-grams - word families
            List<string> wordFamilies = new List<string>();

            Regex wordPairRegex = new Regex(@"(\w+\s+)(?=(\w+))", RegexOptions.IgnorePatternWhitespace); //Regex for pairs of words in a text
            Match matchResult = wordPairRegex.Match(document);
            while (matchResult.Success)
            {
                wordFamilies.Add(matchResult.Groups[1].Value + matchResult.Groups[2].Value);
                matchResult = matchResult.NextMatch();
            }

            foreach (string pair in wordFamilies)
            {
                //Adding word families to word frequency dictionary
                if (words.ContainsKey(pair)) { words[pair]++; }
                else { words.Add(pair, 1); }
            }

            return wordCount;
        }

        public static void BestGovernment(Dictionary<string, double> probDict)
        {
            //Log results
            Menu.Title();
            foreach (KeyValuePair<string, double> pred in probDict) { Console.WriteLine("Log Probability of {0}: {1}", pred.Key, pred.Value); }
            var logBest = probDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key; //selects key with highest value by comparing
            Console.WriteLine("---------------------");
            Console.WriteLine("This document is predicted to be " + logBest + "\n");
            Menu.AnykeyToContinue();
        }
    }
}
