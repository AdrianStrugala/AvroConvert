
using System.Collections.Generic;

public class Data
{
    public int length { get; set; }
    public string text { get; set; }
}

public class ObjArray
{
    public string @class { get; set; }
    public int age { get; set; }
}

public class Dataset
{
    public int min_position { get; set; }
    public bool has_more_items { get; set; }
    public string items_html { get; set; }
    public int new_latent_count { get; set; }
    public Data data { get; set; }
    public List<int> numericalArray { get; set; }
    public List<string> StringArray { get; set; }
    public bool boolArray { get; set; }
    public List<ObjArray> objArray { get; set; }
}
