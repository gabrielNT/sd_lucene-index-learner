using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
namespace AutoComplete.Classes
{
    public static class LuceneProcessor
    {
        private static bool _isNormalizing;
        private static int typedLetterCount;
        private static int totalLetterCount;
        private static string textToBeAnalysed;
        private static string textToBeIndexed;
        private static LuceneSearch luceneSearch;
        private static int overWeight;
        private static volatile bool _stopNow;
        private static volatile bool _isIndexing;
        

        public static void initializeSuggestor()
        {
            luceneSearch = new LuceneSearch("lucene_index");
        }

        public static void indexWord(string word)
        {
            string[] wordsToBeIndexed = Regex.Split(word, @"\W+");
            Thread indexingThread = new Thread(() => indexInThread(wordsToBeIndexed, true));
            indexingThread.Priority = ThreadPriority.Highest;
            indexingThread.Start();
        }

        public static void indexUpdateWord(List<DataType> data_list)
        {
            Thread updatingThread = new Thread(() => updateInThread(data_list));
            updatingThread.Priority = ThreadPriority.Highest;
            updatingThread.Start();
        }

        public static void indexText(string words)
        {
            string[] wordsToBeIndexed = Regex.Split(words, @"\W+");
            Thread indexingThread = new Thread(() => indexInThread(wordsToBeIndexed, false));
            indexingThread.Priority = ThreadPriority.Highest;
            indexingThread.Start();
        }

        public static IEnumerable<DataType> getSuggestions(string prefix)
        {
            List<DataType> suggestions = new List<DataType>();
            IEnumerable<DataType> searchResults = new List<DataType>();
            try
            {
                searchResults = luceneSearch.Search(prefix);
            }
            catch 
            {
                return new List<DataType>();
            }
            int count = 0;
            foreach (DataType word in searchResults.Reverse<DataType>())
            {
                count++;
                suggestions.Add(word);
                if (count == 4)
                    break;
            }                            
            return suggestions;
        }
        
        public static bool saveInDisk()
        {
            try
            {
                luceneSearch.saveCurrentIndexesToDisk();
                return true;
            }
            catch { }
            return false;
        }

        public static IEnumerable<DataType> getAllIndexes()
        {
            if (_isIndexing)
                StopIndexing();
            System.Threading.Thread.Sleep(1000);
            List<DataType> suggestions = new List<DataType>();
            IEnumerable<DataType> searchResults = new List<DataType>();
            try
            {
                searchResults = luceneSearch.GetAllIndexRecords();
            }
            catch
            {
                return new List<DataType>();
            }
            IEnumerable<DataType> query = searchResults.OrderBy(word => word.Weight);
            foreach (DataType word in query.Reverse<DataType>())
            {
                suggestions.Add(word);
            }    
            return suggestions;
        }

        public static bool deleteAllIndexes()
        {
            try
            {
                luceneSearch.ClearLuceneIndex();
                return true;
            }
            catch
            { }
            return false;
        }

        public static void StopIndexing()
        {
            _stopNow = true;
        }

        private static void updateInThread(List<DataType>data_list)
        {
            foreach (DataType data in data_list)
            {
                if (data.Word.Length > 3)
                {
                    string finalWord = RemoveDiacritics(data.Word);
                    overWeight = luceneSearch.AddUpdateLuceneIndex(finalWord, data.Weight);

                    if (overWeight != 0 && !_isNormalizing)
                    {
                        _isNormalizing = true;
                        IEnumerable<DataType> wordsToBeCopied = luceneSearch.GetAllIndexRecords();
                        Thread normalizeThread = new Thread(() => Normalize(wordsToBeCopied));
                        normalizeThread.Start();
                    }
                }
            }
        }

        private static void indexInThread(string[] text, bool isUserEntry)
        {
            _isIndexing = true;
            int numberOfWordsIndexed = 0;
            string InsertedWord = "";
            foreach (string word in text)
            {
                if (_stopNow)
                {
                    Console.WriteLine("Number of words indexed = " + numberOfWordsIndexed.ToString());
                    break;
                }
                numberOfWordsIndexed++;
                
                if (word.Length > 3)
                {
                    string finalWord = RemoveDiacritics(word);
                    InsertedWord = finalWord.ToLower();
                    overWeight = luceneSearch.AddUpdateLuceneIndex(finalWord, isUserEntry);

                    if (overWeight != 0 && !_isNormalizing)
                    {
                        _isNormalizing = true;
                        IEnumerable<DataType> wordsToBeCopied = luceneSearch.GetAllIndexRecords();
                        Thread normalizeThread = new Thread(() => Normalize(wordsToBeCopied));
                        normalizeThread.Start();
                    }
                }    
            }
            _isIndexing = false;
            
        }
        
        private static void Normalize(IEnumerable<DataType> words)
        {            
            LuceneSearch temporarySearch = new LuceneSearch();
            foreach (DataType data in words)
            {
                data.Weight /= 2;
                temporarySearch.AddLuceneIndex(data);
            }
            luceneSearch.changeDirectory(temporarySearch.CurrentDirectory);
            _isNormalizing = false;
        }

        private static string RemoveDiacritics(string value)
        {            
            int j;
            string r = "";

            if (value == null)
            {
                return null;
            }

            value = value.ToLower();
            for (j = 0; j < value.Length; j++)
            {
                if ((value[j] == 'á') ||
                    (value[j] == 'â') ||
                    (value[j] == 'ã'))
                {
                    r = r + "a"; continue;
                }
                if ((value[j] == 'é') ||
                    (value[j] == 'ê'))
                {
                    r = r + "e"; continue;
                }
                if (value[j] == 'í')
                {
                    r = r + "i"; continue;
                }
                if ((value[j] == 'ó') ||
                    (value[j] == 'ô') ||
                    (value[j] == 'õ'))
                {
                    r = r + "o"; continue;
                }
                if ((value[j] == 'ú') ||
                    (value[j] == 'ü'))
                {
                    r = r + "u"; continue;
                }
                if (value[j] == 'ç')
                {
                    r = r + "c"; continue;
                }
                if (value[j] == 'ñ')
                {
                    r = r + "n"; continue;
                }

                r = r + value[j];
            }

            return r;
        }
    }
}
