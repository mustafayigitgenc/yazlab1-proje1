namespace Gumusdere.Navigation
{
    /// <summary>
    /// NavMesh alan numaraları. ProjectSettings/NavMeshAreas.asset ile aynı sırada olmalıdır.
    /// </summary>
    public static class NavAreas
    {
        public const int Walkable = 0;
        public const int NotWalkable = 1;
        public const int Jump = 2;

        /// <summary>Sığ geçit. Sadece Cemal'in alan maskesinde vardır; yürümek normal zeminden 3 kat "pahalıdır".</summary>
        public const int ShallowWater = 3;
        public const float ShallowWaterCost = 3f;

        public static int MaskOf(int area) => 1 << area;
    }
}
