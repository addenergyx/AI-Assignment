﻿using System;
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
            double conLogProb = 0D, coaLogProb = 0D, labLogProb = 0D;

            //foreach (var ppp in testDict) { Console.WriteLine(ppp.Value); }

            //Real number probabilities
            foreach (var word in testDict.Keys)
            {
                if (concpdict.ContainsKey(word)) { conProb = conProb * (Math.Pow(concpdict[word], testDict[word])); } //conditional probability of a word to the power of word frequency in the test document
                if (coacpdict.ContainsKey(word)) { coaProb = coaProb * (Math.Pow(coacpdict[word], testDict[word])); }
                if (labcpdict.ContainsKey(word)) { labProb = conProb * (Math.Pow(labcpdict[word], testDict[word])); }
                //if (labcpdict.ContainsKey(word)) { labProb = conProb + (labcpdict[word] * testDict[word]); } - [wrong] conditional probability of a word times by word frequency in the test document

            }
            //conProb = Math.Pow(10, conProb) * conPriorProbability; //inverse of log, c# doesn't have power (^) operator, 
            conProb = conProb * conPriorProbability;
            coaProb = coaProb * coaPriorProbability;
            labProb = labProb * labPriorProbability;

            //Taking log of probability to avoid floating-point overflow errors
            foreach (var word in testDict.Keys)
            {
                if (concpdict.ContainsKey(word)) { conLogProb = conLogProb * (Math.Log(Math.Pow(concpdict[word], testDict[word]))); }
                if (coacpdict.ContainsKey(word)) { coaLogProb = coaLogProb * (Math.Log(Math.Pow(coacpdict[word], testDict[word]))); }
                if (labcpdict.ContainsKey(word)) { labLogProb = conLogProb * (Math.Log(Math.Pow(labcpdict[word], testDict[word]))); }
            }

            //Addition of logs is the same as multiplication of real numbers
            conLogProb = conLogProb + Math.Log(conPriorProbability); //The logarithm of a positive number may be negative or zero. log of a decimal will probably give a negative number
            coaLogProb = coaLogProb + Math.Log(coaPriorProbability);
            labLogProb = labLogProb + Math.Log(labPriorProbability);

            var predDict = new Dictionary<object, double>
                        {
                            {Doc.Government.Labour, labProb },
                            {Doc.Government.Conservative, conProb},
                            {Doc.Government.Coalition, coaProb}
                        };

            var predLogDict = new Dictionary<object, double>
                        {
                            {Doc.Government.Labour, labLogProb },
                            {Doc.Government.Conservative, conLogProb},
                            {Doc.Government.Coalition, coaLogProb}
                        };

            Menu.Title();
            foreach (KeyValuePair<object, double> pred in predDict)
            {
                Console.WriteLine("Probability of {0}: {1:00.00}%", pred.Key, pred.Value * 100); //string formating to display percentage to 2dp
            }

            var best = predDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key; //selects key with highest value by comparing
            Console.WriteLine("---------------------");
            Console.WriteLine("This document is predicted to be " + best + "\n");

            //Log results
            foreach (KeyValuePair<object, double> pred in predLogDict)
            {
                Console.WriteLine("Log Probability of {0}: {1}", pred.Key, pred.Value);
            }

            var logBest = predLogDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            Console.WriteLine("---------------------");
            Console.WriteLine("This document is predicted to be " + logBest + "\n");
        }

        //Count the frequency of each unique word.  
        public static int WordFrequency(string file, Dictionary<string, int> words, string stopWordsFile)
        {

            //string stopWordsFile = "/Users/David/Coding/ai-assignment/AI-Assignment/stopwords.txt"; //Stop Words look up table
            StreamReader sr = new StreamReader(stopWordsFile);
            string stopWordsText = File.ReadAllText(stopWordsFile);

            var stopWords = stopWordsText.Split();

            var document = File.ReadAllText(file).ToLower(); //Change file to lower case

            foreach (var word in stopWords) { document = Regex.Replace(document, "\\b" + word + "\\b", ""); }

            //removing stopwords messes with the whitespacing of the text
            document = Regex.Replace(document, @"\s+", " "); //replaces multiple spaces with one, needed for word families

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

            //Using n-grams - word families
            List<string> wordFamilies = new List<string>();

            //https://stackoverflow.com/questions/6695327/need-c-sharp-regex-to-get-pairs-of-words-in-a-sentence
            Regex wordPairRegex = new Regex(
    @"(     # Match and capture in backreference no. 1:
     \w+    # one or more alphanumeric characters
     \s+    # one or more whitespace characters.
    )       # End of capturing group 1.
    (?=     # Assert that there follows...
     (\w+)  # another word; capture that into backref 2.
    )       # End of lookahead.",
    RegexOptions.IgnorePatternWhitespace);
            Match matchResult = wordPairRegex.Match(document);
            while (matchResult.Success)
            {
                wordFamilies.Add(matchResult.Groups[1].Value + matchResult.Groups[2].Value);
                matchResult = matchResult.NextMatch();
            }

            //Cleaner way to count frequency compared to using if/else statment 
            //var groups = wordFamilies.GroupBy(s => s).Select(
                //s => new { wordFamilies = s.Key, Count = s.Count() });

            //var dd = groups.ToDictionary(g => g.wordFamilies, g => g.Count);

            //foreach (var paii in dict) {Console.WriteLine( paii.Key + ": " + paii.Value); }

            //foreach (var ele in wordFamilies) { Console.WriteLine(ele); }

            //foreach (var pair in dd) { words.Add(pair.Key, pair.Value) ; } //Adding word families to word frequency

            foreach (string pair in wordFamilies)
            {
                if (words.ContainsKey(pair)) { words[pair]++; }
                else { words.Add(pair, 1); }
            }

            return wordCount;
        }
    }
}
