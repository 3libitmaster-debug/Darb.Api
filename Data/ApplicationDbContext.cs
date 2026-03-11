using Microsoft.EntityFrameworkCore;
using Darb.Api.Models;

namespace darbWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- Database Sets (Tables) ---
        public DbSet<User> Users { get; set; }
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Advertisement> Advertisements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // 1. PROPERTY CONFIGURATIONS (Enums, Indexes, Constraints)
            // ============================================================

            // Convert UserRole Enum to String in Database
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            //modelBuilder.Entity<Trip>()
            //    .Property(t => t.BasePrice)
            //    .HasColumnType("decimal(18,2)"); 

            // Convert Subscription PlanType Enum to String in Database
            modelBuilder.Entity<Subscription>()
               .Property(u => u.PlanType)
               .HasConversion<string>();

            // Ensure Email uniqueness for security and login integrity
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Ensure Phone Number uniqueness for Passengers
            modelBuilder.Entity<Passenger>()
                .HasIndex(p => p.Phone)
                .IsUnique();

            // Ensure Company Name uniqueness to prevent duplicates
            modelBuilder.Entity<Company>()
                .HasIndex(p => p.Name)
                .IsUnique();


            // ============================================================
            // 2. RELATIONSHIP CONFIGURATIONS (One-to-One, One-to-Many)
            // ============================================================

            // --- Passenger & User Relationship (One-to-One) ---
            // A Passenger is a specialized type of User. Deleting a User deletes the Passenger profile.
            modelBuilder.Entity<Passenger>()
                .HasOne(p => p.User)
                .WithOne(u => u.Passenger)
                .HasForeignKey<Passenger>(p => p.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // --- Company & User Relationship (One-to-One) ---
            // A Company is a specialized type of User. Cascade delete ensures data cleanup.
            modelBuilder.Entity<Company>()
                .HasOne(c => c.User)
                .WithOne(u => u.Company)
                .HasForeignKey<Company>(p => p.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // --- Subscription & Company Relationship (One-to-Many) ---
            // A Company can have multiple subscription history records.
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Subscription)
                .HasForeignKey(s => s.CompanyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // --- Trip & Governorate Relationships (Self-Referencing lookups) ---
            // Restrict delete prevents deleting a Governorate if trips are assigned to it.
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.StartGovernate)
                .WithMany()
                .HasForeignKey(t => t.StartGoveId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Trip>()
                .HasOne(t => t.EndGovernate)
                .WithMany()
                .HasForeignKey(t => t.EndGoveId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- Bus & Company Relationship (One-to-Many) ---
            // Buses belong to a specific Transport Company.
            modelBuilder.Entity<Bus>()
                .HasOne(b => b.Company)
                .WithMany(c => c.Bus)
                .HasForeignKey(b => b.CompanyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // --- Trip & Company Relationship (One-to-Many) ---
            // Trips are managed and owned by a specific Company.
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Company)
                .WithMany(c => c.Trips)
                .HasForeignKey(t => t.CompanyId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // --- Trip & Bus Relationship ---
            // Linking a trip to a specific bus. NoAction to avoid circular delete issues.
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Bus)
                .WithMany(b => b.Trip)
                .HasForeignKey(t => t.BusId)
                .OnDelete(DeleteBehavior.NoAction);

            // --- Station & Geography Relationships ---
            // Each Station belongs to a Governorate.
            modelBuilder.Entity<Station>()
                .HasOne(s => s.Governorate)
                .WithMany(g => g.Station)
                .HasForeignKey(s => s.GovernorateId)
                .OnDelete(DeleteBehavior.Restrict);

            // Each Station is managed/owned by a Company.
            modelBuilder.Entity<Station>()
                .HasOne(s => s.Company)
                .WithMany(c => c.Station)
                .HasForeignKey(s => s.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // --- City & Governorate Relationship ---
            // Cities are grouped under Governorates.
            modelBuilder.Entity<City>()
                .HasOne(c => c.Governorate)
                .WithMany(g => g.City)
                .HasForeignKey(c => c.GovernorateId)
                .OnDelete(DeleteBehavior.Restrict);



            // --- Advertisement & User Relationships ---
            // An advertisement is created by a user and owned by a user (Admin).
            // Prevent multiple cascade paths
            modelBuilder.Entity<Advertisement>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Advertisement>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}