using AuthSystem.Api.Domain.Entities;

namespace AuthSystem.Api.Infrastructure.Persistence
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // إنشاء الأدوار
            if (!context.Roles.Any())
            {
                context.Roles.AddRange(
                    new Role { Name = "Admin" },
                    new Role { Name = "User" }
                );
                context.SaveChanges();
            }

            // إنشاء الصلاحيات
            if (!context.Permissions.Any())
            {
                context.Permissions.AddRange(
                    new Permission { Name = "CreateUser" },
                    new Permission { Name = "EditUser" },
                    new Permission { Name = "DeleteUser" },
                    new Permission { Name = "ViewUsers" }
                );
                context.SaveChanges();
            }

            // إنشاء المستخدم الإداري
            if (!context.Users.Any(u => u.Email == "admin@example.com"))
            {
                var adminUser = new User
                {
                    Name = "Admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
                    IsActive = true
                };
                context.Users.Add(adminUser);
                context.SaveChanges();

                var adminRole = context.Roles.First(r => r.Name == "Admin");
                context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
                context.SaveChanges();
            }

            // إنشاء مستخدم عادي
            if (!context.Users.Any(u => u.Email == "user@example.com"))
            {
                var normalUser = new User
                {
                    Name = "Normal User",
                    Email = "user@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123"),
                    IsActive = true
                };
                context.Users.Add(normalUser);
                context.SaveChanges();

                var userRole = context.Roles.First(r => r.Name == "User");
                context.UserRoles.Add(new UserRole { UserId = normalUser.Id, RoleId = userRole.Id });
                context.SaveChanges();
            }

            // ربط الدور Admin بكل الصلاحيات
            var adminRoleId = context.Roles.First(r => r.Name == "Admin").Id;
            var permissions = context.Permissions.Select(p => p.Id).ToList();

            foreach (var permId in permissions)
            {
                if (!context.RolePermissions.Any(rp => rp.RoleId == adminRoleId && rp.PermissionId == permId))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRoleId,
                        PermissionId = permId
                    });
                }
            }

            context.SaveChanges();
        }
    }
}
