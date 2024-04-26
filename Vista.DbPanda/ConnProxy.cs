using Microsoft.Data.SqlClient;
using System.Security;
using System.Text;

/***************************************************************************
第三版 DBHelper.v３.0 on 2023-1-18
***************************************************************************/

namespace Vista.DbPanda;

public class ConnProxy
{
  /// <summary>
  /// 連線字串，平常保持在加密狀態。
  /// </summary>
  private SecureString _connStr;

#if DEBUG
  /// <summary>
  /// for DEBUG: 檢查連線字串內容
  /// </summary>
  public String DebugConnString => AsString(_connStr);
#endif

  public ConnProxy(string connString)
  {
    /// 連線字串只有建構時可設定。
    _connStr = AsSecureString(connString);
  }

  public ConnProxy(SecureString connString)
  {
    /// 連線字串只有建構時可設定。
    _connStr = connString;
  }

  public virtual SqlConnection Open()
  {
    try
    {
      var conn = CreateSqlConnection(_connStr);
      conn.Open();
      //OnOpenSuccess?.Invoke(this, new EventArgs());
      return conn;
    }
    catch (Exception ex)
    {
      //OnOpenFail?.Invoke(this, new ErrMsgEventArgs($"DB連線失敗！", ErrSeverity.Exception, ex));
      throw;
    }
  }

  public async Task<SqlConnection> OpenAsync()
  {
    try
    {
      var conn = CreateSqlConnection(_connStr);
      await conn.OpenAsync();
      //OnOpenSuccess?.Invoke(this, new EventArgs());
      return conn;
    }
    catch (Exception ex)
    {
      //OnOpenFail?.Invoke(this, new ErrMsgEventArgs($"DB連線失敗！", ErrSeverity.Exception, ex));
      throw;
    }
  }

  private static SecureString AsSecureString(String str)
  {
    SecureString secstr = new();
    str.ToList().ForEach(secstr.AppendChar);
    secstr.MakeReadOnly();
    return secstr;
  }

  private static SqlConnection CreateSqlConnection(SecureString ss)
  {
    try
    {
      IntPtr ssAsIntPtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(ss);
      //string connStr = System.Runtime.InteropServices.Marshal.PtrToStringUni(ssAsIntPtr);
      StringBuilder connStr = new();
      for (Int32 i = 0; i < ss.Length; i++)
      {
        // multiply 2 because Unicode chars are 2 bytes
        Char ch = (Char)System.Runtime.InteropServices.Marshal.ReadInt16(ssAsIntPtr, i * 2);
        // do something with each char
        connStr.Append(ch);
      }
      // don't forget to free it at the end
      System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(ssAsIntPtr);

      return new SqlConnection(connStr.ToString());
    }
    catch
    {
      return null;
    }
  }

  private static String AsString(SecureString secstr)
  {
    IntPtr valuePtr = IntPtr.Zero;
    try
    {
      valuePtr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secstr);
      return System.Runtime.InteropServices.Marshal.PtrToStringUni(valuePtr);
    }
    catch
    {
      return null;
    }
    finally
    {
      System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
    }
  }

  // 移除，因為未達預期作用。
  //public event EventHandler OnOpenSuccess;
  //public event EventHandler<ErrMsgEventArgs> OnOpenFail;
}
