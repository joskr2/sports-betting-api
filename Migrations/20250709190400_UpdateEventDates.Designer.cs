﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SportsBetting.Api.Infrastructure.Data;

#nullable disable

namespace SportsBetting.Api.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250709190400_UpdateEventDates")]
    partial class UpdateEventDates
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.7")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("SportsBetting.Api.Core.Entities.Bet", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Amount")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("EventId")
                        .HasColumnType("integer");

                    b.Property<decimal>("Odds")
                        .HasColumnType("decimal(10,2)");

                    b.Property<string>("SelectedTeam")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<int>("UserId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("CreatedAt")
                        .HasDatabaseName("IX_Bets_CreatedAt");

                    b.HasIndex("EventId")
                        .HasDatabaseName("IX_Bets_EventId");

                    b.HasIndex("Status")
                        .HasDatabaseName("IX_Bets_Status");

                    b.HasIndex("UserId")
                        .HasDatabaseName("IX_Bets_UserId");

                    b.ToTable("Bets");
                });

            modelBuilder.Entity("SportsBetting.Api.Core.Entities.Event", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.Property<DateTime>("EventDate")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(200)
                        .HasColumnType("character varying(200)");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("TeamA")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<decimal>("TeamAOdds")
                        .HasColumnType("decimal(10,2)");

                    b.Property<string>("TeamB")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<decimal>("TeamBOdds")
                        .HasColumnType("decimal(10,2)");

                    b.Property<DateTime>("UpdatedAt")
                        .HasColumnType("timestamp with time zone");

                    b.HasKey("Id");

                    b.HasIndex("EventDate")
                        .HasDatabaseName("IX_Events_EventDate");

                    b.HasIndex("Status")
                        .HasDatabaseName("IX_Events_Status");

                    b.ToTable("Events");

                    b.HasData(
                        new
                        {
                            Id = 1,
                            CreatedAt = new DateTime(2025, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc),
                            EventDate = new DateTime(2025, 7, 17, 0, 0, 0, 0, DateTimeKind.Utc),
                            Name = "Real Madrid vs Barcelona - El Clásico",
                            Status = "Upcoming",
                            TeamA = "Real Madrid",
                            TeamAOdds = 2.10m,
                            TeamB = "Barcelona",
                            TeamBOdds = 1.95m,
                            UpdatedAt = new DateTime(2025, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = 2,
                            CreatedAt = new DateTime(2025, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc),
                            EventDate = new DateTime(2025, 7, 15, 0, 0, 0, 0, DateTimeKind.Utc),
                            Name = "Manchester United vs Chelsea - Premier League",
                            Status = "Upcoming",
                            TeamA = "Manchester United",
                            TeamAOdds = 1.85m,
                            TeamB = "Chelsea",
                            TeamBOdds = 2.00m,
                            UpdatedAt = new DateTime(2025, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = 3,
                            CreatedAt = new DateTime(2025, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc),
                            EventDate = new DateTime(2025, 7, 20, 0, 0, 0, 0, DateTimeKind.Utc),
                            Name = "Liverpool vs Arsenal - Premier League",
                            Status = "Upcoming",
                            TeamA = "Liverpool",
                            TeamAOdds = 1.75m,
                            TeamB = "Arsenal",
                            TeamBOdds = 2.20m,
                            UpdatedAt = new DateTime(2025, 7, 10, 0, 0, 0, 0, DateTimeKind.Utc)
                        });
                });

            modelBuilder.Entity("SportsBetting.Api.Core.Entities.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<int>("Id"));

                    b.Property<decimal>("Balance")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("decimal(18,2)")
                        .HasDefaultValue(1000.00m);

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<string>("FullName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasDatabaseName("IX_Users_Email");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("SportsBetting.Api.Core.Entities.Bet", b =>
                {
                    b.HasOne("SportsBetting.Api.Core.Entities.Event", "Event")
                        .WithMany("Bets")
                        .HasForeignKey("EventId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("SportsBetting.Api.Core.Entities.User", "User")
                        .WithMany("Bets")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Event");

                    b.Navigation("User");
                });

            modelBuilder.Entity("SportsBetting.Api.Core.Entities.Event", b =>
                {
                    b.Navigation("Bets");
                });

            modelBuilder.Entity("SportsBetting.Api.Core.Entities.User", b =>
                {
                    b.Navigation("Bets");
                });
#pragma warning restore 612, 618
        }
    }
}
