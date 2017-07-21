using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net;

namespace AutoComplete.Classes
{
    public class DataType
    {
        public int Weight { get; set; }
        public string Word { get; set; }
    }    
}
