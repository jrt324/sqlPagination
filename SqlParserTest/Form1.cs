using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using gudusoft.gsqlparser;
using gudusoft.gsqlparser.nodes;
using gudusoft.gsqlparser.stmt;


namespace SqlParserTest
{
    public partial class Form1 : Form
    {
       List<SqlPageInfo> list =new List<SqlPageInfo>();
        private long StopBytes = 0;
        private long StartBytes = 0;
        public Form1()
        {
            InitializeComponent();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            TGSqlParser sqlparser = new TGSqlParser(EDbVendor.dbvmssql);
            sqlparser.sqltext = txtSql.Text;
            int ret = sqlparser.parse();

            if (ret == 0)
            {
                var select = (TSelectSqlStatement)sqlparser.sqlstatements[0];
                
                if (select.OrderbyClause != null)
                {
                    var orderBy = select.OrderbyClause.String;
                    var colSql = $"ROW_NUMBER() OVER({orderBy}) AS RowNum";
                    var colRowNum = new TResultColumn
                    {
                        Expr = new TExpression
                        {
                            FunctionCall = @select.Gsqlparser.parseFunctionCall(colSql),
                            ExpressionType = EExpressionType.function_t,
                        },
                        AliasClause = new TAliasClause
                        {
                            AliasName = @select.Gsqlparser.parseObjectName("RowNum"),
                            AsToken = new TSourceToken("AS")
                        }
                    };
                    select.ResultColumnList.addResultColumn(colRowNum);
                }
                else
                {
                    throw new Exception("select statement missing order by field");
                }
                var sql = select.ToScript();
                var sqlPaging = $@"SELECT * FROM ( {sql}) AS RowConstrainedResult WHERE RowNum >= 1 AND RowNum< 20 ORDER BY RowNum";

                // for count result
                select.ResultColumnList = new TResultColumnList();
                var countCol = new TResultColumn
                {
                    Expr = new TExpression
                    {
                        FunctionCall = @select.Gsqlparser.parseFunctionCall("count(1) as RowCount"),
                        ExpressionType = EExpressionType.function_t,
                    },
                    AliasClause = new TAliasClause
                    {
                        AliasName = @select.Gsqlparser.parseObjectName("RowCount"),
                        AsToken = new TSourceToken("AS")
                    }
                };
                select.ResultColumnList.addResultColumn(countCol);

                //去除order by 
                select.OrderbyClause = null;

                var totalSql = select.ToScript();

                var info = new SqlPageInfo{PageSql = sqlPaging, TotalSql = totalSql};
                list.Add(info);
            }
            else
            {
                throw new Exception(sqlparser.Errormessage);
                //System.out.println(sqlparser.getErrormessage());
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < 40000; i++)
            {
                button1_Click(null, null);
            }
            stopwatch.Stop();
            var elapsed_time = stopwatch.ElapsedMilliseconds / 1000;
            lbTotalTime.Text = $"总共耗时：{elapsed_time}秒";
            System.GC.Collect();
           
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartBytes = System.GC.GetTotalMemory(true);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            StopBytes = System.GC.GetTotalMemory(true);
            MessageBox.Show("Size is " + (StopBytes - StartBytes));
        }
    }

    public class SqlPageInfo
    {
        public string TotalSql { get; set; }
        public string PageSql { get; set; }
    }
}
