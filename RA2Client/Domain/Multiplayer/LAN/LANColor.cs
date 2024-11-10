using Microsoft.Xna.Framework;

namespace Ra2Client.Domain.LAN
{
    public class LANColor
    {
        public LANColor(string name, Color xnaColor)
        {
            Name = name;
            XNAColor = xnaColor;
        }

        public string Name { get; private set; }
        public Color XNAColor { get; private set; }
    }
}
