using Dapper;
using Microsoft.Extensions.Configuration;
using Vista.DB.Schema;
using Vista.DbPanda;

namespace TestPanda;

[TestClass]
public class UnitTest3 : TestBase
{
  [TestMethod("測試 LoadEx")]
  public void TestMethod1()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    var dataList = conn.LoadEx<MyData>(new { IDN = "A004001" });
    Assert.IsNotNull(dataList);
    Assert.AreEqual(5, dataList.Count, "預計取出 5 筆");
  }

  [TestMethod("測試批次 InsertEx")]
  public void TestMethod2()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    // 
    List<MyProduct> insertList = Enumerable.Range(101, 1000).Aggregate(
      new List<MyProduct>(),
      (list, num) =>
      {
        list.Add(new MyProduct { Sn = 0, Title = $"我是第{num}筆", Status = "Disable" });
        return (List<MyProduct>)list;
      }, list => list);

    int insertCnt = conn.InsertEx<MyProduct>(insertList);
    Assert.AreEqual(1000, insertCnt);

    string deleteBatch = "DELETE MyProduct WHERE SN > 29";
    int deleteCnt = conn.Execute(deleteBatch);
    Assert.AreEqual(1000, deleteCnt);
  }

  [TestMethod("測試批次 BulkInsert")]
  public void TestMethod3()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    // 
    List<MyProduct> insertList = Enumerable.Range(101, 1000).Aggregate(
      new List<MyProduct>(),
      (list, num) =>
      {
        list.Add(new MyProduct { Sn = 0, Title = $"我是第{num}筆", Status = "Disable" });
        return (List<MyProduct>)list;
      }, list => list);

    conn.BulkInsert<MyProduct>(insertList);

    string selectBatchCount = "SELECT COUNT(*) FROM MyProduct WHERE SN > 29";
    int batchCnt = conn.ExecuteScalar<int>(selectBatchCount);
    Assert.AreEqual(1000, batchCnt);

    string deleteBatch = "DELETE MyProduct WHERE SN > 29";
    int deleteCnt = conn.Execute(deleteBatch);
    Assert.AreEqual(1000, deleteCnt);
  }

}
