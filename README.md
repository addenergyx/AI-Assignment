# AI-Assignment

This assignment requires you to develop a Party Affiliation Classifier that analyses the text of a Queen’s Speech and hence predicts the political affinity of the current government at the time the speech was delivered (Labour, Conservative, or LibDem/Con Coalition). There is more than one way to do this, but for this assignment you are required to implement a Naïve Bayes approach (of which there are numerous variations, the specific variation here being a Multinomial Naïve Bayes classifier). Note that on reading any of the speeches you will see that the name of the political party (or parties, if in coalition) are never mentioned, rather the party in power is always referred to as ‘the government’. Whilst there is no practical need for such a Queen’s Speech ‘party affiliation classifier’ the techniques are very relevant to other text-based requirements and is a highly topical AI subject in the field of data analytics and recommender systems. On successful completion you will have a good knowledge of a common machine learning technique applied to a specific example that can be extrapolated to many other scenarios. 
 
In the UK, ‘The Queen’s Speech’ is delivered by the Queen normally once a year in the House of Lords (to which members of the House of Commons and others are invited) to mark the annual state opening of parliament (usually in November but can be at other times, such as immediately after a change of government). It is written by the current government in power and is a kind of ‘mission statement’ by the government for the year ahead. It will typically contain proposals (whether or not achieved) for draft legislation, foreign policy objectives, and perhaps details of upcoming visits to the UK by other heads of state and any state visits to be made overseas. The length of the speech is normally around 10 minutes. There is much pomp and pageantry around the event, after which the contents of the speech are debated by MP’s in the House of Commons. 
 
Eight Queen’s Speech text files are available for use for this assignment and available on Canvas; five are a training set and three are unknowns for testing purposes. This data was obtained by copying and pasting from publically available archives, in particular>  https://www.parliament.uk/about/faqs/house-of-lords-faqs/lords-stateopening/  The only pre-processing that has occurred is the removal of non-verbal content and speech marks. Although probably not required, you may if you wish extend the dataset or change the training and/or test data (a little internet searching will find plenty of on-line archive links similar to the above). 
 
 
Program requirements Given that all students should have a background in C# console programming in fairness to all, your implementation must conform to the following two requirements: 
1. Language must be C# 
2. Application must be console based (a GUI interface would be an unnecessary distraction to the AI aspect of this work). 
 
 

 Program functionality Your program should do the following: 
 
1. On start up the program should prompt the user to either a) undertake training or b) undertake a classification. If training is chosen, the user must specify the relevant input source code file(s). After training the program should optionally write the Bayesian network to file and default to the classification option. If classification is chosen, the user must either use the current network created by the prior training phase or read a pre-trained network from file. The user then enters the filename of the document to be classified, and a result is displayed back to the user. 
2. Appropriate statistics concerning training and classification should also be presented. 
