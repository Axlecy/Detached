﻿using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Detached.Demo.Model
{
    public class MainDbContext : DbContext
    {
        public MainDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }

        public DbSet<SellPoint> SellPoints { get; set; }

        public DbSet<SellPointType> SellPointTypes { get; set; }
    }
}
