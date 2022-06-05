using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace DayZServerControllerUI.LogParser
{
    public class LoggingDbContext : DbContext
    {
        public string DbPath { get; }

        public DbSet<DayZPlayer> Players { get; set; }
        public DbSet<LogLine> LogLines { get; set; }

        public LoggingDbContext()
        {
            string currentFolder = Environment.CurrentDirectory;
            string path = Path.Join(currentFolder, $"logDb.sqlite3");

            DbPath = path;
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<DayZPlayer>()
            //    .HasNoKey();

            //modelBuilder.Entity<LogLine>().HasNoKey();

            base.OnModelCreating(modelBuilder);
        }
    }
}
