﻿using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LSMEmprunts.Data
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string LicenceScanId { get; set; }

        public ICollection<Borrowing> Borrowings { get; set; }
    }


    class UserMapping : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Name).IsRequired();
            builder.Property(e => e.LicenceScanId).IsRequired();

            builder.HasIndex(e => e.Name).IsUnique();
            builder.HasIndex(e => e.LicenceScanId).IsUnique();
        }
    }
}
