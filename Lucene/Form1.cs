using Lucene.Net.Analysis;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Lucene;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;

namespace Lucene
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            dataGridView1.DataSource = table;
        }

        DataTable table
        {
            get
            {
                XmlReader xmlFile;
                xmlFile = XmlReader.Create("../../Data/data.xml", new XmlReaderSettings());
                DataSet ds = new DataSet();
                ds.ReadXml(xmlFile);
                return ds.Tables[0];
            }
        }

        Directory createIndex(DataTable table)
        {
            var directory = new RAMDirectory();

            using (Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
            using (var writer = new IndexWriter(directory, analyzer, new IndexWriter.MaxFieldLength(1000)))
            {
                foreach (DataRow row in table.Rows)
                {
                    var document = new Document();

                    document.Add(new Field("emp_no", row["emp_no"].ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    document.Add(new Field("birth_date", row["birth_date"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("first_name", row["first_name"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("last_name", row["last_name"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("gender", row["gender"].ToString(), Field.Store.YES, Field.Index.ANALYZED));
                    document.Add(new Field("hire_date", row["hire_date"].ToString(), Field.Store.YES, Field.Index.ANALYZED));

                    var birthDate = row["birth_date"].ToString();
                    var hireDate = row["hire_date"].ToString();

                    birthDate = string.Format("{0} {1}", birthDate, birthDate.Replace("-", " - "));
                    hireDate = string.Format("{0} {1}", hireDate, hireDate.Replace("-", " - "));

                    document.Add(new Field("FullText",
                        string.Format("{0} {1} {2} {3} {4} {5}", row["emp_no"], birthDate, row["first_name"], row["last_name"], row["gender"], hireDate), 
                        Field.Store.YES, Field.Index.ANALYZED));

                    writer.AddDocument(document);
                }

                writer.Optimize();
                writer.Flush(true, true, true);
            }

            return directory;
        }

        DataTable search(string Text)
        {
            var dataTable = table.Clone();

            var index = createIndex(table);

            using (var reader = IndexReader.Open(index, true))
            using (var searcher = new IndexSearcher(reader))
            {
                using (Analyzer analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30))
                {
                    var queryParser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, "FullText", analyzer);

                    queryParser.AllowLeadingWildcard = true;

                    var query = queryParser.Parse(Text);

                    var collector = TopScoreDocCollector.Create(1000, true);

                    searcher.Search(query, collector);

                    var matches = collector.TopDocs().ScoreDocs;

                    foreach (var item in matches)
                    {
                        var id = item.Doc;
                        var doc = searcher.Doc(id);

                        var row = dataTable.NewRow();

                        row["emp_no"] = doc.GetField("emp_no").StringValue;
                        row["birth_date"] = doc.GetField("birth_date").StringValue;
                        row["first_name"] = doc.GetField("first_name").StringValue;
                        row["last_name"] = doc.GetField("last_name").StringValue;
                        row["gender"] = doc.GetField("gender").StringValue;
                        row["hire_date"] = doc.GetField("hire_date").StringValue;

                        dataTable.Rows.Add(row);
                    }
                }
            }

            return dataTable;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var query = textBox1.Text.Trim();

            var result = search(query);

            dataGridView1.DataSource = result;
        }
    }
}
