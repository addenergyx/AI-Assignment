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

        public static double ConditionalProbability(double fCat, double nCat, int nWords)
        {
            //Console.WriteLine("{0},{1},{2}",fcat,ncat,nWords);
            double top = fCat + 1;
            double bottom = nCat + nWords;
            //double conditionalProbability = Math.Log10(top / bottom); //Taking log of probability to avoid floating-point overflow errors
            double conditionalProbability = top / bottom;
            //Console.WriteLine(conditionalProbability);
            return conditionalProbability;
        }

        public static double TF(string file, string stopWordsFile, KeyValuePair<string,int> word)
        {
            Dictionary<string, int> tempDict = new Dictionary<string, int>();
            int wordCount = WordFrequency(file, tempDict, stopWordsFile);

            tempDict.TryGetValue(word.Key, out int frequency);

            // Term frequency = count how many times the term appears in a document
            return frequency / (double)wordCount; //to get floating point arithmetic atleast one variable must be a double
        }

        public static double IDF(string[] files, string stopWordsFile, KeyValuePair<string, int> word, string government)
        {            
            // Calculates the IDF for each word
            int wordExistInFileCount = 0;
            int fileCount = 0;
            double idf = 0D;

            foreach (string doc in files)
            {
                if (Doc.DocGovernment(doc) == government)
                {
                    Dictionary<string, int> tempDict2 = new Dictionary<string, int>();
                    WordFrequency(doc, tempDict2, stopWordsFile);
                    if (tempDict2.ContainsKey(word.Key)) { wordExistInFileCount++; }
                    fileCount++;
                }

            }

            // Term inverse document frequency - number of documents in a category that word appears in
            // IDF is log(number of doc in category/no of doc with that term)
            if (wordExistInFileCount == 0) { idf = 1; } //For unseen words must use smoothed inverse document frequency techniques as cannot divide a number by 0 (add 1 to wordExistInFileCount)
            else { idf = 1 + Math.Log(fileCount / (double)wordExistInFileCount); } // 1+ to avoid the "divided by 0" error, if a word appears in all docs of a category then idf will be 1 (lower bound for IDF) as that word is not considered special

            return idf;
        }

        public static double TFIDF (double tf, double idf)
        {
            return tf * idf;
        }

        public static double ConditionProbabilityTFIDF(Dictionary<string, int> catDict, string [] files, string stopWordsFile, Dictionary<string, double> labTFIDF, string government)
        {
            List<double> catTFIDF = new List<double>();

            foreach (var word in catDict)
            {
                List<double> catWordTFIDF = new List<double>();

                foreach (string file in files)
                {
                    double tf = Calculations.TF(file, stopWordsFile, word);
                    double idf = Calculations.IDF(files, stopWordsFile, word, government);
                    catWordTFIDF.Add(Calculations.TFIDF(tf, idf));

                }
                catTFIDF.Add(catWordTFIDF.Sum()); //list containing tf-idf weights of all words in category
                Console.WriteLine(government + ": " + word.Key + ": " + catWordTFIDF.Sum()); //this is a slow process so printing out idf to give user feedback
            }

            double nCat = catTFIDF.Sum();
            Dictionary<string, double> catCpTFIDF = new Dictionary<string, double>();

            foreach (var word in labTFIDF)
            {
                //tf-idf uses same conditional probability formula
                double cp = Calculations.ConditionalProbability(word.Value, catTFIDF.Sum(), catTFIDF.Count());
                catCpTFIDF.Add(word.Key, Math.Log(cp));
            }

            double catprob = catCpTFIDF.Sum(x => x.Value);
            return catprob;
        }

        public static void Classification(Dictionary<string, int> testDict, Dictionary<string, double> concpdict,
                                         Dictionary<string, double> coacpdict, Dictionary<string, double> labcpdict,
                                         double conPriorProbability, double coaPriorProbability, double labPriorProbability)
        {

            double conProb = 1D, coaProb = 1D, labProb = 1D; 
            double conLogProb = 0D, coaLogProb = 0D, labLogProb = 0D;
            
            //Real number probabilities - Can't work due to float overflow
            foreach (var word in testDict.Keys)
            {
                if (concpdict.ContainsKey(word)) { conProb *= Math.Pow(concpdict[word], testDict[word]); } //conditional probability of a word to the power of word frequency in the test document
                if (coacpdict.ContainsKey(word)) { coaProb *= Math.Pow(coacpdict[word], testDict[word]); }
                if (labcpdict.ContainsKey(word)) { labProb *= Math.Pow(labcpdict[word], testDict[word]); }
                // [WRONG] if (labcpdict.ContainsKey(word)) { labProb = conProb + (labcpdict[word] * testDict[word]); } - conditional probability of a word times by word frequency in the test document

            }

            conProb *= conPriorProbability;
            coaProb *= coaPriorProbability;
            labProb *= labPriorProbability;

            //Taking log of probability to avoid floating-point overflow errors
            foreach (var word in testDict.Keys)
            {
                if (concpdict.ContainsKey(word)) { conLogProb += Math.Log(Math.Pow(concpdict[word], testDict[word])); } //Addition of logs is the same as multiplication of real numbers
                if (coacpdict.ContainsKey(word)) { coaLogProb += Math.Log(Math.Pow(coacpdict[word], testDict[word])); }
                if (labcpdict.ContainsKey(word)) { labLogProb += Math.Log(Math.Pow(labcpdict[word], testDict[word])); }
            }

            //conProb = Math.Pow(10, conLogProb) * conPriorProbability; //inverse of log, c# doesn't have power (^) operator, 
            //can't use inverse due to overflow so must keep in log form
            conLogProb += Math.Log(conPriorProbability); //The logarithm of a positive number may be negative or zero. log of a decimal will probably give a negative number
            coaLogProb += Math.Log(coaPriorProbability);
            labLogProb += Math.Log(labPriorProbability);

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

            var best = predDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key; 
            Console.WriteLine("---------------------");
            Console.WriteLine("This document is predicted to be " + best + "\n");

            //Log results
            foreach (KeyValuePair<object, double> pred in predLogDict)
            {
                Console.WriteLine("Log Probability of {0}: {1}", pred.Key, pred.Value);
            }

            var logBest = predLogDict.Aggregate((l, r) => l.Value > r.Value ? l : r).Key; //selects key with highest value by comparing
            Console.WriteLine("---------------------");
            Console.WriteLine("This document is predicted to be " + logBest + "\n");
        }

        //Count the frequency of each unique term
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

            //Lemmatizing document
            var regex = new Regex(@"\b[\s,\.\-:;\(\)]*"); //ignore punctuation
            foreach (string word in regex.Split(document).Where(x => !string.IsNullOrEmpty(x)))
            {
                document = Regex.Replace(document, word, Doc.Lemmatizing(word));
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
