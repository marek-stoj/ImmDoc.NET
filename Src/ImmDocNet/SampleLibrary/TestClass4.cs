using System.Collections.Generic;

namespace SampleLibrary
{
  public class TestClass4 : IDictionary<int, bool>
  {
    public int[][,][][][,,][][][][,,,][][][][] Array;

    #region IDictionary<int,bool> Members

    public void Add(int key, bool value)
    {
      throw new System.NotImplementedException();
    }

    public bool ContainsKey(int key)
    {
      throw new System.NotImplementedException();
    }

    public ICollection<int> Keys
    {
      get { throw new System.NotImplementedException(); }
    }

    public bool Remove(int key)
    {
      throw new System.NotImplementedException();
    }

    public bool TryGetValue(int key, out bool value)
    {
      throw new System.NotImplementedException();
    }

    public ICollection<bool> Values
    {
      get { throw new System.NotImplementedException(); }
    }

    public bool this[int key]
    {
      get
      {
        throw new System.NotImplementedException();
      }
      set
      {
        throw new System.NotImplementedException();
      }
    }

    #endregion

    #region ICollection<KeyValuePair<int,bool>> Members

    public void Add(KeyValuePair<int, bool> item)
    {
      throw new System.NotImplementedException();
    }

    public void Clear()
    {
      throw new System.NotImplementedException();
    }

    public bool Contains(KeyValuePair<int, bool> item)
    {
      throw new System.NotImplementedException();
    }

    public void CopyTo(KeyValuePair<int, bool>[] array, int arrayIndex)
    {
      throw new System.NotImplementedException();
    }

    public int Count
    {
      get { throw new System.NotImplementedException(); }
    }

    public bool IsReadOnly
    {
      get { throw new System.NotImplementedException(); }
    }

    public bool Remove(KeyValuePair<int, bool> item)
    {
      throw new System.NotImplementedException();
    }

    #endregion

    #region IEnumerable<KeyValuePair<int,bool>> Members

    public IEnumerator<KeyValuePair<int, bool>> GetEnumerator()
    {
      throw new System.NotImplementedException();
    }

    #endregion

    #region IEnumerable Members

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      throw new System.NotImplementedException();
    }

    #endregion
  }
}
