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
            double priorProbability = governmentDict[government] / (double)fileCount;
            return priorProbability;
        }

        public static double ConditionalProbability(int fcat, int ncat, int nWords)
        {
            //Console.WriteLine("{0},{1},{2}",fcat,ncat,nWords);
            double top = fcat + 1;
            double bottom = ncat + nWords;
            //double conditionalProbability = Math.Log10(top / bottom); //Taking log of probability to avoid floating-point overflow errors
            double conditionalProbability = top / bottom;
            //Console.WriteLine(conditionalProbability);
            return conditionalProbability;
        }

        public static void Classification(Dictionary<string, int> testDict, Dictionary<string, double> concpdict,
                                         Dictionary<string, double> coacpdict, Dictionary<string, double> labcpdict,
                                         double conPriorProbability, double coaPriorProbability, double labPriorProbability)
        {

            double conProb = 0D, coaProb = 0D, labProb = 0D;

            foreach (var word in testDict.Keys)
            {
                if (concpdict.ContainsKey(word)) { conProb = conProb + (concpdict[word] * testDict[word]); }
                if (coacpdict.ContainsKey(word)) { coaProb = coaProb + (coacpdict[word] * testDict[word]); }
                if (labcpdict.ContainsKey(word)) { labProb = conProb + (labcpdict[word] * testDict[word]); }
            }

            //conProb = Math.Pow(10, conProb) * conPriorProbability; //inverse of log, c# doesn't have power (^) operator, 
            conProb = conProb * conPriorProbability;
            coaProb = coaProb * coaPriorProbability;
            labProb = labProb * labPriorProbability;


            var predDict = new Dictionary<object, double>
                        {
                            {Doc.Government.Labour, labProb },
                            {Doc.Government.Conservative, conProb},
                            {Doc.Government.Coalition, coaProb}
                        };

            Console.Clear();
            foreach (KeyValuePair<object, double> pred in predDict)
            {
                Console.WriteLine("Probability of {0}: {1:00.00}%", pred.Key, pred.Value * 100); //string formating to display percentage to 2dp
            }

            var best = predDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key; //selects key with highest value by comparing
            Console.WriteLine("---------------------");
            Console.WriteLine("This document is predicted to be " + best + "\n");
        }

        //Count the frequency of each unique word.  
        public static int WordFrequency(string file, Dictionary<string, int> words, string stopWordsFile)
        {

            //string stopWordsFile = "/Users/David/Coding/ai-assignment/AI-Assignment/stopwords.txt"; //Stop Words look up table
            StreamReader sr = new StreamReader(stopWordsFile);
            string stopWordsText = File.ReadAllText(stopWordsFile);
            //Console.ReadLine();

            var stopWords = stopWordsText.Split();

            var document = File.ReadAllText(file).ToLower(); //Change file to lower case

            foreach (var word in stopWords) { document = Regex.Replace(document, "\\b" + word + "\\b", ""); }

            int wordCount = document.Split(' ').Length; //Total number of words in each doc including repeats. This method of counting words takes a considerable amount of time

            //stemming document
            var regex = new Regex(@"\b[\s,\.\-:;\(\)]*"); //ignore punctuation
            foreach (string word in regex.Split(document).Where(x => !string.IsNullOrEmpty(x)))
            {
                document = Regex.Replace(document, word, Doc.Stemming(word));
            }
            //foreach (string word in document.Split(' ')) { document = Regex.Replace(document, word, Stemming(word)); }

            var wordPattern = new Regex(@"\w+"); // \w+ matches any word character plus 1, this should account for apostrophes as I think these can affect algorithm performance

            foreach (Match match in wordPattern.Matches(document))
            {
                words.TryGetValue(match.Value, out int currentCount);
                currentCount++;
                words[match.Value] = currentCount;
            }

            return wordCount;
        }
    }
}
