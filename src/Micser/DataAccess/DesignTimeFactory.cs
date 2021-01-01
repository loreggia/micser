﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Micser.DataAccess
{
    // ReSharper disable once UnusedType.Global
    public class DesignTimeFactory : IDesignTimeDbContextFactory<EngineDbContext>
    {
        public EngineDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder();
            builder.UseSqlite("DesignTimeEngineDb.db");
            return new EngineDbContext(builder.Options);
        }
    }
}