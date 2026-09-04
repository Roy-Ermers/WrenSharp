


using Wren;

namespace WrenTest;

[WrenClass("engine", "Vector3")]
public partial class Vector3
{
    [WrenProperty]
    public int X { get; set; }
    
    [WrenProperty]
    public int Y { get; set; }
    
    [WrenProperty]
    public int Z { get; set; }
    
    public Vector3(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vector3()
    {
        X = 0;
        Y = 0;
        Z = 0;
    }
    
    [WrenMethod]
    public double GetLength()
    {
        return Math.Sqrt(X * Y * Z);
    }    
    [WrenMethod]
    public string Print()
    {
        return $"({X}, {Y}, {Z})";
    }
}