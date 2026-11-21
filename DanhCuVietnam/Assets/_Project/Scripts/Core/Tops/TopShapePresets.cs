namespace DanhCu.Core.Tops
{
    /// <summary>
    /// The three initial prototype sizes. These are ASSUMED, configurable prototype
    /// dimensions chosen for playtesting; they are not historical or measured values.
    /// Replace them with measurements of real tops when available (Docs/TOP_MODEL.md §2).
    /// </summary>
    public static class TopShapePresets
    {
        public static TopShapeParameters Small()
        {
            return new TopShapeParameters
            {
                TotalHeight = 0.060,
                BodyDiameter = 0.045,
                CollarDiameter = 0.008,
                TipExposedLength = 0.010,
                TipShankDiameter = 0.004,
                TipConeLength = 0.0065,
                TipPointRadius = 0.0006,
                TipEmbedDepth = 0.012,
                RopeGrooveCount = 3,
                RopeGrooveDepth = 0.0003,
                RopeGrooveWidth = 0.0014,
            };
        }

        /// <summary>Same as <c>new TopShapeParameters()</c>.</summary>
        public static TopShapeParameters Medium() => new TopShapeParameters();

        public static TopShapeParameters Large()
        {
            return new TopShapeParameters
            {
                TotalHeight = 0.090,
                BodyDiameter = 0.066,
                CollarDiameter = 0.012,
                TipExposedLength = 0.014,
                TipShankDiameter = 0.006,
                TipConeLength = 0.0095,
                TipPointRadius = 0.0010,
                TipEmbedDepth = 0.018,
                RopeGrooveCount = 5,
                RopeGrooveDepth = 0.0005,
                RopeGrooveWidth = 0.0018,
            };
        }
    }
}
