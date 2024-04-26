using Dapper;
using Microsoft.Extensions.Configuration;
using Vista.DB.Schema;
using Vista.DbPanda;

namespace TestPanda;

[TestClass]
public class UnitTest2 : TestBase
{
  [TestMethod("資料庫查詢")]
  public void TestMethod1()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    string sql = @"SELECT * FROM MyData WHERE IDN = @IDN";
    var dataList = conn.Query<MyData>(sql, new { IDN = "A003003" }).AsList();
    Assert.IsNotNull(dataList);
    Assert.AreEqual(5, dataList.Count, "預計取出 5 筆");
    var info = dataList[0];
    Assert.AreEqual("A003003", info.IDN);
    Assert.AreEqual("你好嗎我很好", info.Title);
  }

  [TestMethod("資料庫單筆CRUD")]
  public void TestMethod2()
  {
    var connString = Configuration.GetConnectionString("DefaultConnection");
    Assert.IsNotNull(connString);
    var proxy = new ConnProxy(connString);
    using var conn = proxy.Open();

    //# 新增一筆
    MyData newData = new MyData
    {
      SN = 0,
      IDN = "A009009",
      Title = "今天天氣真好",
      Amount = (decimal?)987.1234,
      Birthday = DateTime.Today,
      Remark = "來自測試專案"
    };

    long newId = conn.InsertEx(newData);
    newData.SN = newId;

    //# 取出該筆
    var info = conn.GetEx<MyData>(new { SN = newId });
    Assert.IsNotNull(info);
    Assert.AreEqual(DateTime.Today, info.Birthday);

    //# 更新該筆
    info.Amount = (decimal?)9999.8888;
    info.Birthday = DateTime.Today.AddDays(-1);
    int updCnt = conn.UpdateEx<MyData>(info, new { info.SN });
    Assert.AreEqual(1, updCnt);

    var updInfo = conn.GetEx<MyData>(new { info.SN });
    Assert.IsNotNull(updInfo);
    Assert.AreEqual(info.Amount, updInfo.Amount);
    Assert.AreEqual(info.Birthday, updInfo.Birthday);

    //# 刪除
    int delCnt = conn.DeleteEx<MyData>(new { updInfo.IDN });
    Assert.IsTrue(delCnt > 0);
  }

}
