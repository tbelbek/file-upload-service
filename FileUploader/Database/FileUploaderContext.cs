using System;
using System.Data.Entity;

namespace FileUploader.Database
{
    public class FileUploaderContext : DbContext
    {

        public virtual DbSet<Files> Files { get; set; }
    }

    public class Files
    {
        public Guid Id { get; set; }
        public string HashVal { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
    }
}