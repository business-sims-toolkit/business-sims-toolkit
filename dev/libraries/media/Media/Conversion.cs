using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Drawing;

using DirectShowLib;

namespace Media
{
	public static class Conversion
	{
		[DllImport("GdiPlus.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		static extern int GdipCreateBitmapFromGdiDib (IntPtr pBitmapInfoHeader, IntPtr pPixelData, out IntPtr pBitmap);

		public static Bitmap GetBitmapFromDib (IntPtr pDib)
		{
			IntPtr pPixelData = GetPixelInfo(pDib);
			IntPtr pBitmap;

			DsError.ThrowExceptionForHR(GdipCreateBitmapFromGdiDib(pDib, pPixelData, out pBitmap));

			if (pBitmap != IntPtr.Zero)
			{
				MethodInfo method = (typeof (Bitmap)).GetMethod("FromGDIplus", BindingFlags.Static | BindingFlags.NonPublic);
				if (method != null)
				{
					return (Bitmap) (method.Invoke(null, new object [] { pBitmap }));
				}
			}

			return null;
		}

		static IntPtr GetPixelInfo (IntPtr pDib)
		{
			BitmapInfoHeader header = (BitmapInfoHeader) Marshal.PtrToStructure(pDib, typeof (BitmapInfoHeader));

			if (header.ImageSize == 0)
			{
				header.ImageSize = ((((header.Width * header.BitCount) + 31) & ~ 31) >> 3) * header.Height;
			}

			int pointer = (int) header.ClrUsed;
			if ((pointer == 0) && (header.BitCount <= 8))
			{
				pointer = 1 << header.BitCount;
			}

			pointer = (pointer * 4) + (int) header.Size + (int) pDib;
			
			return (IntPtr) pointer;
		}
	}
}