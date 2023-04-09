using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace MediaCurator.Services
{
   public interface IFileSystemService: IHostedService, IDisposable
   {
      List<FileSystemMount> Mounts { get; }
   }
}
