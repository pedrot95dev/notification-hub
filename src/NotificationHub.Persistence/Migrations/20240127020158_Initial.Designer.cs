﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NotificationHub.Persistence;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NotificationHub.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20240127020158_Initial")]
    partial class Initial
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("NotificationHub.Persistence.Entities.Application", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Domain")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("EmailDestination")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("ExternalId")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("SmtpConfigurationId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("SmtpConfigurationId")
                        .IsUnique();

                    b.ToTable("Applications");
                });

            modelBuilder.Entity("NotificationHub.Persistence.Entities.EmailSent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid>("ApplicationId")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.HasIndex("ApplicationId");

                    b.ToTable("EmailSents");
                });

            modelBuilder.Entity("NotificationHub.Persistence.Entities.SmtpConfiguration", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<bool>("EnableSsl")
                        .HasColumnType("boolean");

                    b.Property<string>("Host")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<int>("Port")
                        .HasColumnType("integer");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("SmtpConfigurations");
                });

            modelBuilder.Entity("NotificationHub.Persistence.Entities.Application", b =>
                {
                    b.HasOne("NotificationHub.Persistence.Entities.SmtpConfiguration", "SmtpConfiguration")
                        .WithOne("Application")
                        .HasForeignKey("NotificationHub.Persistence.Entities.Application", "SmtpConfigurationId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("SmtpConfiguration");
                });

            modelBuilder.Entity("NotificationHub.Persistence.Entities.EmailSent", b =>
                {
                    b.HasOne("NotificationHub.Persistence.Entities.Application", "Application")
                        .WithMany("EmailSents")
                        .HasForeignKey("ApplicationId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.Navigation("Application");
                });

            modelBuilder.Entity("NotificationHub.Persistence.Entities.Application", b =>
                {
                    b.Navigation("EmailSents");
                });

            modelBuilder.Entity("NotificationHub.Persistence.Entities.SmtpConfiguration", b =>
                {
                    b.Navigation("Application");
                });
#pragma warning restore 612, 618
        }
    }
}
