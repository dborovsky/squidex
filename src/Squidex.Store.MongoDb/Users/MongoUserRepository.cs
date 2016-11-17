﻿// ==========================================================================
//  MongoUserRepository.cs
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex Group
//  All rights reserved.
// ==========================================================================

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.MongoDB;
using Squidex.Infrastructure;
using Squidex.Read.Users;
using Squidex.Read.Users.Repositories;

namespace Squidex.Store.MongoDb.Users
{
    public sealed class MongoUserRepository : IUserRepository
    {
        private readonly UserManager<IdentityUser> userManager;

        public MongoUserRepository(UserManager<IdentityUser> userManager)
        {
            Guard.NotNull(userManager, nameof(userManager));

            this.userManager = userManager;
        }

        public Task<List<IUserEntity>> FindUsersByEmail(string email)
        {
            var users = userManager.Users.Where(x => x.NormalizedEmail.Contains(email)).Take(10).ToList();
            
            return Task.FromResult(users.Select(x => (IUserEntity)new MongoUserEntity(x)).ToList());
        }

        public async Task<IUserEntity> FindUserByIdAsync(string id)
        {
            var user = await userManager.FindByIdAsync(id);

            return user != null ? new MongoUserEntity(user) : null;
        }
    }
}
