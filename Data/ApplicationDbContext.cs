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
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<CompanySubscription> CompanySubscription { get; set; }
        public DbSet<Governorate> Governorates { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Station> Stations { get; set; }
        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<Bank> Banks { get; set; }
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<TripSchedule> TripSchedules { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Passenger> Passenger { get; set; }
        public DbSet<ETicket> ETickets { get; set; }
        public DbSet<TripFare> TripFares { get; set; }
        public DbSet<Review> Review { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // 1. PROPERTY CONFIGURATIONS (Enums, Indexes, Constraints)
            // ============================================================

            // Convert AccountRole Enum to String in Database
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            // Convert Subscription PlanType Enum to String in Database
            modelBuilder.Entity<CompanySubscription>()
               .Property(u => u.PlanType)
               .HasConversion<string>();

            // Ensure Email uniqueness for security and login integrity
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Ensure Phone Number uniqueness for Customers
            modelBuilder.Entity<Customer>()
                .HasIndex(p => p.Phone)
                .IsUnique();

            // Ensure Company Name uniqueness to prevent duplicates
            modelBuilder.Entity<Company>()
                .HasIndex(p => p.Name)
                .IsUnique();


            // ============================================================
            // 2. RELATIONSHIP CONFIGURATIONS (One-to-One, One-to-Many)
            // ============================================================

            // --- Customer & User Relationship (One-to-One) ---
            // A Customer is a specialized type of User. Deleting a User deletes the Customer profile.
            modelBuilder.Entity<Customer>()
                .HasOne(p => p.User)
                .WithOne(u => u.Customer)
                .HasForeignKey<Customer>(p => p.UserId)
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
            modelBuilder.Entity<CompanySubscription>()
                .HasOne(s => s.Company)
                .WithMany(c => c.CompanySubscription)
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

            // --- TripFare & Governorate Relationships ---
            modelBuilder.Entity<TripFare>()
                .HasOne(tf => tf.FromGovernorate)
                .WithMany()
                .HasForeignKey(tf => tf.FromGovId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripFare>()
                .HasOne(tf => tf.ToGovernorate)
                .WithMany()
                .HasForeignKey(tf => tf.ToGovId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripFare>()
                .HasOne(tf => tf.Station)
                .WithMany()
                .HasForeignKey(tf => tf.StationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripFare>()
                .HasOne(tf => tf.Company)
                .WithMany()
                .HasForeignKey(tf => tf.CompanyId)
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


            modelBuilder.Entity<Review>()
                .HasOne(p => p.Customer)
                .WithMany(r => r.Review)
                .HasForeignKey(p => p.CustomerId);

     



            // --- Advertisement & User Relationships ---
            // An advertisement is created by a User and owned by a User (Admin).
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

            // --- Bank & BankAccount Relationships ---
            modelBuilder.Entity<BankAccount>()
                .HasOne(ba => ba.Bank)
                .WithMany(b => b.BankAccounts)
                .HasForeignKey(ba => ba.BankId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<BankAccount>()
                .HasOne(ba => ba.Company)
                .WithMany(c => c.BankAccounts)
                .HasForeignKey(ba => ba.CompanyId)
                .OnDelete(DeleteBehavior.Cascade); 

            // --- Booking & ETicket Relationship (One-to-One) ---
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.ETicket)
                .WithOne(e => e.Booking)
                .HasForeignKey<ETicket>(e => e.BookingId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // --- Booking Relationships (Prevent multiple cascade paths) ---
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany()
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.TripSchedule)
                .WithMany()
                .HasForeignKey(b => b.TripScheduleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripSchedule>()
                .HasOne(tr => tr.Trip)
                .WithMany(t => t.TripSchedules)
                .HasForeignKey(tr => tr.TripId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TripSchedule>()
                .HasOne(tr => tr.Station)
                .WithMany()
                .HasForeignKey(tr => tr.StationId)
                .OnDelete(DeleteBehavior.Restrict);


            // Review relationship
            modelBuilder.Entity<Review>()
               .HasOne(p => p.Customer)
               .WithMany(r => r.Review)
               .HasForeignKey(p => p.CustomerId)
               .OnDelete(DeleteBehavior.Restrict); // Â‰« ⁄ÿ·‰« «·Õ–› «· ·ﬁ«∆Ì ··„”«›—

            modelBuilder.Entity<Review>()
                .HasOne(p => p.Company)
                .WithMany(r => r.Review)
                .HasForeignKey(p => p.CompanyId)
                .OnDelete(DeleteBehavior.Cascade); // « —ﬂ Â–« ≈–« √—œ  Õ–› «·„—«Ã⁄… ⁄‰œ Õ–› «·‘—ﬂ…

        }
    }
}