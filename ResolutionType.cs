using System;

namespace MediaCurator
{
   /// <summary>
   /// The type representing a photo or video file's resolution. It has a width and a height property
   /// and multiple ways of supplying them.
   /// </summary>
   public class ResolutionType
   {
      public uint Width { get; set; }
      public uint Height { get; set; }

      /// <summary>
      /// Initializes a new instance of the <see cref="ResolutionType"/> class having been supplied
      /// the resolution inside a string of "WxH" format.
      /// </summary>
      /// <param name="resolution">The resolution with width and height separated using 'x'.</param>
      public ResolutionType(string resolution)
      {
         // Initialize the Width and Height.
         Width = Height = 0;

         try
         {
            if (resolution.Contains('x'))
            {
               Width = Convert.ToUInt16(resolution.Substring(0, resolution.IndexOf('x')));
               Height = Convert.ToUInt16(resolution.Substring((resolution.IndexOf('x') + 1),
                                                                (resolution.Length - (resolution.IndexOf('x') + 1))));
            }
         }
         catch (Exception)
         {
            // 0x0 it is then!
            return;
         }
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="ResolutionType"/> class having been supplied 
      /// the width and the height.
      /// </summary>
      /// <param name="width">The width.</param>
      /// <param name="height">The height.</param>
      public ResolutionType(uint width, uint height)
      {
         Width = width;
         Height = height;
      }

      /// <summary>
      /// Returns a <see cref="System.String" /> that represents this instance.
      /// </summary>
      /// <returns>
      /// A <see cref="System.String" /> that represents this instance. The format in which the 
      /// resolution is returned is "WxH".
      /// </returns>
      public override string ToString()
      {
         string resolution = "";

         if ((Width > 0) && (Height > 0))
         {
            resolution = String.Format("{0}x{1}", Width, Height);
         }

         return resolution;
      }
   }
}