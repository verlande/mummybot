using mummybot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace mummybot
{
    public class mummybotContextFactory : IDesignTimeDbContextFactory<mummybotDbContext>
    {
        public mummybotDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<mummybotDbContext>();
            var creds = new Services.ConfigService();
            optionsBuilder.UseNpgsql(creds.Config["dbstring"]);
            var context = new mummybotDbContext(optionsBuilder.Options);
            return context;
        }
    }

    public class mummybotDbContext : DbContext
    {
        public mummybotDbContext(DbContextOptions<mummybotDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Tags> Tags { get; set; }
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<UsersAudit> UsersAudit { get; set; }
        public virtual DbSet<Guilds> Guilds { get; set; }
        public virtual DbSet<Blacklist> Blacklist { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.0-rtm-35687");
            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<Tags>(entity =>
            {
                entity.ToTable("tags");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Author).HasColumnName("author");

                entity.Property(e => e.Content)
                    .IsRequired()
                    .HasColumnName("content");

                entity.Property(e => e.Createdat).HasColumnName("createdat");

                entity.Property(e => e.Guild).HasColumnName("guild");

                entity.Property(e => e.IsCommand).HasColumnName("iscommand");

                entity.Property(e => e.LastUsedBy).HasColumnName("lastusedby");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasMaxLength(12);

                entity.Property(e => e.Uses).HasColumnName("uses");

                entity.Property(e => e.LastUsed).HasColumnName("lastused");
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.ToTable("users");

                entity.HasIndex(e => e.UserId)
                    .HasName("users_userid_key")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Avatar).HasColumnName("avatar");

                entity.Property(e => e.GuildId).HasColumnName("guildid");

                entity.Property(e => e.Joined).HasColumnName("joined");

                entity.Property(e => e.Nickname).HasColumnName("nickname");

                entity.Property(e => e.TagBanned)
                    .HasColumnName("tagbanned")
                    .HasDefaultValueSql("false");

                entity.Property(e => e.UserId).HasColumnName("userid");

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("username");
            });

            modelBuilder.Entity<UsersAudit>(entity =>
            {
                entity.ToTable("users_audit");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.ChangedOn)
                    .HasColumnName("changedon")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.GuildId).HasColumnName("guildid");

                entity.Property(e => e.Nickname).HasColumnName("nickname");

                entity.Property(e => e.UserId).HasColumnName("userid");

                entity.Property(e => e.Username).HasColumnName("username");
            });

            modelBuilder.Entity<Guilds>(entity =>
            {
                entity.ToTable("guilds");

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.GuildId).HasColumnName("guildid")
                    .IsRequired();

                entity.Property(e => e.GuildName).HasColumnName("guildname")
                    .IsRequired();

                entity.Property(e => e.OwnerId).HasColumnName("ownerid")
                    .IsRequired();

                entity.Property(e => e.Active).HasColumnName("active")
                    .IsRequired()
                    .HasDefaultValue("true");

                entity.Property(e => e.Region).HasColumnName("region")
                    .IsRequired();

                entity.Property(e => e.Greeting).HasColumnName("greeting")
                    .IsRequired().HasDefaultValue("**%user% has joined**");

                entity.Property(e => e.Goodbye).HasColumnName("goodbye")
                    .IsRequired().HasDefaultValue("**%user% has left**");

                entity.Property(e => e.GreetChl).HasColumnName("greetchl");

		        entity.Property(e => e.FilterInvites).HasColumnName("filterinvites");

                entity.Property(e => e.Regex).HasColumnName("regex");
            });

            modelBuilder.Entity<Blacklist>(entity =>
            {
                entity.ToTable("blacklist");

                entity.Property(e => e.Id).HasColumnName("id");
                
                entity.Property(e => e.UserId).HasColumnName("userid")
                    .IsRequired();
                
                entity.Property(e => e.Reason).HasColumnName("reason");
                
                entity.Property(e => e.CreatedAt).HasColumnName("createdat")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
