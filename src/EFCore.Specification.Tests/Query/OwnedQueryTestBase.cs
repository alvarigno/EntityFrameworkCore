// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Xunit;

// ReSharper disable InconsistentNaming
namespace Microsoft.EntityFrameworkCore.Query
{
    public abstract class OwnedQueryTestBase<TFixture> : IClassFixture<TFixture>
        where TFixture : OwnedQueryTestBase<TFixture>.OwnedQueryFixtureBase, new()
    {
        protected OwnedQueryTestBase(TFixture fixture)
        {
            Fixture = fixture;
            fixture.ListLoggerFactory.Clear();
        }

        protected TFixture Fixture { get; }

        [Fact]
        public virtual void Query_with_owned_entity_equality_operator()
        {
            using (var context = CreateContext())
            {
                var query
                    = (from a in context.Set<LeafA>()
                       from b in context.Set<LeafB>()
                       where a.LeafAAddress == b.LeafBAddress
                       select a).ToList();

                Assert.Equal(0, query.Count);
            }
        }

        [Fact]
        public virtual void Query_with_owned_entity_equality_method()
        {
            using (var context = CreateContext())
            {
                var query
                    = (from a in context.Set<LeafA>()
                       from b in context.Set<LeafB>()
                       where a.LeafAAddress.Equals(b.LeafBAddress)
                       select a).ToList();

                Assert.Equal(0, query.Count);
            }
        }

        [Fact]
        public virtual void Query_with_owned_entity_equality_object_method()
        {
            using (var context = CreateContext())
            {
                var query
                    = (from a in context.Set<LeafA>()
                       from b in context.Set<LeafB>()
                       where Equals(a.LeafAAddress, b.LeafBAddress)
                       select a).ToList();

                Assert.Equal(0, query.Count);
            }
        }

        [Fact]
        public virtual void Query_for_base_type_loads_all_owned_navs()
        {
            using (var context = CreateContext())
            {
                var people = context.Set<OwnedPerson>().ToList();

                Assert.Equal(4, people.Count);
                Assert.True(people.All(p => p.PersonAddress != null));
                Assert.True(people.OfType<Branch>().All(b => b.BranchAddress != null));
                Assert.True(people.OfType<LeafA>().All(a => a.LeafAAddress != null));
                Assert.True(people.OfType<LeafB>().All(b => b.LeafBAddress != null));

                Assert.True(people.All(p => p.Orders.Count == (p.Id == 1 ? 2 : 1)));
            }
        }

        [Fact]
        public virtual void No_ignored_include_warning_when_implicit_load()
        {
            using (var context = CreateContext())
            {
                var count = context.Set<OwnedPerson>().Count();

                Assert.Equal(4, count);
            }
        }

        [Fact]
        public virtual void Query_for_branch_type_loads_all_owned_navs()
        {
            using (var context = CreateContext())
            {
                var people = context.Set<Branch>().ToList();

                Assert.Equal(2, people.Count);
                Assert.True(people.All(p => p.PersonAddress != null));
                Assert.True(people.All(b => b.BranchAddress != null));
                Assert.True(people.OfType<LeafA>().All(a => a.LeafAAddress != null));

                Assert.True(people.All(p => p.Orders.Count == 1));
            }
        }

        [Fact]
        public virtual void Query_for_leaf_type_loads_all_owned_navs()
        {
            using (var context = CreateContext())
            {
                var people = context.Set<LeafA>().ToList();

                Assert.Equal(1, people.Count);
                Assert.True(people.All(p => p.PersonAddress != null));
                Assert.True(people.All(b => b.BranchAddress != null));
                Assert.True(people.All(a => a.LeafAAddress != null));
            }
        }

        [Fact]
        public virtual void Query_when_group_by()
        {
            using (var context = CreateContext())
            {
                var people = context.Set<OwnedPerson>().GroupBy(op => op.Id).ToList();

                Assert.Equal(4, people.Count);
                Assert.True(people.SelectMany(p => p).All(p => p.PersonAddress != null));
                Assert.True(people.SelectMany(p => p).OfType<Branch>().All(b => b.BranchAddress != null));
                Assert.True(people.SelectMany(p => p).OfType<LeafA>().All(a => a.LeafAAddress != null));
                Assert.True(people.SelectMany(p => p).OfType<LeafB>().All(b => b.LeafBAddress != null));
            }
        }

        [Fact]
        public virtual void Query_when_subquery()
        {
            using (var context = CreateContext())
            {
                var people
                    = context.Set<OwnedPerson>()
                        .Distinct()
                        .OrderBy(p => p.Id)
                        .Take(5)
                        .Select(
                            op => new
                            {
                                op
                            })
                        .ToList();

                Assert.Equal(4, people.Count);
                Assert.True(people.All(p => p.op.PersonAddress != null));
                Assert.True(people.Select(p => p.op).OfType<Branch>().All(b => b.BranchAddress != null));
                Assert.True(people.Select(p => p.op).OfType<LeafA>().All(a => a.LeafAAddress != null));
                Assert.True(people.Select(p => p.op).OfType<LeafB>().All(b => b.LeafBAddress != null));
            }
        }

        [Fact]
        public virtual void Fubar()
        {
            using (var ctx = CreateContext())
            {
                var query = ctx.Set<OwnedPerson>().Where(p => p.PersonAddress.Country.Name == "USA").Select(p => p.PersonAddress.Country.Name);
                var result = query.ToList();
            }
        }

        [Fact]
        public virtual void Fubar2()
        {
            using (var ctx = CreateContext())
            {
                var query = ctx.Set<OwnedPerson>().Where(p => p.Orders.Count > 0).Select(p => p.Orders);
                var result = query.ToList();
            }
        }

        [Fact]
        public virtual void Fubar3()
        {
            using (var ctx = CreateContext())
            {
                var query = ctx.Set<OwnedPerson>().Select(p => p.PersonAddress.Country.Planet.Id);
                var result = query.ToList();
            }
        }

        protected virtual DbContext CreateContext() => Fixture.CreateContext();

        public abstract class OwnedQueryFixtureBase : SharedStoreFixtureBase<PoolableDbContext>
        {
            protected override string StoreName { get; } = "OwnedQueryTest";

            protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
            {
                modelBuilder.Entity<OwnedPerson>(
                    eb =>
                    {
                        eb.HasData(
                            new OwnedPerson
                            {
                                Id = 1
                            });

                        eb.OwnsOne(
                            p => p.PersonAddress, ab =>
                            {
                                ab.HasData(
                                    new
                                    {
                                        OwnedPersonId = 1
                                    }, new
                                    {
                                        OwnedPersonId = 2
                                    }, new
                                    {
                                        OwnedPersonId = 3
                                    }, new
                                    {
                                        OwnedPersonId = 4
                                    });

                                ab.OwnsOne(a => a.Country, cb =>
                                {
                                    cb.HasData(
                                    new
                                    {
                                        OwnedAddressOwnedPersonId = 1,
                                        PlanetId = 1,
                                        NationalAnthemId = 1,
                                        Name = "USA"
                                    }, new
                                    {
                                        OwnedAddressOwnedPersonId = 2,
                                        PlanetId = 1,
                                        NationalAnthemId = 1,
                                        Name = "USA"
                                    }, new
                                    {
                                        OwnedAddressOwnedPersonId = 3,
                                        PlanetId = 1,
                                        NationalAnthemId = 1,
                                        Name = "USA"
                                    }, new
                                    {
                                        OwnedAddressOwnedPersonId = 4,
                                        PlanetId = 1,
                                        NationalAnthemId = 1,
                                        Name = "USA"
                                    });

                                    cb.HasOne(cc => cc.Planet).WithMany().HasForeignKey(ee => ee.PlanetId).OnDelete(DeleteBehavior.Restrict);
                                    cb.HasOne(cc => cc.NationalAnthem).WithOne().HasForeignKey<OwnedCountry>(ee => ee.NationalAnthemId);
                                });
                            });

                        eb.OwnsMany(
                            p => p.Orders, ob =>
                            {
                                ob.HasData(
                                    new
                                    {
                                        Id = -10,
                                        ClientId = 1
                                    },
                                    new
                                    {
                                        Id = -11,
                                        ClientId = 1
                                    },
                                    new
                                    {
                                        Id = -20,
                                        ClientId = 2
                                    },
                                    new
                                    {
                                        Id = -30,
                                        ClientId = 3
                                    },
                                    new
                                    {
                                        Id = -40,
                                        ClientId = 4
                                    }
                                    );
                            });
                    });

                modelBuilder.Entity<Branch>(
                    eb =>
                    {
                        eb.HasData(
                            new Branch
                            {
                                Id = 2
                            });

                        eb.OwnsOne(
                            p => p.BranchAddress, ab =>
                            {
                                ab.HasData(
                                    new
                                    {
                                        BranchId = 2
                                    }, new
                                    {
                                        BranchId = 3
                                    });

                                ab.OwnsOne(a => a.Country, cb =>
                                {
                                    cb.HasData(
                                        new
                                        {
                                            OwnedAddressBranchId = 2,
                                            PlanetId = 1,
                                            NationalAnthemId = 2,
                                            Name = "Canada"
                                        }, new
                                        {
                                            OwnedAddressBranchId = 3,
                                            PlanetId = 1,
                                            NationalAnthemId = 2,
                                            Name = "Canada"
                                        });

                                    cb.HasOne(cc => cc.Planet).WithMany().HasForeignKey(ee => ee.PlanetId).OnDelete(DeleteBehavior.Restrict);
                                    cb.HasOne(cc => cc.NationalAnthem).WithOne().HasForeignKey<OwnedCountry>(ee => ee.NationalAnthemId);
                                });
                            });
                    });

                modelBuilder.Entity<LeafA>(
                    eb =>
                    {
                        eb.HasData(
                            new LeafA
                            {
                                Id = 3
                            });

                        eb.OwnsOne(
                            p => p.LeafAAddress, ab =>
                            {
                                ab.HasData(
                                    new
                                    {
                                        LeafAId = 3
                                    });

                                ab.OwnsOne(a => a.Country, cb =>
                                {
                                    cb.HasData(
                                    new
                                    {
                                        OwnedAddressLeafAId = 3,
                                        PlanetId = 1,
                                        Name = "Mexico"
                                    });

                                    cb.HasOne(cc => cc.Planet).WithMany().HasForeignKey(ee => ee.PlanetId).OnDelete(DeleteBehavior.Restrict);
                                    cb.HasOne(cc => cc.NationalAnthem).WithOne().HasForeignKey<OwnedCountry>(ee => ee.NationalAnthemId);
                                });
                            });
                    });

                modelBuilder.Entity<LeafB>(
                    eb =>
                    {
                        eb.HasData(
                            new LeafB
                            {
                                Id = 4
                            });

                        eb.OwnsOne(
                            p => p.LeafBAddress, ab =>
                            {
                                ab.HasData(
                                    new
                                    {
                                        LeafBId = 4
                                    });

                                ab.OwnsOne(a => a.Country, cb =>
                                {
                                    cb.HasData(
                                    new
                                    {
                                        OwnedAddressLeafBId = 4,
                                        PlanetId = 1,
                                        Name = "Panama"
                                    });

                                    cb.HasOne(cc => cc.Planet).WithMany().HasForeignKey(ee => ee.PlanetId).OnDelete(DeleteBehavior.Restrict);
                                    cb.HasOne(cc => cc.NationalAnthem).WithOne().HasForeignKey<OwnedCountry>(ee => ee.NationalAnthemId);
                                });
                            });
                    });

                modelBuilder.Entity<Planet>(pb =>
                {
                    pb.HasData(new Planet { Id = 1 });
                });

                modelBuilder.Entity<NationalAnthem>(pb =>
                {
                    pb.HasData(
                        new NationalAnthem { Id = 1, Name = "The Star-Spangled Banner" },
                        new NationalAnthem { Id = 2, Name = "O Canada" });
                });
            }

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
                => base.AddOptions(builder).ConfigureWarnings(wcb => wcb.Throw());

            public override PoolableDbContext CreateContext()
            {
                var context = base.CreateContext();
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                return context;
            }
        }

        protected class OwnedAddress
        {
            public OwnedCountry Country { get; set; }
        }

        protected class OwnedCountry
        {
            public string Name { get; set; }

            public int PlanetId { get; set; }
            public Planet Planet { get; set; }

            public int? NationalAnthemId { get; set; }
            public NationalAnthem NationalAnthem { get; set; }
        }

        protected class NationalAnthem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        protected class OwnedPerson
        {
            public int Id { get; set; }
            public OwnedAddress PersonAddress { get; set; }
            public ICollection<Order> Orders { get; set; }
        }

        protected class Order
        {
            public int Id { get; set; }
            public OwnedPerson Client { get; set; }
        }

        protected class Branch : OwnedPerson
        {
            public OwnedAddress BranchAddress { get; set; }
        }

        protected class LeafA : Branch
        {
            public OwnedAddress LeafAAddress { get; set; }
        }

        protected class LeafB : OwnedPerson
        {
            public OwnedAddress LeafBAddress { get; set; }
        }

        protected class Planet
        {
            public int Id { get; set; }
        }
    }
}
