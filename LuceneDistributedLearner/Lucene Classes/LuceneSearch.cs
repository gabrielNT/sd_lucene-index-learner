using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Support;
using Version = Lucene.Net.Util.Version;
using AutoComplete.Classes.BrAnalyzer;

namespace AutoComplete.Classes
{
    public  class LuceneSearch
    {
        private const int userStep = 5;
        private const int preAnalyzedWordStep = 1;
        private const int normalizeBoundary = 2000;

        private  string _luceneDir = Path.Combine("C:\\Lucene\\", "lucene_index");
        private  FSDirectory _directoryTemp;
        private  IndexWriter _indWriter;
        private  IndexSearcher _indSearcher;
        private  IEnumerable<DataType> existantIndex;
        private  BrazilianAnalyzer analyzer;
        private  QueryParser parser;
        private  int hits_limit;
        private  Sort sort;
        private  IEnumerable<DataType> fuzzyResults;
        private  IEnumerable<DataType> commonQueryResults;
        private  IEnumerable<DataType> results;
        private RAMDirectory _ramDirectory;
        private Lucene.Net.Store.Directory _currentDir;

        public Lucene.Net.Store.Directory CurrentDirectory
        {
            get { return _ramDirectory; }
        }

        private  FSDirectory _diskDirectory
        {
            get
            {
                if (_directoryTemp == null) 
                    _directoryTemp = FSDirectory.Open(new DirectoryInfo(_luceneDir));
                if (IndexWriter.IsLocked(_directoryTemp)) 
                    IndexWriter.Unlock(_directoryTemp);
                var lockFilePath = Path.Combine(_luceneDir, "write.lock");
                if (File.Exists(lockFilePath)) 
                    File.Delete(lockFilePath);
                return _directoryTemp;
            }
        }

        public LuceneSearch(string path)
        {            
            _luceneDir = Path.Combine("C:\\Lucene\\",path);
            try //to garantee that the C:/Lucene is created
            {
                _ramDirectory = new RAMDirectory(_diskDirectory);
            }
            catch
            {
                _ramDirectory = new RAMDirectory();
                _indWriter = new IndexWriter(_diskDirectory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
                CloseWriter();
            }
            sort = new Sort(new SortField[] { new SortField("Weight", SortField.INT) });
            hits_limit = 10000;
        }

        public LuceneSearch()
        {
            _ramDirectory = new RAMDirectory();
            sort = new Sort(new SortField[] { new SortField("Weight", SortField.INT) });
            hits_limit = 10000;
        }

        public void changeDirectory(Lucene.Net.Store.Directory dir)
        {
            _ramDirectory = new RAMDirectory(dir);
        }


        private  void InitializeWriter()
        {
            analyzer = new BrazilianAnalyzer(Version.LUCENE_30);
            parser = new QueryParser(Version.LUCENE_30, "Word", analyzer);
            try
            {
                _indWriter = new IndexWriter(_ramDirectory, analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
            }
            catch
            {
                _indWriter = new IndexWriter(_ramDirectory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);
            }
            _indWriter.MergeFactor = 50;
        }

        private  void InitializeReader()
        {
            analyzer = new BrazilianAnalyzer(Version.LUCENE_30);
            parser = new QueryParser(Version.LUCENE_30, "Word", analyzer);
            _indSearcher = new IndexSearcher(_ramDirectory, false);
        }

        private  void CloseWriter()
        {
            _indWriter.Dispose();
        }

        private  void CloseReader()
        {
            analyzer.Close();
            _indSearcher.Dispose();
        }
                
        public  bool ClearLuceneIndex() //delete all the indexes in RAM (the disk must be syncronized to be cleared as well)
        {
            try
            {
                InitializeWriter();
                _indWriter.DeleteAll();
                CloseWriter();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        //                                              weight do raw_data
        private int _addToLuceneIndex(string word, int currentWeight)
        {
            //peixe - 10 BASE PPRINCIPAL
            //peixe - 15 RAW DATA PROCESSOR
            existantIndex = new List<DataType>();
            existantIndex = _searchToIndex(word); // SEARCH NA BASE PRINCIPAL
            InitializeWriter();
            int previousWeight = 0;
            if (existantIndex.Any() && existantIndex != null)
            {
                previousWeight = existantIndex.First().Weight; //10
                currentWeight += previousWeight; //15
                Query query = parseQuery(word, parser); //peixe na base principal
                _indWriter.DeleteDocuments(query); //deleto peixe da base principal
            }

            // adds new entries to the index (in a new document)
            Document doc = new Document();

            doc.Add(new Field("Weight", currentWeight.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Word", word, Field.Store.YES, Field.Index.ANALYZED));

            _indWriter.AddDocument(doc); //na base principal

            CloseWriter();

            if (currentWeight >= normalizeBoundary)
                return currentWeight;
            return 0;
        }
        
        private int _addToLuceneIndex(string word, bool isUserEntry)
        {            
            existantIndex = new List<DataType>();
            existantIndex = _searchToIndex(word);
            InitializeWriter();
            int currentWeight = 0;
            if (existantIndex.Any() && existantIndex != null)
            {
                currentWeight = Step(existantIndex.First().Weight, isUserEntry);
                Query query = parseQuery(word, parser);
                _indWriter.DeleteDocuments(query);
            }
            else
            {
                currentWeight = Step(0, isUserEntry);
            }
            

            // adds new entries to the index (in a new document)
            Document doc = new Document();
            
            doc.Add(new Field("Weight", currentWeight.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Word", word, Field.Store.YES, Field.Index.ANALYZED));

            _indWriter.AddDocument(doc);
            
            CloseWriter();

            if (currentWeight >= normalizeBoundary)
                return currentWeight;
            return 0;
        }
        
        public int AddUpdateLuceneIndex(string word, bool isUserEntry)
        {
            return _addToLuceneIndex(word, isUserEntry);
        }

        public int AddUpdateLuceneIndex(string word, int actualWeight)
        {
            return _addToLuceneIndex(word, actualWeight);
        }

        public void AddLuceneIndex(DataType readyWord) //doesn't verify previous occurences of a word, considering therefore, that there are no repetitions (used in normalization)
        {
            InitializeWriter();
            Document doc = new Document();

            doc.Add(new Field("Weight", readyWord.Weight.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("Word", readyWord.Word, Field.Store.YES, Field.Index.ANALYZED));
            
            _indWriter.AddDocument(doc);

            CloseWriter();
        }        

        private  DataType _mapLuceneDocumentToData(Document doc)
        {
            return new DataType
            {
                Weight = Convert.ToInt32(doc.Get("Weight")),
                Word = doc.Get("Word"),
            };
        }

        private  IEnumerable<DataType> _mapLuceneToDataList(IEnumerable<Document> hits)
        {
            return hits.Select(_mapLuceneDocumentToData).ToList();
        }

        private  IEnumerable<DataType> _mapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {            
            return hits.Select(hit => _mapLuceneDocumentToData(searcher.Doc(hit.Doc))).ToList();
        }

        #region Código Base do Search (comentado apenas)
        //private  IEnumerable<SampleData> _search(string searchQuery, string searchField = "")
        //{
        //    InitializeReader();
        //    // validação
        //    if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<SampleData>();

        //    // preparacao do searcher
        //    //using (var searcher = new IndexSearcher(_directory, false))
        //    //{
        //        var hits_limit = 1000; //numero de resultados trazidos pela busca
        //        //var analyzer = new StandardAnalyzer(Version.LUCENE_30);
        //        var analyzer = new BrazilianAnalyzer(Version.LUCENE_30);

        //        // busca por um campo unico
        //        if (!string.IsNullOrEmpty(searchField))
        //        {
        //            var parser = new QueryParser(Version.LUCENE_30, searchField, analyzer);
        //            //var query = parseQuery(searchQuery, parser);
        //            var query = parseQuery(searchQuery, parser);
        //            Sort sort = new Sort(SortField.FIELD_SCORE);
        //            //var filter = new DuplicateFilter("Word");
        //            //var hits = searcher.Search(query, null, hits_limit, sort).ScoreDocs;
        //            var hits = _indSearcher.Search(query, null, hits_limit, sort).ScoreDocs;

        //            var results = _mapLuceneToDataList(hits, _indSearcher);
        //            //se nao achou nada, tenta fazer a query com fuzzy (encontrando similares)
        //            if (results.Count() == 0)
        //            {
        //                query = fparseQuery(searchQuery);
        //                hits = _indSearcher.Search(query, null, hits_limit, sort).ScoreDocs;
        //                results = _mapLuceneToDataList(hits, _indSearcher);
        //            }

                    
        //            analyzer.Close();
        //            //_indSearcher.Dispose();
        //            CloseReader();
        //            return results;
        //        }
        //        // search by multiple fields (ordered by RELEVANCE)
        //        else
        //        {
        //            var parser = new MultiFieldQueryParser
        //                (Version.LUCENE_30, new[] { "Weight", "Word"}, analyzer);
        //            var query = parseQuery(searchQuery, parser);                    
        //            Sort sort = new Sort(SortField.FIELD_SCORE);
        //            var hits = _indSearcher.Search(query, null, hits_limit, sort).ScoreDocs;//(searcher.CreateWeight(query), null, hits_limit).ScoreDocs; //(query, null, hits_limit,Sort.RELEVANCE ).ScoreDocs;                   

        //            var results = _mapLuceneToDataList(hits, _indSearcher);

        //            //se nao achou nada, tenta fazer a query com fuzzy (encontrando similares)
        //            if (results.Count() == 0)
        //            {
        //                query = fparseQuery(searchQuery);
        //                hits = _indSearcher.Search(query, null, hits_limit, sort).ScoreDocs;
        //                results = _mapLuceneToDataList(hits, _indSearcher);
        //            }

                    

        //            analyzer.Close();
        //            CloseReader();
        //            //_indSearcher.Dispose();
        //            return results;
        //        }

        //    //}
        //}
        #endregion

        
        private  IEnumerable<DataType> _searchToIndex (string searchQuery)
        {
            // validation
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<DataType>();
            // set up lucene searcher
            InitializeReader();
            Query query = parseQuery(searchQuery, parser);
            var hits = _indSearcher.Search(query, hits_limit).ScoreDocs;
            results = _mapLuceneToDataList(hits, _indSearcher);
            CloseReader();
            return results;                            
        }

        private  IEnumerable<DataType> _search(string searchQuery) //search suggestions
        {
            if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", ""))) return new List<DataType>();

            InitializeReader();
            var query = parseQuery(searchQuery, parser);
            var hits = _indSearcher.Search(query, null, hits_limit, sort).ScoreDocs;
            commonQueryResults = _mapLuceneToDataList(hits, _indSearcher);
            if (commonQueryResults.Count() < 4) //if there is no results with the given prefix, tries to find similar word entries.
            {
                query = fparseQuery(searchQuery);
                hits = _indSearcher.Search(query, null, hits_limit, sort).ScoreDocs;
                fuzzyResults = _mapLuceneToDataList(hits, _indSearcher);
                bool isNotACommonWord;
                if (commonQueryResults.Count() > 0)
                {
                    results = commonQueryResults;
                    foreach (DataType fuzzyWord in fuzzyResults.Reverse<DataType>())
                    {
                        isNotACommonWord = true;
                        foreach(DataType commonWord in  results.Reverse<DataType>())
                        {
                            if (fuzzyWord.Word == commonWord.Word)
                            {
                                isNotACommonWord = false;
                                break;                                
                            }
                        }
                        if (isNotACommonWord)
                        {
                            results = results.Concat(new[] { fuzzyWord });
                            if (results.Count() == 4)
                                break;
                        }
                    }
                }
                else
                {
                    results = fuzzyResults;
                }
            }
            else
            {
                results = commonQueryResults;
            }
            CloseReader();
            return results;           
        } 

        
        private  Query parseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
            return query;
        }

        private  FuzzyQuery fparseQuery(string searchQuery) 
        {
            var query = new FuzzyQuery(new Term("Word", searchQuery), 0.5f);
            return query;
        }

        public  IEnumerable<DataType> Search(string input, string fieldName = "") //adjusts the query and returns only the word
        {
            if (string.IsNullOrEmpty(input)) return new List<DataType>();
            var terms = input.Trim().Replace("-", " ").Split(' ')
                .Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Trim() + "*");
            input = string.Join(" ", terms);
            return _search(input);
        }
                
        public  IEnumerable<DataType> GetAllIndexRecords() //returns all the words indexed in Lucene at the moment (it takes some time to process because of the ListBox .Add function, but the retrieving itself is minimal)
        {
            // checks the existance of the index base
            if (!System.IO.Directory.EnumerateFiles(_luceneDir).Any()) return new List<DataType>();

            var searcher = new IndexSearcher(_ramDirectory, false);
            var reader = IndexReader.Open(_ramDirectory, false);
            var docs = new List<Document>();
            var term = reader.TermDocs();
            while (term.Next()) docs.Add(searcher.Doc(term.Doc));
            reader.Dispose();
            searcher.Dispose();
            return _mapLuceneToDataList(docs);
        }

        public void saveCurrentIndexesToDisk()
        {
            analyzer = new BrazilianAnalyzer(Version.LUCENE_30);
            parser = new QueryParser(Version.LUCENE_30, "Word", analyzer);
            _indWriter = new IndexWriter(_diskDirectory, analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
            _indWriter.DeleteAll();
            _indWriter.AddIndexesNoOptimize(_ramDirectory);
            CloseWriter();
        }
        
        private int Step(int currentWeight, bool isUserEntry)
        {
            if (isUserEntry) //if it was an user entry, the weight is incremented in 10, otherwise in 1
                return currentWeight + userStep;            
            else
                return currentWeight + preAnalyzedWordStep;
        }
    }
}