namespace BinaryNodeList
{
    /// <summary>
    /// List of node types defined by the BNL spec.
    /// </summary>
    public enum BnlNodeType
    {
        Any = 0x00,
        String = 0x01,
        Vec1b = 0x11,
        Vec2b = 0x12,
        Vec3b = 0x13,
        Vec4b = 0x14,
        Vec1s = 0x21,
        Vec2s = 0x22,
        Vec3s = 0x23,
        Vec4s = 0x24,
        Vec1i = 0x41,
        Vec2i = 0x42,
        Vec3i = 0x43,
        Vec4i = 0x44,
        Vec1f = 0x4A,
        Vec2f = 0x4B,
        Vec3f = 0x4C,
        Vec4f = 0x4D,
        Vec1l = 0x81,
        Vec2l = 0x82,
        Vec3l = 0x83,
        Vec4l = 0x84,
        Vec1d = 0x8A,
        Vec2d = 0x8B,
        Vec3d = 0x8C,
        Vec4d = 0x8D,
    }
}
