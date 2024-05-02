using Dapper;
using Microsoft.Extensions.Configuration;
using Vista.DB.Schema;
using Vista.DbPanda;

namespace TestPanda;

[TestClass]
public class UnitTest4 : TestBase
{
  [TestMethod("測試 prShowMeTheMoney")]
  public void TestMethod1()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    int ret = conn.CallprShowMeTheMoney();
    Assert.AreEqual(1, ret);
  }

  [TestMethod("測試 prBatchInsertMyData")]
  public void TestMethod2()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    //------
    List<MyDataTvp> dataList = Enumerable.Range(101, 10).Aggregate(
      new List<MyDataTvp>(),
      (list, num) =>
      {
        list.Add(new MyDataTvp
        {
          IDN = $"TEST{num}",
          Title = "測試 prBatchInsertMyData 整批加入",
          Amount = 999,
          Birthday = DateTime.Today
        });

        return list;
      });

    var result = conn.CallprBatchInsertMyData(dataList);
    Assert.AreEqual(10, result[0].RowsAffected);
    
    // 清除測試資料
    string purgeSql = "DELETE FROM MyData WHERE Title = '測試 prBatchInsertMyData 整批加入' ";
    int delCnt = conn.Execute(purgeSql);
    Assert.AreEqual(10, delCnt);
  }

  [TestMethod("測試 View: vwTestView")]
  public void TestMethod3()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    //------
    string sql = "SELECT * FROM vwTestView ";
    var dataList = conn.QueryEx<vwTestView>(sql);
    Assert.IsTrue(dataList.Count > 10);
  }

  [TestMethod("測試 View Function: vwMyData")]
  public void TestMethod4()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    //------
    List<vwMyDataResult> dataList = conn.CallvwMyData("今天天氣真好", 1000);
    Assert.IsTrue(dataList.Count > 10);
  }
}
