using mummybot.Models;
using Microsoft.EntityFrameworkCore;

namespace mummybot
{
    public class mummybotDbContext : DbContext
    {
        public mummybotDbContext(DbContextOptions<mummybotDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Tags> Tags { get; set; }
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<UsersAudit> UsersAudit { get; set; }
        //public virtual DbSet<UserAudits> UserAudits { get; set; }
        public virtual DbSet<MessageLogs> MessageLogs { get; set; }
        public virtual DbSet<Guilds> Guilds { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.0-rtm-35687");

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

                entity.Property(e => e.GuildName)
                    //.IsRequired()
                    .HasColumnName("guildname");

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

            //modelBuilder.Entity<UserAudits>(entity =>
            //{
            //    entity.ToTable("user_audit");

            //    entity.Property(e => e.Id).HasColumnName("id");

            //    entity.Property(e => e.ChangedOn)
            //        .HasColumnName("changedon")
            //        .HasDefaultValueSql("CURRENT_TIMESTAMP");

            //    entity.Property(e => e.GuildId).HasColumnName("guildid");

            //    entity.Property(e => e.Nickname).HasColumnName("nickname");

            //    entity.Property(e => e.UserId).HasColumnName("userid");

            //    entity.Property(e => e.Username).HasColumnName("username");
            //});

            modelBuilder.Entity<MessageLogs>(entity =>
            {
                entity.ToTable("message_logs");

                entity.HasIndex(e => e.Messageid)
                    .HasName("message_logs_messageid_uindex")
                    .IsUnique();

                entity.Property(e => e.Id).HasColumnName("id");

                entity.Property(e => e.Attachments).HasColumnName("attachments");

                entity.Property(e => e.Authorid).HasColumnName("authorid");

                entity.Property(e => e.Channelid).HasColumnName("channelid");

                entity.Property(e => e.Channelname)
                    .IsRequired()
                    .HasColumnName("channelname")
                    .HasMaxLength(100);

                entity.Property(e => e.Content).HasColumnName("content");

                entity.Property(e => e.UpdatedContent).HasColumnName("updatedcontent");

                entity.Property(e => e.Createdat)
                    .HasColumnName("createdat")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.Deleted).HasColumnName("deleted");

                entity.Property(e => e.Deletedat).HasColumnName("deletedat");

                entity.Property(e => e.Guildid).HasColumnName("guildid");

                entity.Property(e => e.Mentionedusers)
                    .IsRequired()
                    .HasColumnName("mentionedusers")
                    .HasColumnType("character varying(32)[]");

                entity.Property(e => e.Messageid).HasColumnName("messageid");

                entity.Property(e => e.Nickname)
                    .HasColumnName("nickname")
                    .HasMaxLength(32);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("username")
                    .HasMaxLength(32);
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

                entity.Property(e => e.Region).HasColumnName("region");

                entity.Property(e => e.Greeting).HasColumnName("greeting");

                entity.Property(e => e.Goodbye).HasColumnName("goodbye");

                entity.Property(e => e.GreetChl).HasColumnName("greetchl");
            });
        }
    }
}