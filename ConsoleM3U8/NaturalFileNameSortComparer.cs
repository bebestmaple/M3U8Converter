namespace ConsoleM3U8
{
    public sealed class NaturalFileNameSortComparer : System.Collections.Generic.IComparer<string>
    {
    	/// <summary>
    	/// compare
    	/// </summary>
    	/// <param name="a"></param>
    	/// <param name="b"></param>
    	/// <returns></returns>
    	public int Compare(string? a, string? b)
    	{
    		return NaturalCompare(a, b);
    	}

    	private static int NaturalCompare(string? s1, string? s2)
    	{
    		if (s1 == null)
    			return (s2 == null) ? 0 : -1;
    		else if (s2 == null)
    			return 1;

    		int len1 = s1.Length;
    		int len2 = s2.Length;
    		int i1 = 0, i2 = 0;

    		while (i1 < len1 && i2 < len2)
    		{
    			char c1 = s1[i1];
    			char c2 = s2[i2];

    			if (char.IsDigit(c1) && char.IsDigit(c2))
    			{
    				long num1 = 0;
    				long num2 = 0;

    				while (i1 < len1 && char.IsDigit(s1[i1]))
    				{
    					num1 = num1 * 10 + (s1[i1] - '0');
    					i1++;
    				}

    				while (i2 < len2 && char.IsDigit(s2[i2]))
    				{
    					num2 = num2 * 10 + (s2[i2] - '0');
    					i2++;
    				}

    				if (num1 != num2)
    					return (num1 < num2) ? -1 : 1;
    			}
    			else
    			{
    				if (c1 != c2)
    					return (c1 < c2) ? -1 : 1;

    				i1++;
    				i2++;
    			}
    		}

    		return len1 - len2;
    	}
    }






}
