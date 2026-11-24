using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public class LibraryDbContext : DbContext
    {
        // Fixed UTC timestamp for deterministic, repeatable migrations/seed data.
        private static readonly DateTime SeedTimestamp =
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public LibraryDbContext(
            DbContextOptions<LibraryDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BookAuthor> BookAuthors { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<MembershipType> MembershipTypes { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<LibrarySetting> LibrarySettings { get; set; }

        /// <summary>
        /// Project-wide convention: every DateTime / DateTime? is stored as Unix
        /// seconds (bigint) and read back as UTC. DateOnly properties are not
        /// affected, so loan/membership dates remain SQL `date` columns.
        /// </summary>
        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<DateTime>()
                .HaveConversion<UnixTimeConverter>();

            configurationBuilder.Properties<DateTime?>()
                .HaveConversion<NullableUnixTimeConverter>();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =====================================
            // 1. PRIMARY KEYS
            // =====================================

            modelBuilder.Entity<BookAuthor>()
                .HasKey(ba => new { ba.BookId, ba.AuthorId });

            // =====================================
            // 2. FOREIGN KEY RELATIONSHIPS
            // =====================================

            modelBuilder.Entity<Book>()
               .HasOne(b => b.Category)
               .WithMany()
               .HasForeignKey(b => b.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookAuthor>()
                .HasOne(ba => ba.Book)
                .WithMany()
                .HasForeignKey(ba => ba.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookAuthor>()
                .HasOne(ba => ba.Author)
                .WithMany()
                .HasForeignKey(ba => ba.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Membership>()
                .HasOne(m => m.Member)
                .WithMany()
                .HasForeignKey(m => m.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Membership>()
                .HasOne(m => m.MembershipType)
                .WithMany()
                .HasForeignKey(m => m.MembershipTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Book)
                .WithMany()
                .HasForeignKey(l => l.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Member)
                .WithMany()
                .HasForeignKey(l => l.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Book)
                .WithMany()
                .HasForeignKey(r => r.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Reservation>()
                .HasOne(r => r.Member)
                .WithMany()
                .HasForeignKey(r => r.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================
            // 3. UNIQUE CONSTRAINTS / COLUMN TYPES
            // =====================================

            modelBuilder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            modelBuilder.Entity<Book>()
                .HasIndex(b => b.ISBN)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            modelBuilder.Entity<MembershipType>()
                .HasIndex(mt => mt.Name)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            // Case-insensitive email lookups/uniqueness.
            modelBuilder.HasPostgresExtension("citext");

            modelBuilder.Entity<Member>()
                .Property(m => m.Email)
                .HasColumnType("citext");

            // Soft-deleted members release their email address for reuse.
            modelBuilder.Entity<Member>()
                .HasIndex(m => m.Email)
                .IsUnique()
                .HasFilter("\"DeletedAt\" IS NULL");

            // NOTE: [Required] and [MaxLength] live on the model classes as
            // DataAnnotations. EF Core picks them up by convention, so they are
            // not repeated here. This section only holds relational concerns.

            // =====================================
            // 4. PERFORMANCE INDEXES
            // =====================================

            modelBuilder.Entity<BookAuthor>()
                .HasIndex(ba => ba.AuthorId);

            modelBuilder.Entity<Membership>()
                .HasIndex(m => new { m.MemberId, m.Status });

            // =====================================
            // 5. CHECK CONSTRAINTS
            // =====================================

            modelBuilder.Entity<Member>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Member_Email_Valid",
                    "char_length(trim(\"Email\")) > 0 AND char_length(\"Email\") <= 150"));

            modelBuilder.Entity<Book>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Book_TotalCopies",
                    "\"TotalCopies\" >= 0"));

            modelBuilder.Entity<MembershipType>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint("CK_MembershipType_DurationMonths", "\"DurationMonths\" > 0");
                    t.HasCheckConstraint("CK_MembershipType_Fee", "\"Fee\" >= 0");
                    t.HasCheckConstraint("CK_MembershipType_BorrowLimit", "\"BorrowLimit\" > 0");
                    t.HasCheckConstraint("CK_MembershipType_LoanPeriodDays", "\"LoanPeriodDays\" > 0");
                });

            modelBuilder.Entity<Membership>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Membership_EndDate", "\"EndDate\" >= \"StartDate\"");
                    t.HasCheckConstraint("CK_Membership_PaymentStatus", "\"PaymentStatus\" IN ('Paid', 'Pending')");
                    t.HasCheckConstraint("CK_Membership_Status", "\"Status\" IN ('Active', 'Expired', 'Cancelled')");
                });

            modelBuilder.Entity<Loan>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Loan_DueDate", "\"DueDate\" >= \"IssueDate\"");
                    t.HasCheckConstraint("CK_Loan_ReturnDate", "\"ReturnDate\" IS NULL OR \"ReturnDate\" >= \"IssueDate\"");
                    t.HasCheckConstraint("CK_Loan_FineAmount", "\"FineAmount\" >= 0");
                    t.HasCheckConstraint("CK_Loan_FinePaidAmount", "\"FinePaidAmount\" >= 0 AND \"FinePaidAmount\" <= \"FineAmount\"");
                });

            modelBuilder.Entity<Reservation>()
                .ToTable(t => t.HasCheckConstraint(
                    "CK_Reservation_Status",
                    "\"Status\" IN ('Pending', 'Fulfilled', 'Cancelled', 'Expired')"));

            modelBuilder.Entity<LibrarySetting>()
                .ToTable(t =>
                {
                    t.HasCheckConstraint("CK_LibrarySetting_ReservationHoldDays", "\"ReservationHoldDays\" > 0");
                    t.HasCheckConstraint("CK_LibrarySetting_FinePerDay", "\"FinePerDay\" >= 0");
                    t.HasCheckConstraint("CK_LibrarySetting_MaximumFine", "\"MaximumFine\" >= 0");
                    t.HasCheckConstraint("CK_LibrarySetting_FineBorrowingLimit", "\"FineBorrowingLimit\" >= 0");
                });

            // =====================================
            // 6. DECIMAL PRECISION
            // =====================================

            modelBuilder.Entity<MembershipType>().Property(mt => mt.Fee).HasPrecision(10, 2);
            modelBuilder.Entity<Loan>().Property(l => l.FineAmount).HasPrecision(8, 2);
            modelBuilder.Entity<Loan>().Property(l => l.FinePaidAmount).HasPrecision(8, 2);
            modelBuilder.Entity<LibrarySetting>().Property(ls => ls.FinePerDay).HasPrecision(8, 2);
            modelBuilder.Entity<LibrarySetting>().Property(ls => ls.MaximumFine).HasPrecision(8, 2);
            modelBuilder.Entity<LibrarySetting>().Property(ls => ls.FineBorrowingLimit).HasPrecision(8, 2);

            // =====================================
            // 7. DEFAULT VALUES
            // =====================================

            modelBuilder.Entity<Book>().Property(b => b.TotalCopies).HasDefaultValue(0);
            modelBuilder.Entity<Member>().Property(m => m.IsActive).HasDefaultValue(true);
            modelBuilder.Entity<MembershipType>().Property(mt => mt.IsActive).HasDefaultValue(true);
            modelBuilder.Entity<Membership>().Property(m => m.PaymentStatus).HasDefaultValue("Pending");
            modelBuilder.Entity<Membership>().Property(m => m.Status).HasDefaultValue("Active");
            modelBuilder.Entity<Loan>().Property(l => l.FineAmount).HasDefaultValue(0m);
            modelBuilder.Entity<Loan>().Property(l => l.FinePaidAmount).HasDefaultValue(0m);
            modelBuilder.Entity<Reservation>().Property(r => r.Status).HasDefaultValue("Pending");
            modelBuilder.Entity<LibrarySetting>().Property(ls => ls.ReservationHoldDays).HasDefaultValue(2);
            modelBuilder.Entity<LibrarySetting>().Property(ls => ls.FinePerDay).HasDefaultValue(5m);
            modelBuilder.Entity<LibrarySetting>().Property(ls => ls.MaximumFine).HasDefaultValue(200m);
            modelBuilder.Entity<LibrarySetting>().Property(ls => ls.FineBorrowingLimit).HasDefaultValue(100m);

            // =====================================
            // 8. BUSINESS-RULE INDEXES
            // =====================================

            // BR-06: a member cannot hold two copies of the same book at once.
            modelBuilder.Entity<Loan>()
                .HasIndex(l => new { l.BookId, l.MemberId })
                .IsUnique()
                .HasFilter("\"ReturnDate\" IS NULL AND \"DeletedAt\" IS NULL");

            // One pending reservation per member per book.
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.BookId, r.MemberId })
                .IsUnique()
                .HasFilter("\"Status\" = 'Pending' AND \"DeletedAt\" IS NULL");

            // BR-07: first come, first served queue.
            modelBuilder.Entity<Reservation>()
                .HasIndex(r => new { r.BookId, r.ReservedAt, r.ReservationId })
                .HasFilter("\"Status\" = 'Pending' AND \"DeletedAt\" IS NULL");

            // =====================================
            // 9. SOFT DELETE QUERY FILTERS (BR-09)
            // =====================================

            // Entities with no parent dependency: own DeletedAt only.
            modelBuilder.Entity<Category>()
                .HasQueryFilter(e => e.DeletedAt == null);

            modelBuilder.Entity<Author>()
                .HasQueryFilter(e => e.DeletedAt == null);

            modelBuilder.Entity<MembershipType>()
                .HasQueryFilter(e => e.DeletedAt == null);

            // LibrarySetting is deliberately excluded: it is a single editable
            // config row with no delete flow, so it has no DeletedAt column.

            modelBuilder.Entity<Member>()
                .HasQueryFilter(e => e.DeletedAt == null);

            // Book is the required dependent of Category, so a live book
            // must not survive its category being soft-deleted.
            modelBuilder.Entity<Book>()
                .HasQueryFilter(b => b.DeletedAt == null
                    && b.Category.DeletedAt == null);

            // Join row: hidden if the link itself, its book, or its author
            // was soft-deleted.
            modelBuilder.Entity<BookAuthor>()
                .HasQueryFilter(ba => ba.DeletedAt == null
                    && ba.Book.DeletedAt == null
                    && ba.Author.DeletedAt == null);

            modelBuilder.Entity<Loan>()
                .HasQueryFilter(l => l.DeletedAt == null
                    && l.Book.DeletedAt == null
                    && l.Member.DeletedAt == null);

            modelBuilder.Entity<Reservation>()
                .HasQueryFilter(r => r.DeletedAt == null
                    && r.Book.DeletedAt == null
                    && r.Member.DeletedAt == null);

            modelBuilder.Entity<Membership>()
                .HasQueryFilter(m => m.DeletedAt == null
                    && m.Member.DeletedAt == null
                    && m.MembershipType.DeletedAt == null);

            // =====================================
            // 10. SEED DATA
            // =====================================

            modelBuilder.Entity<MembershipType>().HasData(
                new MembershipType
                {
                    MembershipTypeId = 1,
                    Name = "Regular",
                    DurationMonths = 12,
                    Fee = 0m,
                    BorrowLimit = 3,
                    LoanPeriodDays = 14,
                    IsActive = true,
                    CreatedAt = SeedTimestamp
                },
                new MembershipType
                {
                    MembershipTypeId = 2,
                    Name = "Premium",
                    DurationMonths = 12,
                    Fee = 500m,
                    BorrowLimit = 5,
                    LoanPeriodDays = 21,
                    IsActive = true,
                    CreatedAt = SeedTimestamp
                });

            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    CategoryId = 1,
                    Name = "Fiction",
                    Description = "Fictional works",
                    CreatedAt = SeedTimestamp
                },
                new Category
                {
                    CategoryId = 2,
                    Name = "Technology",
                    Description = "Computing and engineering",
                    CreatedAt = SeedTimestamp
                },
                new Category
                {
                    CategoryId = 3,
                    Name = "History",
                    Description = "Historical non-fiction",
                    CreatedAt = SeedTimestamp
                });

            modelBuilder.Entity<Author>().HasData(
                new Author
                {
                    AuthorId = 1,
                    FullName = "Jane Austen",
                    Bio = "English novelist",
                    CreatedAt = SeedTimestamp
                },
                new Author
                {
                    AuthorId = 2,
                    FullName = "Robert C. Martin",
                    Bio = "Software engineer and author",
                    CreatedAt = SeedTimestamp
                },
                new Author
                {
                    AuthorId = 3,
                    FullName = "Yuval Noah Harari",
                    Bio = "Historian and author",
                    CreatedAt = SeedTimestamp
                });

            modelBuilder.Entity<Book>().HasData(
                new Book
                {
                    BookId = 1,
                    Title = "Pride and Prejudice",
                    ISBN = "9780141439518",
                    CategoryId = 1,
                    TotalCopies = 3,
                    CreatedAt = SeedTimestamp
                },
                new Book
                {
                    BookId = 2,
                    Title = "Clean Code",
                    ISBN = "9780132350884",
                    CategoryId = 2,
                    TotalCopies = 2,
                    CreatedAt = SeedTimestamp
                },
                new Book
                {
                    BookId = 3,
                    Title = "Sapiens",
                    ISBN = "9780062316097",
                    CategoryId = 3,
                    TotalCopies = 1,
                    CreatedAt = SeedTimestamp
                });

            modelBuilder.Entity<BookAuthor>().HasData(
                new BookAuthor { BookId = 1, AuthorId = 1, CreatedAt = SeedTimestamp },
                new BookAuthor { BookId = 2, AuthorId = 2, CreatedAt = SeedTimestamp },
                new BookAuthor { BookId = 3, AuthorId = 3, CreatedAt = SeedTimestamp });

            modelBuilder.Entity<Member>().HasData(
                new Member
                {
                    MemberId = 1,
                    FullName = "Aarav Sharma",
                    Email = "aarav@example.com",
                    Phone = "9876543210",
                    IsActive = true,
                    CreatedAt = SeedTimestamp
                },
                new Member
                {
                    MemberId = 2,
                    FullName = "Priya Patel",
                    Email = "priya@example.com",
                    Phone = "9876543211",
                    IsActive = true,
                    CreatedAt = SeedTimestamp
                });

            modelBuilder.Entity<LibrarySetting>().HasData(
                new LibrarySetting
                {
                    LibrarySettingId = 1,
                    ReservationHoldDays = 2,
                    FinePerDay = 5m,
                    MaximumFine = 200m,
                    FineBorrowingLimit = 100m,
                    CreatedAt = SeedTimestamp
                });
        }
    }
}
