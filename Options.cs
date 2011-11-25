using System;
using System.Collections;
using System.Linq;
using System.Text;


public class Arg
{
  public Arg(double a_min, double a_inc, double a_cnt) { m_min = a_min; m_inc = a_inc; m_cnt = a_cnt; m_best = 0; m_cur = 0; }
  public double m_min;
  public double m_inc;
  public double m_cnt;
  public int m_cur;
  public int m_best;
}


public class Options : IEnumerable, IEnumerator
{
  public Options(Arg[] a_args)
  {
    m_args = a_args;
    m_args[0].m_cur = -1;
  }

  Arg[] m_args;

  public IEnumerator GetEnumerator()
  {
    return (IEnumerator)this;
  }

  public bool MoveNext()
  {
    bool valid = false;
    foreach (Arg a in m_args)
    {
      a.m_cur++;
      if (a.m_cur >= a.m_cnt)
      {
        a.m_cur = 0;
      }
      else
      {
        valid = true;
        break;
      }
    }
    return valid;
  }

  public void Reset()
  {
    foreach (Arg a in m_args)
    {
      a.m_cur = 0;
    }
    m_args[0].m_cur = -1;
  }

  public object Current
  {
    get
    {
      string r = "";
      foreach (Arg a in m_args)
      {
        r += (a.m_min + a.m_cur * a.m_inc).ToString() + ",";
      }
      if(r.Length > 0)
      {
        r = r.Remove(r.Length - 1);
      }
      return r;
    }
  }
}
