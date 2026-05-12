using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SearchTool_ServerSide.Authentication;
using SearchTool_ServerSide.Models;
using ServerSide.Models;
using Newtonsoft.Json;
namespace SearchTool_ServerSide.Data
{
    public class GlobalDBContext : DbContext
    {
        public GlobalDBContext(DbContextOptions<GlobalDBContext> options) : base(options) { }
        public DbSet<Drug> Drugs { get; set; }
        public DbSet<DrugClass> DrugClasses { get; set; }
        public DbSet<DrugInsurance> DrugInsurances { get; set; }
        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<Script> Scripts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ClassInsurance> ClassInsurances { get; set; }
        public DbSet<ScriptItem> ScriptItems { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<DrugBranch> DrugBranches { get; set; }
        public DbSet<MainCompany> MainCompanies { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<InsuranceRx> InsuranceRxes { get; set; }
        public DbSet<InsurancePCN> InsurancePCNs { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<SearchLog> SearchLogs { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        public DbSet<RolePermission> RolePermissions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define Composite Keys
            modelBuilder.Entity<DrugInsurance>(entity =>
            {
                entity.HasKey(item => new { item.InsuranceId, item.DrugId, item.BranchId });
            });

            modelBuilder.Entity<ClassInsurance>(entity =>
            {
                entity.HasKey(ci => new { ci.InsuranceId, ci.ClassInfoId, ci.Date, ci.BranchId });

                // Define foreign key relationships
                entity.HasOne(ci => ci.Insurance)
                    .WithMany()
                    .HasForeignKey(ci => ci.InsuranceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.ClassInfo)
                    .WithMany()
                    .HasForeignKey(ci => ci.ClassInfoId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ci => ci.Branch)
                .WithMany()
                .HasForeignKey(ci => ci.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DrugBranch>(entity =>
            {
                entity.HasKey(db => new { db.DrugNDC, db.BranchId });

                // Relationships
                entity.HasOne(db => db.Drug)
                      .WithMany() // optionally `.WithMany(d => d.DrugBranches)` if you add navigation in Drug
                      .HasForeignKey(db => db.DrugNDC)
                      .HasPrincipalKey(d => d.NDC) // since DrugNDC links to NDC, not Id
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(db => db.Branch)
                      .WithMany() // optionally `.WithMany(b => b.DrugBranches)` if you add navigation in Branch
                      .HasForeignKey(db => db.BranchId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Specialty>().HasData(
                          new { Id = 1, Name = "Dermatology specialty", }
                      );
            // Seed Data for Branches
            modelBuilder.Entity<MainCompany>().HasData(
               new { Id = 1, Name = "California Dermatology", SpecialtyId = 1 }

           );
            modelBuilder.Entity<Branch>().HasData(
                new Branch { Id = 1, Name = "California Dermatology Institute Thousand Oaks", Location = "Thousand Oaks", Code = "1", MainCompanyId = 1 },
                new Branch { Id = 2, Name = "California Dermatology Institute Northridge", Location = "Northridge", Code = "2", MainCompanyId = 1 },
                new Branch { Id = 3, Name = "California Dermatology Institute Huntington Park", Location = "Huntington Park", Code = "3", MainCompanyId = 1 },
                new Branch { Id = 4, Name = "California Dermatology Institute Palmdale", Location = "Palmdale", Code = "4", MainCompanyId = 1 },
                new Branch { Id = 5, Name = "VIRTUAL", Location = "VIRTUAL", Code = "5", MainCompanyId = 1 }

            );
        }
    }
   
    public class SearchToolDBContext : DbContext
    {
        private readonly UserAccessToken _userAccessToken;

        public SearchToolDBContext(DbContextOptions<SearchToolDBContext> options) : base(options)
        {
        }
        public SearchToolDBContext(DbContextOptions<SearchToolDBContext> options, UserAccessToken userAccessToken) : base(options)
        {
            _userAccessToken = userAccessToken;
        }

        public DbSet<Drug> Drugs { get; set; }
        public DbSet<DrugClass> DrugClasses { get; set; }
        public DbSet<ClassInfo> ClassInfos { get; set; }
        public DbSet<DrugInsurance> DrugInsurances { get; set; }
        public DbSet<Insurance> Insurances { get; set; }
        public DbSet<Script> Scripts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ClassInsurance> ClassInsurances { get; set; }
        public DbSet<ScriptItem> ScriptItems { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<DrugBranch> DrugBranches { get; set; }
        public DbSet<MainCompany> MainCompanies { get; set; }
        public DbSet<Specialty> Specialties { get; set; }
        public DbSet<InsuranceRx> InsuranceRxes { get; set; }
        public DbSet<InsurancePCN> InsurancePCNs { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<SearchLog> SearchLogs { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<DrugMedi> DrugMedis { get; set; }
        public DbSet<ClassType> ClassTypes { get; set; }
        public DbSet<SearchDrugDetailsLogs> DrugModals { get; set; }
        public DbSet<FeedbackFormEntry> FeedbackForms { get; set; }
        public DbSet<SectionEntry> Sections { get; set; }
        public DbSet<QuestionEntry> Questions { get; set; }
        public DbSet<InsuranceStatus> InsuranceStatuses { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<DrugAlternativeStatus> DrugAlternativeStatuses { get; set; }
        public DbSet<DrugAlternativeReport> DrugAlternativeReports { get; set; }
        public DbSet<DrugDiseaseAddHistory> DrugDiseaseAddHistories { get; set; }
        public DbSet<Disease> Diseases { get; set; }
        public DbSet<DiseaseVisibilitySettings> DiseaseVisibilitySettings { get; set; }
        public DbSet<UserDiseaseVisibility> UserDiseaseVisibility { get; set; }

        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Wholesaler> Wholesalers { get; set; }
        public DbSet<DrugWholesalerPrescriber> DrugWholesalerPrescribers { get; set; }

        // New DbSets for Permissions


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

          
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<DiseaseVisibilitySettings>().HasData(new DiseaseVisibilitySettings
    {
        Id = 1,
        Mode = SearchTool_ServerSide.Models.Enums.DiseaseVisibilityMode.AllDoctors,
        UpdatedAt = DateTime.UtcNow,
        UpdatedByUserId = null
    });


modelBuilder.Entity<UserDiseaseVisibility>()
    .HasIndex(x => new { x.UserId, x.DiseaseId })
    .IsUnique();

modelBuilder.Entity<UserDiseaseVisibility>()
    .HasOne(x => x.User)
    .WithMany()
    .HasForeignKey(x => x.UserId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<UserDiseaseVisibility>()
    .HasOne(x => x.Disease)
    .WithMany()
    .HasForeignKey(x => x.DiseaseId)
    .OnDelete(DeleteBehavior.Cascade);




          // Add index for disease name filtering
             modelBuilder.Entity<Disease>()
            .HasIndex(d => d.Name);




            modelBuilder.Entity<Chat>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.UserId).IsRequired();

                e.HasMany(x => x.Messages)
                    .WithOne(x => x.Chat)
                    .HasForeignKey(x => x.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Message>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Role).IsRequired().HasMaxLength(10);
                e.Property(x => x.Text).IsRequired();

                e.HasIndex(x => new { x.ChatId, x.Timestamp });
            });
            modelBuilder.Entity<DrugDiseaseAddHistory>(b =>
            {
                b.HasKey(dd => dd.Id);
                b.HasIndex(dd => new { dd.DrugId, dd.DiseaseId, dd.UserId })
                .IsUnique();

            });
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<InsuranceStatus>(b =>
      {
          // NEW: composite PK
          b.HasKey(i => new { i.SourceDrugNDC, i.TargetDrugNDC, i.InsuranceRxId });

          // keep your old relationships:
          b.HasOne(i => i.SourceDrug)
              .WithMany()
              .HasForeignKey(i => i.SourceDrugNDC)
              .HasPrincipalKey(d => d.NDC)
              .OnDelete(DeleteBehavior.Restrict);

          b.HasOne(i => i.TargetDrug)
              .WithMany()
              .HasForeignKey(i => i.TargetDrugNDC)
              .HasPrincipalKey(d => d.NDC)
              .OnDelete(DeleteBehavior.Restrict);

          b.HasOne(i => i.InsuranceRx)
              .WithMany()
              .HasForeignKey(i => i.InsuranceRxId)
              // Consider Restrict/NoAction to avoid accidental cascades;
              // keep Cascade if you're sure it's desired.
              .OnDelete(DeleteBehavior.Restrict);

          // NEW: Reports relationship
          b.HasMany(i => i.Reports)
              .WithOne(r => r.InsuranceStatus)
              .HasForeignKey(r => new { r.SourceDrugNDC, r.TargetDrugNDC, r.InsuranceRxId })
              .OnDelete(DeleteBehavior.Cascade);
      });
            modelBuilder.Entity<Report>(b =>
         {
             b.HasKey(r => r.Id);

             b.HasIndex(r => new { r.InsuranceRxId, r.SourceDrugNDC, r.TargetDrugNDC, r.StatusDate });

             b.HasOne(r => r.User)
                 .WithMany()
                 .HasForeignKey(r => r.UserEmail)
                 .HasPrincipalKey(u => u.Email)
                 .OnDelete(DeleteBehavior.SetNull);
         });
            modelBuilder.Entity<DrugAlternativeStatus>(b =>
            {
                // NEW: composite PK
                b.HasKey(i => new { i.SourceDrugNDC, i.TargetDrugNDC, i.ClassInfoId });

                // keep your old relationships:
                b.HasOne(i => i.SourceDrug)
                    .WithMany()
                    .HasForeignKey(i => i.SourceDrugNDC)
                    .HasPrincipalKey(d => d.NDC)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(i => i.TargetDrug)
                    .WithMany()
                    .HasForeignKey(i => i.TargetDrugNDC)
                    .HasPrincipalKey(d => d.NDC)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(i => i.ClassInfo)
                    .WithMany()
                    .HasForeignKey(i => i.ClassInfoId)
                    // Consider Restrict/NoAction to avoid accidental cascades;
                    // keep Cascade if you're sure it's desired.
                    .OnDelete(DeleteBehavior.Restrict);

                // NEW: Reports relationship
                b.HasMany(i => i.Reports)
                    .WithOne(r => r.DrugAlternativeStatus)
                    .HasForeignKey(r => new { r.SourceDrugNDC, r.TargetDrugNDC, r.ClassInfoId })
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DrugAlternativeReport>(b =>
            {
                b.HasKey(r => r.Id);

                b.HasIndex(r => new { r.ClassInfoId, r.SourceDrugNDC, r.TargetDrugNDC, r.StatusDate });

                b.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserEmail)
                    .HasPrincipalKey(u => u.Email)
                    .OnDelete(DeleteBehavior.SetNull);
            });
            modelBuilder.Entity<SearchDrugDetailsLogs>(entity =>
            {
                entity.HasKey(e => new { e.UserEmail, e.NDC });
                entity.HasOne(e => e.User)
                    .WithMany(u => u.SearchDrugDetailsLogs)
                    .HasForeignKey(e => e.UserEmail)
                    .HasPrincipalKey(u => u.Email)
                    .OnDelete(DeleteBehavior.Cascade); // Or Restrict   
                entity.HasOne(e => e.Drug)
                    .WithMany(d => d.SearchDrugDetailsLogs)
                    .HasForeignKey(e => e.NDC)
                    .HasPrincipalKey(d => d.NDC)
                    .OnDelete(DeleteBehavior.Cascade); // Or Restrict
            });
            modelBuilder.Entity<QuestionEntry>()
                        .Property(q => q.SelectedAnswersJson)
                        .HasColumnType("jsonb");
            // Define Composite Keys
            modelBuilder.Entity<DrugClass>(entity =>
            {
                entity.HasKey(dc => new { dc.DrugId, dc.ClassId });

                // Define foreign key relationships
                entity.HasOne(dc => dc.Drug)
                    .WithMany(d => d.DrugClasses)
                    .HasForeignKey(dc => dc.DrugId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(dc => dc.ClassInfo)
                    .WithMany(c => c.DrugClasses)
                    .HasForeignKey(dc => dc.ClassId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DrugInsurance>(entity =>
            {
                entity.HasKey(item => new { item.InsuranceId, item.DrugId, item.BranchId });
            });

            modelBuilder.Entity<ClassInsurance>(entity =>
            {
                entity.HasKey(ci => new { ci.InsuranceId, ci.ClassInfoId, ci.Date, ci.BranchId });

                // Define foreign key relationships
                entity.HasOne(ci => ci.Insurance)
                    .WithMany()
                    .HasForeignKey(ci => ci.InsuranceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.ClassInfo)
                    .WithMany()
                    .HasForeignKey(ci => ci.ClassInfoId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ci => ci.Branch)
                .WithMany()
                .HasForeignKey(ci => ci.BranchId)
                .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<DrugBranch>(entity =>
            {
                entity.HasKey(db => new { db.DrugNDC, db.BranchId });

                // Relationships
                entity.HasOne(db => db.Drug)
                      .WithMany() // optionally `.WithMany(d => d.DrugBranches)` if you add navigation in Drug
                      .HasForeignKey(db => db.DrugNDC)
                      .HasPrincipalKey(d => d.NDC) // since DrugNDC links to NDC, not Id
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(db => db.Branch)
                      .WithMany() // optionally `.WithMany(b => b.DrugBranches)` if you add navigation in Branch
                      .HasForeignKey(db => db.BranchId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<ClassType>().HasData(new { Id = 1, Name = "TestClass", Description = "Just for Testing" });
            modelBuilder.Entity<Specialty>().HasData(
                          new { Id = 1, Name = "Dermatology specialty" }
                      );
            // Seed Data for Branches
            modelBuilder.Entity<MainCompany>().HasData(
               new { Id = 1, Name = "California Dermatology", SpecialtyId = 1, ClassTypeId = 1 },
               new { Id = 2, Name = "Spark Medi-Cal", SpecialtyId = 1, ClassTypeId = 1 }
           );
            modelBuilder.Entity<Branch>().HasData(
                new Branch { Id = 1, Name = "California Dermatology Institute Thousand Oaks", Location = "Thousand Oaks", Code = "1", MainCompanyId = 1 },
                new Branch { Id = 2, Name = "California Dermatology Institute Northridge", Location = "Northridge", Code = "2", MainCompanyId = 1 },
                new Branch { Id = 3, Name = "California Dermatology Institute Huntington Park", Location = "Huntington Park", Code = "3", MainCompanyId = 1 },
                new Branch { Id = 4, Name = "California Dermatology Institute Palmdale", Location = "Palmdale", Code = "4", MainCompanyId = 1 },
                new Branch { Id = 5, Name = "VIRTUAL", Location = "VIRTUAL", Code = "5", MainCompanyId = 1 },
                new Branch { Id = 6, Name = "ASP", Location = "VIRTUAL", Code = "6", MainCompanyId = 2 }

            );
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserEmail)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Action)
                    .IsRequired();

                entity.Property(e => e.Date)
                    .HasDefaultValueSql("NOW()"); // For PostgreSQL

                // Foreign key relationship: UserEmail -> User.Email
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Logs)
                    .HasForeignKey(e => e.UserEmail)
                    .HasPrincipalKey(u => u.Email)
                    .OnDelete(DeleteBehavior.Cascade); // Or Restrict
            });
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserEmail)
                    .IsRequired()
                    .HasMaxLength(255);
                entity.Property(e => e.Date)
                    .HasDefaultValueSql("NOW()"); // PostgreSQL or use GETUTCDATE() for SQL Server
                entity.Property(e => e.TotalNet).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPatientPay).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalInsurancePay).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalAcquisitionCost).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AddtionalCost).HasColumnType("decimal(18,2)");

                // UserEmail -> User.Email relationship


                // UserEmail -> User.Email relationship

            });
            modelBuilder.Entity<User>(entity =>
                {
                    entity.HasKey(u => u.Id);
                    entity.HasIndex(u => u.Email).IsUnique(); // Required for .HasPrincipalKey
                });

            // for permissions
            //تعريف العلاقات والقواعد   

            modelBuilder.Entity<Permission>(entity =>
   {
       entity.ToTable("permissions");

       entity.HasKey(p => p.Id);

       entity.Property(p => p.Name)
           .IsRequired()
           .HasMaxLength(200);

       entity.HasIndex(p => p.Name).IsUnique();

       entity.Property(p => p.Description)
           .HasMaxLength(1000);
   });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("role_permissions");

                entity.HasKey(rp => new { rp.Role, rp.PermissionId }); // Composite Primary Key

                entity.Property(rp => rp.Role)
                    .HasConversion<int>(); // enum -> int

                entity.HasOne(rp => rp.Permission)
                    .WithMany(p => p.RolePermissions)
                    .HasForeignKey(rp => rp.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // for userpermissions as up
            /*
              modelBuilder.Entity<UserPermission>(entity =>
              {

                 entity.HasKey(up => new { up.UserId, up.PermissionId }); // Composite Primary Key

                 entity.HasOne(up => up.User) // each UserPermission has one User
                 .WithMany(u => u.UserPermissions)   // each User has many UserPermissions
                 .HasForeignKey(up => up.UserId)    // Foreign Key
                 .OnDelete(DeleteBehavior.Cascade);  // When a User is deleted, delete related UserPermissions

                 entity.HasOne(up => up.Permission)   // each UserPermission has one Permission   
                 .WithMany(p => p.UserPermissions)    // each Permission has many UserPermissions
                 .HasForeignKey(up => up.PermissionId)  // Foreign Key
                 .OnDelete(DeleteBehavior.Cascade);    // When a Permission is deleted, delete related UserPermissions



              });
     */

            modelBuilder.Entity<OrderItem>(entity =>
            {
                // DrugId → Drug.Id
                entity.HasOne(e => e.Drug)
                    .WithMany()
                    .HasForeignKey(e => e.DrugNDC)
                    .HasPrincipalKey(e => e.NDC)
                    .OnDelete(DeleteBehavior.Restrict);
            });



            modelBuilder.Entity<ScriptItem>(entity =>
            {
                entity.HasOne(e => e.Prescriber)
                    .WithMany()
                    .HasForeignKey(e => e.UserEmail)
                    .HasPrincipalKey(e => e.Email)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<SearchLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserEmail)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.SearchType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Date)
                    .HasDefaultValueSql("NOW()"); // PostgreSQL; use GETUTCDATE() for SQL Server

                // UserEmail → User.Email
                entity.HasOne(e => e.User)
                    .WithMany() // or .WithMany(u => u.SearchLogs) if reverse navigation exists
                    .HasForeignKey(e => e.UserEmail)
                    .HasPrincipalKey(u => u.Email)
                    .OnDelete(DeleteBehavior.Cascade);

                // DrugId → Drug.Id
                entity.HasOne(e => e.Drug)
                    .WithMany()
                    .HasForeignKey(e => e.DrugNDC)
                    .HasPrincipalKey(e => e.NDC)
                    .OnDelete(DeleteBehavior.Restrict);

                // Optional Insurance FKs
                entity.HasOne(e => e.InsuranceRx)
                    .WithMany()
                    .HasForeignKey(e => e.RxgroupId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Insurance)
                    .WithMany()
                    .HasForeignKey(e => e.BinId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.InsurancePCN)
                    .WithMany()
                    .HasForeignKey(e => e.PcnId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }


}
