using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleLibrary
{
  public class TestClass3<T, U> : List<List<T[]>> where T : U
  {
  }
}
