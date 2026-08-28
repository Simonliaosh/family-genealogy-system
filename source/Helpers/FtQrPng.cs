using QRCoder;

namespace FamilyTree.Helpers;

public static class FtQrPng
{
    public static byte[] PngBytes(string text, int pixelsPerModule = 8)
    {
        var gen = new QRCodeGenerator();
        var data = gen.CreateQrCode(text ?? "", QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(Math.Clamp(pixelsPerModule, 4, 16));
    }
}
