﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace EntityFrameworkCore.Detached.ManyToMany
{
    public class ManyToManyPatchContext
    {
        #region Fields

        static MethodInfo LoadCollectionMethodInfo = typeof(ManyToManyPatchContext).GetMethod(nameof(LoadCollection), BindingFlags.NonPublic | BindingFlags.Instance);
        DbContext _dbContext;

        #endregion

        #region Ctor.

        public ManyToManyPatchContext(DbContext context)
        {
            _dbContext = context;
        }

        #endregion

        /// <summary>
        /// Externally loads the collections marked as ManyToMany.
        /// </summary>
        /// <param name="entityType">Metadata of the entity being processed.</param>
        /// <param name="entity">Entity whose properties will be loaded.</param>
        public async Task LoadManyToManyRelations(EntityType entityType, object entity)
        {
            foreach (ManyToManyNavigation m2mProperty in entityType.GetManyToManyNavigations())
            {
                // query collection for the given many to many (imperformant!!!).
                MethodInfo loadMethodInfo = LoadCollectionMethodInfo
                                                .MakeGenericMethod(m2mProperty.End1.EntityType.ClrType,
                                                                   m2mProperty.End2.EntityType.ClrType);

                Task<object> loadedCollectionTask = (Task<object>)loadMethodInfo.Invoke(this, new[] { entity });
                object loadedCollection = await loadedCollectionTask;

                // set the result to the many to many property.
                m2mProperty.End1.Property.Setter.SetClrValue(entity, loadedCollection);
            }

            // recursively load other many to many properties.
            foreach (Navigation nav in entityType.GetNavigations())
            {
                bool owned = nav.IsOwned();
                if (owned)
                {
                    object value = nav.Getter.GetClrValue(entity);
                    EntityType itemType = nav.GetTargetType();

                    if (value != null)
                    {
                        if (nav.IsCollection())
                        {
                            foreach (object item in (IEnumerable)value)
                            {
                                await LoadManyToManyRelations(itemType, item);
                            }
                        }
                        else
                        {
                            await LoadManyToManyRelations(itemType, value);
                        }
                    }
                }
            }
        }

        async Task<object> LoadCollection<TEnd1, TEnd2>(TEnd1 entity)
            where TEnd1 : class
        {
            return await _dbContext.Set<ManyToManyEntity<TEnd1, TEnd2>>()
                             .Where(m2m => m2m.End1 == entity)
                             .Select(m2m => m2m.End2)
                             .ToListAsync();
        }
    }
}
